namespace Pallet.Tests

open System
open System.Net
open Fuchu
open Fuchu.Test
open Pallet.Core

[<AutoOpen>]
module Util =
  ////////////////////////////////////////
  //  _   _ _   _ _                     //
  // | | | | |_(_) |___                 //
  // | | | | __| | / __|                //
  // | |_| | |_| | \__ \                //
  //  \___/ \__|_|_|___/                //
  ////////////////////////////////////////

  /// Callbacks fired when instantiating the Raft
  type Callbacks<'a,'b> =
    { SendRequestVote     : Node<'b>        -> VoteRequest<'b>        -> unit
    ; SendAppendEntries   : Node<'b>        -> AppendEntries<'a,'b>   -> unit
    ; SendInstallSnapshot : Node<'b>        -> InstallSnapshot<'a,'b> -> unit
    ; PersistSnapshot     : LogEntry<'a,'b> -> unit
    ; PrepareSnapshot     : Raft<'a,'b>     -> Log<'a,'b>
    ; RetrieveSnapshot    : unit            -> LogEntry<'a,'b> option
    ; ApplyLog            : 'a              -> unit
    ; NodeAdded           : Node<'b>        -> unit
    ; NodeUpdated         : Node<'b>        -> unit
    ; NodeRemoved         : Node<'b>        -> unit
    ; Configured          : Node<'b> array  -> unit
    ; StateChanged        : RaftState       -> RaftState              -> unit
    ; PersistVote         : Node<'b> option -> unit
    ; PersistTerm         : Term            -> unit
    ; PersistLog          : LogEntry<'a,'b> -> unit
    ; DeleteLog           : LogEntry<'a,'b> -> unit
    ; HasSufficientLogs   : Node<'b>        -> unit
    ; LogMsg              : Node<'b>        -> String                 -> unit
    }

    interface IRaftCallbacks<'a,'b> with
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
      member self.HasSufficientLogs node      = self.HasSufficientLogs node
      member self.LogMsg node str             = self.LogMsg node str

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

  let mk_cbs (data: 'd ref) =
    let OnSendRequestVote n v =
      sprintf "SendRequestVote: %A" v |> log

    let OnSendAppendEntries n ae =
      ae.ToString()
      |> sprintf "SendAppendEntries: %s"
      |> log

    let OnSendInstallSnapshot n is =
      is.ToString()
      |> sprintf "SendInstall: %s"
      |> log

    let OnApplyLog en =
      sprintf "ApplyLog: %A" en
      |> log

    let OnPersistVote (n : Node<'node> option) =
      sprintf "PeristVote: %A" n
      |> log

    let OnPersistTerm n =
      sprintf "PeristVote: %A" n
      |> log

    let OnPersistLog (l : LogEntry<'data,'node>) =
      l.ToString()
      |> sprintf "LogOffer: %s"
      |> log

    let OnDeleteLog (l : LogEntry<'data,'node>) =
      l.ToString()
      |> sprintf "LogPoll: %s"
      |> log

    let OnHasSufficientLogs n =
      n.ToString()
      |> sprintf "HasSufficientLogs: %s"
      |> log

    let OnLogMsg n s =
      sprintf "logMsg: %s" s
      |> log

    let OnPrepareSnapshot raft =
      createSnapshot !data raft

    let OnPersistSnapshot (entry: LogEntry<_,_>) =
      sprintf "Perisisting Snapshot: %A" entry
      |> log

    let OnRetrieveSnapshot _ =
      "Asked to retrieve last snapshot"
      |> log
      None

    let OnNodeAdded node =
      sprintf "Node added: %A" node
      |> log

    let OnNodeRemoved node =
      sprintf "Node removed: %A" node
      |> log

    let OnNodeUpdated node =
      sprintf "Node updated: %A" node
      |> log

    let OnConfigured nodes =
      sprintf "Cluster configuration applied:\n%A" nodes
      |> log

    let OnStateChanged olds news =
      sprintf "state changed"
      |> log

    { SendRequestVote     = OnSendRequestVote
    ; SendAppendEntries   = OnSendAppendEntries
    ; SendInstallSnapshot = OnSendInstallSnapshot
    ; PrepareSnapshot     = OnPrepareSnapshot
    ; PersistSnapshot     = OnPersistSnapshot
    ; RetrieveSnapshot    = OnRetrieveSnapshot
    ; ApplyLog            = OnApplyLog
    ; NodeAdded           = OnNodeAdded
    ; NodeUpdated         = OnNodeUpdated
    ; NodeRemoved         = OnNodeRemoved
    ; Configured          = OnConfigured
    ; StateChanged        = OnStateChanged
    ; PersistVote         = OnPersistVote
    ; PersistTerm         = OnPersistTerm
    ; PersistLog          = OnPersistLog
    ; DeleteLog           = OnDeleteLog
    ; HasSufficientLogs   = OnHasSufficientLogs
    ; LogMsg              = OnLogMsg
    }

  let defaultServer (data : 'v) =
    let self : Node<'v> = Node.create (RaftId.Create()) data
    Raft.create self

  let runWithData data action =
    let self = Node.create (RaftId.Create()) ()
    let raft = Raft.create self
    let cbs = mk_cbs data :> IRaftCallbacks<_,_>
    runRaft raft cbs action

  let runWithDefaults action = runWithData (ref ()) action

  type Msg<'data,'node> =
    | RequestVote           of sender:NodeId * req:VoteRequest<'node>
    | RequestVoteResponse   of sender:NodeId * vote:VoteResponse
    | AppendEntries         of sender:NodeId * ae:AppendEntries<'data,'node>
    | AppendEntriesResponse of sender:NodeId * ar:AppendResponse

  type Sender<'data,'node> =
    { Inbox     : List<Msg<'data,'node>>  ref
    ; Outbox    : List<Msg<'data,'node>>  ref
    }

  [<RequireQualifiedAccess>]
  module Sender =
    let create<'a,'b> =
      { Inbox  = ref List.empty<Msg<'a,'b>>
      ; Outbox = ref List.empty<Msg<'a,'b>>
      }

  let __append_msg (sender:Sender<_,_>) (msg:Msg<_,_>) =
    sender.Inbox := msg :: !sender.Inbox
    sender.Outbox := msg :: !sender.Outbox

  let senderRequestVote (sender:Sender<_,_>) (node:Node<_>) req =
    let msg = RequestVote(node.Id, req)
    __append_msg sender msg

  let senderAppendEntries (sender:Sender<_,_>) (node:Node<_>) ae =
    let msg = AppendEntries(node.Id, ae)
    __append_msg sender msg

  let getVote = function
    | RequestVote(_,req) -> req
    | _ -> failwith "not a vote request"

  let getAppendEntries = function
    | AppendEntries(_,ae) -> ae
    | _ -> failwith "not a vote request"

  let getTerm (vote:VoteRequest<_>) = vote.Term

  let konst a _ = a

  let runWithRaft r c m = runRaft r c m

  let expectError e = function
    | Left (e',_) -> if e <> e' then failwithf "Unexpected error: %A" e'
    | _ as v -> failwithf "Expected error but got: %A" v

  let noError = function
    | Left (e,_) -> failwithf "ERROR: %A" e
    | _ -> ()
