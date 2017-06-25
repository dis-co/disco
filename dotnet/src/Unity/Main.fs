[<RequireQualifiedAccess>]
module Iris.Unity

open System
open Iris.Core
open Iris.Client
open System.Threading

type OptionBuilder() =
    member x.Bind(v,f) = Option.bind f v
    member x.Return v = Some v
    member x.ReturnFrom o = o
    member x.Zero() = Some ()

let option = OptionBuilder()

type ObjectId = int

type State =
  { IsRunning: bool
    PinGroup: PinGroup
    GameObjects: Map<ObjectId, Action<float>>
    PendingRegistrations: (int*Action<double>) list }

type Msg =
  | IrisEvent of ClientEvent
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
        Port = port 3500us }

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
    
    let client = ApiClient.create server myself

    match client.Start() with
    | Right () ->
      Logger.info "startClient" "Successfully started Unity ApiClient"
      print "Successfully started Iris Client"
      myself.Id, client
    | Left error ->
      let msg = string error
      Logger.err "startClient" msg
      exn msg |> raise

let startActor(state, client: IApiClient, print: string->unit) = Actor.Start(fun inbox -> async {
  let rec loop state = async {
    let! msg = inbox.Receive()
    sprintf "DEBUG: Iris client actor received message: %A" msg |> print
    let newState =
      try
        match msg with
        | IrisEvent ev ->
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
          | ClientEvent.Status(ServiceStatus.Running) ->
            print("DEBUG: Registrations pending " + (string state.PendingRegistrations.Length))
            for (id,cb) in state.PendingRegistrations do
              inbox.Post(RegisterObject(id,cb))
            { state with IsRunning = true; PendingRegistrations = [] }
          | _ -> state
        | RegisterObject(objectId, callback) ->
          // TODO: Batch pin creation
          if state.IsRunning then
            let pinGroup =
              let id = sprintf "%O/%i" state.PinGroup.Id objectId |> Id
              print("DEBUG: Request to register game object with id: " + (string id))
              if not(Map.containsKey id state.PinGroup.Pins)
              then
                sprintf "Registering pin %O to Iris" id |> print
                let pin = Pin.number id (string objectId) state.PinGroup.Id [|astag "Scale"|] [|1.|]
                client.AddPin(pin)
                { state.PinGroup with Pins = Map.add id pin state.PinGroup.Pins }
              else state.PinGroup
            // Update allways the internal map in case the callback has changed
            { state with PinGroup = pinGroup; GameObjects = Map.add objectId callback state.GameObjects }
          else            
            { state with PendingRegistrations = (objectId, callback)::state.PendingRegistrations }
      with
      | ex ->
        Logger.err "Iris.Unity.actorLoop" ex.Message
        print("Iris client error: " + ex.Message)
        state
    return! loop newState
  }
  return! loop state
})

let startApiClientAndActor (serverIp, serverPort: uint16, print) =
  let clientId, client = startApiClient(serverIp, serverPort, print) 
  let state =
    // Create PinGroup and add it to Iris
    let pinGroup: PinGroup =
      { Id = Guid.NewGuid().ToString().[..7] |> sprintf "unity-%s" |> Id
        Name = name "Unity"
        Client = clientId
        Pins = Map.empty }
    client.AddPinGroup(pinGroup)
    { IsRunning = false; PinGroup = pinGroup; GameObjects = Map.empty; PendingRegistrations = [] }
  let actor = startActor(state, client, print)
  print("DEBUG: Iris client actor started")
  client, actor

let private myLock = obj()
let mutable private client = None

[<CompiledName("GetIrisClient")>]
let getIrisClient(serverIp, serverPort, print: Action<string>) =
    lock myLock (fun () ->
      match client with
      | Some client ->
        print.Invoke("Reciclying Iris client instance")
        client
      | None ->
        let apiClient, actor = startApiClientAndActor(serverIp, serverPort, print.Invoke)
        print.Invoke("DEBUG: Subscribing to API Client")
        // Subscribe to API client events
        let apiobs = apiClient.Subscribe(IrisEvent >> actor.Post)
        let client2: IIrisClient =
          let mutable disposed = false
          { new IIrisClient with
              member this.Dispose() =
                if not disposed then
                  disposed <- true
                  apiobs.Dispose()
                  (actor :> IDisposable).Dispose()
                  print.Invoke("Iris client disposed")
              member this.RegisterGameObject(objectId: int, callback: Action<double>) =
                RegisterObject(objectId, callback) |> actor.Post }
        client <- Some client2
        client2
    )
      