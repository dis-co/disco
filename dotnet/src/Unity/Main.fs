[<RequireQualifiedAccess>]
module Iris.Unity

open System
open System.Collections.Generic
open Iris.Core
open Iris.Client
open System.Threading

type OptionBuilder() =
    member x.Bind(v,f) = Option.bind f v
    member x.Return v = Some v
    member x.ReturnFrom o = o
    member x.Zero() = Some ()

let option = OptionBuilder()

type State =
  { IsRunning: bool
    PinGroups: Map<string,PinGroup>
    Callbacks: Map<string, Action<double[]>> }

type Msg =
  | IrisEvent of ClientEvent
  | RegisterObject of groupName: string * pinName: string * values: IDictionary<string,double> * callback: Action<double[]>
  | Dispose

type Actor = MailboxProcessor<Msg>

type IIrisClient =
  inherit IDisposable
  abstract member Guid: Guid
  abstract member RegisterGameObject: groupName: string * pinName: string * values: IDictionary<string,double> * callback: Action<double[]> -> unit

let startApiClient(clientGuid: Guid, serverIp, serverPort: uint16, clientIp, clientPort: uint16, print: string->unit) =
    let myself: IrisClient =
      { Id = string clientGuid |> Id
        Name = string clientGuid
        Role = Role.Renderer
        Status = ServiceStatus.Starting
        IpAddress = IPv4Address clientIp
        Port = port clientPort }
    let server: IrisServer =
      { Port = port serverPort; IpAddress = IPv4Address serverIp }
    sprintf "Unity client at %O:%O connecting to Iris at %O:%O..."
      myself.IpAddress myself.Port server.IpAddress server.Port |> print
    let client = ApiClient.create server myself
    myself.Id, client

let addPin(pin: Pin, client: IApiClient, isRunning: bool) =
  if isRunning then
    client.RemovePin(pin)
    client.AddPin(pin)

let addPinGroup(pinGroup: PinGroup, client: IApiClient, isRunning: bool) =
  if isRunning then
    client.RemovePinGroup(pinGroup)
    client.AddPinGroup(pinGroup)

let startActor(state, client: IApiClient, clientId, print: string->unit) =
  let mutable apiobs: IDisposable option = None
  let actor =
    Actor.Start(fun inbox -> async {
      let rec loop (state: State) = async {
        let! msg = inbox.Receive()
        let newState =
          try
            match msg with
            | Dispose ->
              apiobs |> Option.iter (fun x -> x.Dispose())
              for KeyValue(_,group) in state.PinGroups do
                  client.RemovePinGroup(group)
              client.Dispose()
              print("Iris client disposed")
              None
            | IrisEvent ev ->
              match ev with
              | ClientEvent.Update(UpdateSlices(Slices.NumberSlices(id, slices))) ->
                match Map.tryFind (string id) state.Callbacks with
                | Some callback -> callback.Invoke(slices)
                | None -> ()
                Some state
              | ClientEvent.Status status ->
                print(sprintf "IrisClient status: %A" status)
                if status = ServiceStatus.Running then
                  //for KeyValue(_,pinGroup) in state.PinGroups do
                  //  addPinGroup(pinGroup, client, true)
                  Some { state with IsRunning = true }
                else Some state
              | _ -> Some state
            | RegisterObject(groupName, pinName, values, callback) ->
              let groupId, pinId = Id groupName, Id(groupName + "/" + pinName)
              let tags, values = values |> Seq.map (fun kv -> (astag kv.Key), kv.Value) |> Seq.toArray |> Array.unzip
              let pin = Pin.number pinId pinName groupId tags values
              let pinGroup =
                match Map.tryFind groupName state.PinGroups with
                | Some pinGroup ->
                  if Map.containsKey pinId pinGroup.Pins then
                    failwithf "There's already a pin registered with group %s and name %s" groupName pinName
                  addPin(pin, client, state.IsRunning)
                  { pinGroup with Pins = Map.add pinId pin pinGroup.Pins }
                | None ->
                  let pinGroup = { Id = Id groupName; Name = name groupName; Client = clientId; Pins = Map[pinId,pin] }
                  addPinGroup(pinGroup, client, state.IsRunning)
                  pinGroup
              Some { state with PinGroups = Map.add groupName pinGroup state.PinGroups; Callbacks = Map.add (string pinId) callback state.Callbacks }
          with
          | ex ->
            Logger.err "Iris.Unity.actorLoop" ex.Message
            print("Iris client error: " + ex.Message)
            Some state
        match newState with
        | Some newState -> return! loop newState
        | None -> return ()
      }
      return! loop state
    })
  // Subscribe to API client events
  apiobs <- client.Subscribe(IrisEvent >> actor.Post) |> Some
  match client.Start() with
  | Right () ->
    Logger.info "startClient" "Successfully started Unity ApiClient"
    print(sprintf "Successfully started Iris Client (status %A)" client.Status)
    actor
  | Left error ->
    let msg = string error
    Logger.err "startClient" msg
    print ("Couldn't start Iris Client: " + msg)
    exn msg |> raise

let startApiClientAndActor(clientGuid, serverIp, serverPort: uint16, clientIp, clientPort, print) =
  let clientId, client = startApiClient(clientGuid, serverIp, serverPort, clientIp, clientPort, print)
  let state = { IsRunning = false; PinGroups = Map.empty; Callbacks = Map.empty }
  let actor = startActor(state, client, clientId, print)
  client, actor

let private myLock = obj()
let mutable private client: IIrisClient option = None

[<CompiledName("GetIrisClient")>]
let getIrisClient(clientGuid, serverIp, serverPort, clientIp, clientPort, print: Action<string>) =
    lock myLock (fun () ->
      match client with
      | Some client ->
        if client.Guid <> clientGuid then
          failwithf "An Iris Client with guid %O has already been started" client.Guid
        client
      | None ->
        let apiClient, actor = startApiClientAndActor(clientGuid, serverIp, serverPort, clientIp, clientPort, print.Invoke)
        let client2: IIrisClient =
          let mutable disposed = false
          { new IIrisClient with
              member this.Dispose() =
                if not disposed then
                  disposed <- true
                  actor.Post Dispose
              member this.Guid = clientGuid
              member this.RegisterGameObject(groupName, pinName, values, callback) =
                RegisterObject(groupName, pinName, values, callback) |> actor.Post }
        client <- Some client2
        client2
    )
