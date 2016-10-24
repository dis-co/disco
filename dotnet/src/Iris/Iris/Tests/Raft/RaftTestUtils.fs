namespace Iris.Tests.Raft

open System
open System.Net
open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Raft


[<AutoOpen>]
module RaftTestUtils =
  ////////////////////////////////////////
  //  _   _ _   _ _                     //
  // | | | | |_(_) |___                 //
  // | | | | __| | / __|                //
  // | |_| | |_| | \__ \                //
  //  \___/ \__|_|_|___/                //
  ////////////////////////////////////////

  /// Callbacks fired when instantiating the Raft
  [<NoEquality;NoComparison>]
  type Callbacks =
    { SendRequestVote     : RaftNode        -> VoteRequest            -> VoteResponse option
    ; SendAppendEntries   : RaftNode        -> AppendEntries          -> AppendResponse option
    ; SendInstallSnapshot : RaftNode        -> InstallSnapshot        -> AppendResponse option
    ; PersistSnapshot     : LogEntry        -> unit
    ; PrepareSnapshot     : RaftValue       -> Log option
    ; RetrieveSnapshot    : unit            -> LogEntry option
    ; ApplyLog            : StateMachine    -> unit
    ; NodeAdded           : RaftNode        -> unit
    ; NodeUpdated         : RaftNode        -> unit
    ; NodeRemoved         : RaftNode        -> unit
    ; Configured          : RaftNode array  -> unit
    ; StateChanged        : RaftState       -> RaftState              -> unit
    ; PersistVote         : RaftNode option -> unit
    ; PersistTerm         : Term            -> unit
    ; PersistLog          : LogEntry        -> unit
    ; DeleteLog           : LogEntry        -> unit
    ; LogMsg              : LogLevel        -> RaftNode  -> String     -> unit
    }

    interface IRaftCallbacks with
      member self.SendRequestVote node req    = self.SendRequestVote node req
      member self.SendAppendEntries node ae   = self.SendAppendEntries node ae
      member self.SendInstallSnapshot node is = self.SendInstallSnapshot node is
      member self.PersistSnapshot log         = self.PersistSnapshot log
      member self.PrepareSnapshot raft        = self.PrepareSnapshot raft
      member self.RetrieveSnapshot ()         = self.RetrieveSnapshot ()
      member self.ApplyLog log                = self.ApplyLog log
      member self.NodeAdded node              = self.NodeAdded node
      member self.NodeUpdated node            = self.NodeUpdated node
      member self.NodeRemoved node            = self.NodeRemoved node
      member self.Configured nodes            = self.Configured nodes
      member self.StateChanged olds news      = self.StateChanged olds news
      member self.PersistVote node            = self.PersistVote node
      member self.PersistTerm node            = self.PersistTerm node
      member self.PersistLog log              = self.PersistLog log
      member self.DeleteLog log               = self.DeleteLog log
      member self.LogMsg level node str       = self.LogMsg level node str

  let log str =
    // printfn "%s" str
    ignore str

  /// abstract over Assert.Equal to create pipe-lineable assertions
  let expect (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Assert.Equal(msg, a, b t) // apply t to b

  let assume (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Assert.Equal(msg, a, b t) // apply t to b
    t

  let expectM (msg: string) (a: 'a) (b: 't -> 'a) =
    get >>= fun thing ->
      Assert.Equal(msg, a, b thing)
      returnM ()

  let mkcbs (data: StateMachine ref) =
    let onSendRequestVote n v =
      sprintf "SendRequestVote: %A" v |> log
      None

    let onSendAppendEntries n ae =
      string ae
      |> sprintf "SendAppendEntries: %s"
      |> log
      None

    let onSendInstallSnapshot n is =
      string is
      |> sprintf "SendInstall: %s"
      |> log
      None

    let onApplyLog en =
      sprintf "ApplyLog: %A" en
      |> log

    let onPersistVote (n : RaftNode option) =
      sprintf "PeristVote: %A" n
      |> log

    let onPersistTerm n =
      sprintf "PeristVote: %A" n
      |> log

    let onPersistLog (l : LogEntry) =
      l.ToString()
      |> sprintf "LogOffer: %s"
      |> log

    let onDeleteLog (l : LogEntry) =
      l.ToString()
      |> sprintf "LogPoll: %s"
      |> log

    let onLogMsg l n s =
      sprintf "logMsg: %s" s
      |> log

    let onPrepareSnapshot raft =
      Raft.createSnapshot !data raft |> Some

    let onPersistSnapshot (entry: LogEntry) =
      sprintf "Perisisting Snapshot: %A" entry
      |> log

    let onRetrieveSnapshot _ =
      "Asked to retrieve last snapshot"
      |> log
      None

    let onNodeAdded node =
      sprintf "Node added: %A" node
      |> log

    let onNodeRemoved node =
      sprintf "Node removed: %A" node
      |> log

    let onNodeUpdated node =
      sprintf "Node updated: %A" node
      |> log

    let onConfigured nodes =
      sprintf "Cluster configuration applied:\n%A" nodes
      |> log

    let onStateChanged olds news =
      sprintf "state changed"
      |> log

    { SendRequestVote     = onSendRequestVote
    ; SendAppendEntries   = onSendAppendEntries
    ; SendInstallSnapshot = onSendInstallSnapshot
    ; PrepareSnapshot     = onPrepareSnapshot
    ; PersistSnapshot     = onPersistSnapshot
    ; RetrieveSnapshot    = onRetrieveSnapshot
    ; ApplyLog            = onApplyLog
    ; NodeAdded           = onNodeAdded
    ; NodeUpdated         = onNodeUpdated
    ; NodeRemoved         = onNodeRemoved
    ; Configured          = onConfigured
    ; StateChanged        = onStateChanged
    ; PersistVote         = onPersistVote
    ; PersistTerm         = onPersistTerm
    ; PersistLog          = onPersistLog
    ; DeleteLog           = onDeleteLog
    ; LogMsg              = onLogMsg
    }

  let defaultServer _ =
    let self : RaftNode = Node.create (Id.Create())
    Raft.mkRaft self

  let runWithCBS cbs action =
    let self = Node.create (Id.Create())
    let raft = Raft.mkRaft self
    runRaft raft cbs action

  let runWithData data action =
    let self = Node.create (Id.Create())
    let raft = Raft.mkRaft self
    let cbs = mkcbs data :> IRaftCallbacks
    runRaft raft cbs action

  let defSM = StateMachine.DataSnapshot State.Empty

  let runWithDefaults action =
    let orv _ _ = None
    let oae _ _ = None
    let ois _ _ = None
    runWithData (ref defSM) action

  type Msg =
    | RequestVote           of sender:NodeId * req:VoteRequest
    | RequestVoteResponse   of sender:NodeId * vote:VoteResponse
    | AppendEntries         of sender:NodeId * ae:AppendEntries
    | AppendEntriesResponse of sender:NodeId * ar:AppendResponse

  type Sender =
    { Inbox     : List<Msg>  ref
    ; Outbox    : List<Msg>  ref
    }

    static member create =
      { Inbox  = ref List.empty<Msg>
      ; Outbox = ref List.empty<Msg> }

  let __append_msg (sender:Sender) (msg:Msg) =
    sender.Inbox  := msg :: !sender.Inbox
    sender.Outbox := msg :: !sender.Outbox

  let senderRequestVote (sender:Sender) (resp: VoteResponse option) (node: RaftNode) req =
    let msg = RequestVote(node.Id, req)
    __append_msg sender msg
    resp

  let senderAppendEntries (sender:Sender) (resp: AppendResponse option) (node: RaftNode) ae =
    let msg = AppendEntries(node.Id, ae)
    __append_msg sender msg
    resp

  let getVote = function
    | RequestVote(_,req) -> req
    | _ -> failwith "not a vote request"

  let getAppendEntries = function
    | AppendEntries(_,ae) -> ae
    | _ -> failwith "not a vote request"

  let getTerm (vote:VoteRequest) = vote.Term

  let konst a _ = a

  let runWithRaft r c m = runRaft r c m

  let expectError e = function
    | Left (e',_) -> if e <> e' then failwithf "Unexpected error: %A" e'
    | _ as v -> failwithf "Expected error but got: %A" v

  let noError = function
    | Left (e,_) -> failwithf "ERROR: %A" e
    | _ -> ()
