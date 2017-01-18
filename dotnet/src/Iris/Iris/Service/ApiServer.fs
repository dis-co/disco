namespace Iris.Serivce

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Client
open Iris.Zmq
open Iris.Service.Interfaces
open Iris.Serialization.Api
open Iris.Serialization.Raft

// * ApiServer module

[<AutoOpen>]
module ApiServer =

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  let private tag (str: string) = sprintf "IApiServer.%s" str

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<int, IObserver<ApiEvent>>

  // ** Client

  [<NoComparison;NoEquality>]
  type private Client =
    { Meta: IrisClient
      Socket: Req }

  // ** ClientStateData

  [<NoComparison;NoEquality>]
  type private ClientStateData =
    { Server: Rep
      Clients: Map<Id,Client> }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Server

  // ** ClientState

  [<NoComparison;NoEquality>]
  type private ClientState =
    | Loaded of ClientStateData
    | Idle

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | Clients of Map<Id,IrisClient>
    | Ok

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start         of chan:ReplyChan * IrisConfig
    | Dispose       of chan:ReplyChan
    | UpdateClients of chan:ReplyChan * StateMachine
    | GetClients    of chan:ReplyChan
    | AddClient     of chan:ReplyChan * IrisClient
    | RemoveClient  of chan:ReplyChan * IrisClient

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** Listener

  type private Listener = IObservable<ApiEvent>

  // ** createListener

  let private createListener (subscriptions: Subscriptions) =
    { new Listener with
        member self.Subscribe(obs) =
          while not (subscriptions.TryAdd(obs.GetHashCode(), obs)) do
            Thread.Sleep(1)

          { new IDisposable with
              member self.Dispose() =
                match subscriptions.TryRemove(obs.GetHashCode()) with
                | true, _  -> ()
                | _ -> subscriptions.TryRemove(obs.GetHashCode())
                      |> ignore } }

  // ** requestHandler

  let private requestHandler (agent: ApiAgent) (raw: byte array) =
    match Binary.decode raw with
    | Right Ping -> Binary.encode Pong
    | Right (Register client) ->
      match agent.PostAndReply(fun chan -> Msg.AddClient(chan, client)) with
      | Right Reply.Ok -> Binary.encode OK
      | Right other -> Binary.encode (NOK "internal error")
      | Left error ->
        error
        |> (string >> NOK)
        |> Binary.encode
    | Right (UnRegister client) ->
      match agent.PostAndReply(fun chan -> Msg.RemoveClient(chan, client)) with
      | Right Reply.Ok -> Binary.encode OK
      | Right other -> Binary.encode (NOK "internal error")
      | Left error ->
        error
        |> (string >> NOK)
        |> Binary.encode
    | Left error ->
      error
      |> (string >> NOK)
      |> Binary.encode

  // ** start

  let private start (chan: ReplyChan) (agent: ApiAgent) (config: IrisConfig) =
    let addr = "heheheheh"
    let server = new Rep(addr, requestHandler agent)
    match server.Start() with
    | Right () ->
      chan.Reply(Right Reply.Ok)
      Loaded { Clients = Map.empty; Server = server }
    | Left error ->
      chan.Reply(Left error)
      dispose server
      Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ClientState)
                          (agent: ApiAgent)
                          (config: IrisConfig) =
    match state with
    | Loaded data ->
      dispose data
      start chan agent config
    | Idle ->
      start chan agent config

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      dispose data.Server
      chan.Reply(Right Reply.Ok)
      Idle
    | Idle ->
      chan.Reply(Right Reply.Ok)
      Idle

  // ** handleAddClient

  let private handleAddClient (chan: ReplyChan)
                              (state: ClientState)
                              (subs: Subscriptions)
                              (meta: IrisClient) =
    match state with
    | Loaded data ->
      chan.Reply(Right Reply.Ok)
      let client = { Meta = meta; Socket = Unchecked.defaultof<Req> }
      Loaded { data with Clients = Map.add meta.Id client data.Clients }
    | Idle ->
      chan.Reply(Right Reply.Ok)
      Idle

  // ** handleRemoveClient

  let private handleRemoveClient (chan: ReplyChan)
                                 (state: ClientState)
                                 (subs: Subscriptions)
                                 (client: IrisClient) =
    match state with
    | Loaded data ->
      chan.Reply(Right Reply.Ok)
      Loaded { data with Clients = Map.remove client.Id data.Clients }
    | Idle ->
      chan.Reply(Right Reply.Ok)
      Idle

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    async {
      printfn "updating client: %A" client.Meta.Id
    }

  // ** handleUpdateClients

  let private handleUpdateClients (chan: ReplyChan) (state: ClientState) (sm: StateMachine) =
    match state with
    | Loaded data ->
      data.Clients
      |> Map.toArray
      |> Array.map (snd >> updateClient sm)
      |> Async.Parallel
      |> Async.RunSynchronously
      |> ignore
      chan.Reply(Right Reply.Ok)
      state
    | Idle ->
      "ClientApi not running"
      |> Error.asClientError (tag "handleUpdateClients")
      |> Either.fail
      |> chan.Reply
      state

  // ** handleGetClients

  let private handleGetClients (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      data.Clients
      |> Map.map (fun k v -> v.Meta)
      |> Reply.Clients
      |> Either.succeed
      |> chan.Reply
      state
    | Idle ->
      "ClientApi not running"
      |> Error.asClientError (tag "handleGetClients")
      |> Either.fail
      |> chan.Reply
      state

  // ** loop

  let private loop (initial: ClientState) (subs: Subscriptions) (inbox: ApiAgent) =
    let rec act (state: ClientState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          match msg with
          | Msg.Start(chan,config)        -> handleStart chan state inbox config
          | Msg.Dispose chan              -> handleDispose chan state
          | Msg.AddClient(chan,client)    -> handleAddClient chan state subs client
          | Msg.RemoveClient(chan,client) -> handleRemoveClient chan state subs client
          | Msg.UpdateClients(chan,sm)    -> handleUpdateClients chan state sm
          | Msg.GetClients(chan)          -> handleGetClients chan state

        return! act newstate
      }
    act initial

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  // ** ApiServer module

  [<RequireQualifiedAccess>]
  module ApiServer =

    let create (config: IrisConfig) =
      either {
        let cts = new CancellationTokenSource()
        let subs = new Subscriptions()
        let agent = new ApiAgent(loop Idle subs, cts.Token)
        let listener = createListener subs
        agent.Start()

        return
          { new IApiServer with
              member self.Start () =
                match agent.PostAndReply(fun chan -> Msg.Start(chan,config)) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "Start")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Clients
                with get () =
                  match agent.PostAndReply(fun chan -> Msg.GetClients(chan)) with
                  | Right (Reply.Clients clients) -> Either.succeed clients
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "UpdateClients")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.UpdateClients (sm: StateMachine) =
                match agent.PostAndReply(fun chan -> Msg.UpdateClients(chan, sm)) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "UpdateClients")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Subscribe (callback: ApiEvent -> unit) =
                { new IObserver<ApiEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Dispose () =
                agent.PostAndReply(fun chan -> Msg.Dispose chan)
                |> ignore
                dispose cts
            }
      }
