[<RequireQualifiedAccess>]
module Iris.Unity

open System
open Iris.Core
open Iris.Client
open ZeroMQ
open System.Threading

type OptionBuilder() =
    member x.Bind(v,f) = Option.bind f v
    member x.Return v = Some v
    member x.ReturnFrom o = o
    member x.Zero() = Some ()

let option = OptionBuilder()

type ObjectId = int

type State =
  { PinGroup: PinGroup
    GameObjects: Map<ObjectId, Action<float>> }

type Msg =
  | IrisEvent of ClientEvent
  | UpdateState of State
  | RegisterObject of objectId: int * callback: Action<double>

type Actor = MailboxProcessor<Msg>

type IIrisClient =
  inherit IDisposable
  abstract member RegisterGameObject: objectId: int * callback: Action<float> -> unit
 
let startApiClient(serverIp, serverPort: uint16, print: string->unit) =
    let myself: IrisClient =
      { Id = Id.Create()
        Name = "Unity Client"
        Role = Role.Renderer
        Status = ServiceStatus.Starting
        IpAddress = IPv4Address "127.0.0.1"
        Port = port 10500us }

    let server : IrisServer =
      let ip =
        match IpAddress.TryParse serverIp with
        | Right ip ->  ip
        | Left error ->
          error
          |> string
          |> Logger.err "startApiClient"
          IPv4Address "127.0.0.1"
      { Port = port serverPort; IpAddress = ip }

    sprintf "Unity client at %O:%O connecting to Iris at %O:%O..."
      myself.IpAddress myself.Port server.IpAddress server.Port |> print
    
    let zcontext = new ZContext()
    let client = ApiClient.create zcontext server myself

    match client.Start() with
    | Right () ->
      Logger.info "startClient" "Successfully started ApiClient"
      print "Successfully started ApiClient"
      myself.Id, zcontext, client
    | Left error ->
      let msg = string error
      Logger.err "startClient" msg
      exn msg |> raise

let withState (state: State option) (f: State->State) =
  match state with
  | Some state -> f state |> Some
  // TODO: Log/throw exception if state it's not initialized
  | None -> state

let startApiClientAndActor (serverIp, serverPort: uint16, print) =
  let clientId, zcontext, client = startApiClient(serverIp, serverPort, print) 
  let actor = Actor.Start(fun inbox -> async {
    let rec loop state = async {
      let! msg = inbox.Receive()
      let newState =
        try
          match msg with
          | UpdateState state -> Some state
          | IrisEvent ev ->
            withState state <| fun state ->
              match ev with
              //| ClientEvent.Update(UpdatePinGroup pinGroup) when pinGroup.Id = state.PinGroup.Id ->
              | ClientEvent.Update(UpdatePin pin) when pin.PinGroup = state.PinGroup.Id ->
                let objectId =
                  let id = string pin.Id
                  id.Substring(id.IndexOf('/') + 1) |> int
                option {
                  let! callback = Map.tryFind objectId state.GameObjects
                  let! value = pin.Values.At(index 0).NumberValue
                  callback.Invoke(value)
                } |> function
                  | Some () -> state // Update state.PinGroup?
                  | None -> state
              | _ -> state
          | RegisterObject(objectId, callback) ->
            // TODO: Batch pin creation
            withState state <| fun state ->
              let pinGroup =
                let id = sprintf "%O/%i" state.PinGroup.Id objectId |> Id
                if not(Map.containsKey id state.PinGroup.Pins)
                then
                  sprintf "Registering pin %O to Iris" id |> print
                  let pin = Pin.number id (string objectId) state.PinGroup.Id [|astag "Scale"|] [|1.|]
                  client.AddPin(pin)
                  { state.PinGroup with Pins = Map.add id pin state.PinGroup.Pins }
                else state.PinGroup
              // Update allways the internal map in case the callback has changed
              { PinGroup = pinGroup; GameObjects = Map.add objectId callback state.GameObjects }
        with
        | ex -> Logger.err "Iris.Unity.actorLoop" ex.Message; state
      return! loop state
    }
    return! loop None
  })

  // Create PinGroup and add it to Iris
  let pinGroup: PinGroup =
    { Id = Guid.NewGuid().ToString().[..7] |> sprintf "unity-%s" |> Id
      Name = name "Unity"
      Client = clientId
      Pins = Map.empty }
  client.AddPinGroup(pinGroup)
  { PinGroup = pinGroup; GameObjects = Map.empty }
  |> UpdateState |> actor.Post

  zcontext, client, actor

let private myLock = obj()
let mutable private client = None

[<CompiledName("GetIrisClient")>]
let getIrisClient(serverIp, serverPort, print: Action<string>) =
    lock myLock (fun () ->
      match client with
      | Some client ->
        print.Invoke("Reciclying client instance")
        client
      | None ->
        let zcontext, apiClient, actor = startApiClientAndActor(serverIp, serverPort, print.Invoke)
        // Subscribe to API client events
        let apiobs = apiClient.Subscribe(fun ev -> actor.Post(IrisEvent ev))
        let client2: IIrisClient =
          { new IIrisClient with
              member this.Dispose() =
                apiobs.Dispose()
                zcontext.Dispose()
                (actor :> IDisposable).Dispose()
              member this.RegisterGameObject(objectId: int, callback: Action<double>) =
                RegisterObject(objectId, callback) |> actor.Post }
        client <- Some client2
        client2
    )
      