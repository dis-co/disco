[<RequireQualifiedAccess>]
module Iris.Unity

open System
open Iris.Core
open Iris.Client
open ZeroMQ

type State =
  { Client: IApiClient
    PinGroup: PinGroup }

type Msg =
  | IrisEvent of ClientEvent
  | UpdateState of State
  | RegisterGameObject of objectId: int * callback: Action<float>

type RawActor = MailboxProcessor<Msg>

type Actor(rawActor: RawActor, [<ParamArray>] disposables: IDisposable[]) =
  member __.Post(msg) =
    rawActor.Post(msg)
  interface IDisposable with
    member __.Dispose() =
      for disp in disposables do
        disp.Dispose()
      (rawActor :> IDisposable).Dispose()

let startApiClient(actor: RawActor, serverIp, serverPort: string) =
    let myself: IrisClient =
      { Id = Id.Create()
        Name = "Unity Client"
        Role = Role.Renderer
        Status = ServiceStatus.Starting
        IpAddress = IPv4Address "127.0.0.1"
        Port = port Constants.DEFAULT_API_CLIENT_PORT }

    let server : IrisServer =
      let ip =
        match IpAddress.TryParse serverIp with
        | Right ip ->  ip
        | Left error ->
          error
          |> string
          |> Logger.err "startApiClient"
          IPv4Address "127.0.0.1"
      let port =
        try serverPort |> uint16 |> port
        with _ -> port Constants.DEFAULT_API_PORT
      { Port = port; IpAddress = ip }

    let client = ApiClient.create (new ZContext()) server myself

    match client.Start() with
    | Right () ->
      Logger.info "startClient" "successfully started ApiClient"
      let pinGroup: PinGroup =
        { Id = Id.Create()
          Name = name "Unity"
          Client = myself.Id
          Pins = Map.empty }
      client.AddPinGroup(pinGroup)
      let state = { Client = client; PinGroup = pinGroup }
      let obs = client.Subscribe(fun ev -> actor.Post(IrisEvent ev))
      state, obs
    | Left error ->
      let msg = string error
      Logger.err "startClient" msg
      exn msg |> raise

let startAgent (serverIp, serverPort: string) =
  let rawActor = new RawActor(fun inbox -> async {
    let rec loop state = async {
      let! msg = inbox.Receive()
      let newState =
        match msg with
        | UpdateState state -> Some state
        | IrisEvent ev -> failwith "TODO"
        | RegisterGameObject(objectId, callback) -> failwith "TODO"
      return! loop state
    }
    return! loop None
  })
  let initState, disp = startApiClient(rawActor, serverIp, serverPort) 
  rawActor.Start()
  rawActor.Post(UpdateState initState)
  new Actor(rawActor, disp)
 
let registerObject(actor: Actor, objectId: int, callback: Action<float>) =
  actor.Post(RegisterGameObject(objectId, callback))

