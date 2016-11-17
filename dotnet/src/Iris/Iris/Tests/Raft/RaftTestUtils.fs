namespace Iris.Tests.Raft

open System
open System.Net
open Expecto
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
    ; PersistSnapshot     : RaftLogEntry    -> unit
    ; PrepareSnapshot     : RaftValue       -> RaftLog option
    ; RetrieveSnapshot    : unit            -> RaftLogEntry option
    ; ApplyLog            : StateMachine    -> unit
    ; NodeAdded           : RaftNode        -> unit
    ; NodeUpdated         : RaftNode        -> unit
    ; NodeRemoved         : RaftNode        -> unit
    ; Configured          : RaftNode array  -> unit
    ; StateChanged        : RaftState       -> RaftState              -> unit
    ; PersistVote         : RaftNode option -> unit
    ; PersistTerm         : Term            -> unit
    ; PersistLog          : RaftLogEntry    -> unit
    ; DeleteLog           : RaftLogEntry    -> unit
    ; LogMsg              : RaftNode        -> CallSite -> LogLevel -> String -> unit
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
      member self.LogMsg node site level str  = self.LogMsg node site level str

  /// abstract over Assert.Equal to create pipe-lineable assertions
  let expect (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Expect.equal (b t) a msg // apply t to b

  let assume (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Expect.equal (b t) a msg // apply t to b
    t

  let expectM (msg: string) (a: 'a) (b: 't -> 'a) =
    get >>= fun thing ->
      Expect.equal (b thing) a msg
      returnM ()

  let mkcbs (data: StateMachine ref) =
    let onSendRequestVote (n: RaftNode) (v: VoteRequest) =
      sprintf "%A" v
      |> Logger.debug n.Id "SendRequestVote"
      None

    let onSendAppendEntries (n: RaftNode) (ae: AppendEntries) =
      string ae
      |> sprintf "%s"
      |> Logger.debug n.Id "SendAppendEntries"
      None

    let onSendInstallSnapshot (n: RaftNode) (is: InstallSnapshot) =
      string is
      |> sprintf "%s"
      |> Logger.debug n.Id "SendInstall"
      None

    let onApplyLog en =
      sprintf "%A" en
      |> Logger.debug (Id "<unknown>") "ApplyLog"

    let onPersistVote (n : RaftNode option) =
      sprintf "%A" n
      |> Logger.debug (Id "<fixme>") "PeristVote"

    let onPersistTerm (t: Term) =
      sprintf "%A" t
      |> Logger.debug (Id "<unknown>") "PeristVote"

    let onPersistLog (l : RaftLogEntry) =
      l.ToString()
      |> sprintf "%s"
      |> Logger.debug (Id "<unknown>") "PersistLog"

    let onDeleteLog (l : RaftLogEntry) =
      l.ToString()
      |> sprintf "%s"
      |> Logger.debug (Id "<unknown>") "DeleteLog"

    let onLogMsg (node: RaftNode) site level str =
      Logger.log level node.Id site str

    let onPrepareSnapshot raft =
      Raft.createSnapshot !data raft |> Some

    let onPersistSnapshot (entry: RaftLogEntry) =
      sprintf "Perisisting Snapshot: %A" entry
      |> Logger.debug (Id "<unknown>") "PersistSnapshot"

    let onRetrieveSnapshot _ =
      "Asked to retrieve last snapshot"
      |> Logger.debug (Id "<unknown>") "RetrieveSnapshot"
      None

    let onNodeAdded node =
      sprintf "Node added: %A" node
      |> Logger.debug (Id "<unknown>") "NodeAdded"

    let onNodeRemoved node =
      sprintf "Node removed: %A" node
      |> Logger.debug (Id "<unknown>") "NodeRemoved"

    let onNodeUpdated node =
      sprintf "Node updated: %A" node
      |> Logger.debug (Id "<unknown>") "NodeUpdated"

    let onConfigured nodes =
      sprintf "Cluster configuration applied:\n%A" nodes
      |> Logger.debug (Id "<unknown>") "Configured"

    let onStateChanged olds news =
      sprintf "state changed"
      |> Logger.debug (Id "<unknown>") "StateChanged"

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
