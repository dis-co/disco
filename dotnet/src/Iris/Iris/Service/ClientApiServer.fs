namespace Iris.Serivce

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Client

// * IClientApiServer

type IClientApiServer =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>

// * IrisClientEvent

type IrisClientEvent =
  | AddClient    of IrisClient
  | RemoveClient of IrisClient

// * ClientApiServer module

[<AutoOpen>]
module ClientApiServer =

  // ** tag

  let private tag (str: string) = sprintf "IClientApiServer.%s" str

  // ** Subscriptions

  type private Subscriptions = ConcurrentBag<IObserver<IrisClientEvent>>

  // ** ClientState

  type private ClientState =
    { Clients: Map<Id, IrisClient> }

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | Ok

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Dispose      of chan:ReplyChan
    | AddClient    of chan:ReplyChan * IrisClient
    | RemoveClient of chan:ReplyChan * IrisClient

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ClientState) =
    chan.Reply(Right Reply.Ok)
    state

  // ** handleAddClient

  let private handleAddClient (chan: ReplyChan)
                              (state: ClientState)
                              (subs: Subscriptions)
                              (client: IrisClient) =
    chan.Reply(Right Reply.Ok)
    { state with Clients = Map.add client.Id client state.Clients }

  // ** handleRemoveClient

  let private handleRemoveClient (chan: ReplyChan)
                                 (state: ClientState)
                                 (subs: Subscriptions)
                                 (client: IrisClient) =
    chan.Reply(Right Reply.Ok)
    { state with Clients = Map.remove client.Id state.Clients }

  // ** loop

  let private loop (initial: ClientState) (subs: Subscriptions) (inbox: ApiAgent) =
    let rec act (state: ClientState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          match msg with
          | Msg.Dispose chan              -> handleDispose chan state
          | Msg.AddClient(chan,client)    -> handleAddClient chan state subs client
          | Msg.RemoveClient(chan,client) -> handleRemoveClient chan state subs client

        return! act newstate
      }
    act initial

  [<RequireQualifiedAccess>]
  module ApiServer =

    let create (config: IrisConfig) =
      either {
        let cts = new CancellationTokenSource()
        let initial = { Clients = Map.empty }
        let subs = new Subscriptions()
        let agent = new ApiAgent(loop initial subs, cts.Token)

        return
          { new IClientApiServer with
              member self.Start () =
                agent.Start()
                |> Either.succeed

              member self.Dispose () =
                agent.PostAndReply(fun chan -> Msg.Dispose chan)
                |> ignore
                dispose cts
            }
      }
