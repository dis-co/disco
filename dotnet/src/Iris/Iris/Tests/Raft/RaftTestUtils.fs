namespace Iris.Tests.Raft

open System
open System.Net
open Expecto
open Iris.Core
open Iris.Raft
open Iris.Tests


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
    { SendRequestVote     : RaftMember        -> VoteRequest            -> VoteResponse option
    ; SendAppendEntries   : RaftMember        -> AppendEntries          -> AppendResponse option
    ; SendInstallSnapshot : RaftMember        -> InstallSnapshot        -> AppendResponse option
    ; PersistSnapshot     : RaftLogEntry    -> unit
    ; PrepareSnapshot     : RaftValue       -> RaftLog option
    ; RetrieveSnapshot    : unit            -> RaftLogEntry option
    ; ApplyLog            : StateMachine    -> unit
    ; MemberAdded         : RaftMember        -> unit
    ; MemberUpdated       : RaftMember        -> unit
    ; MemberRemoved       : RaftMember        -> unit
    ; Configured          : RaftMember array  -> unit
    ; StateChanged        : RaftState       -> RaftState              -> unit
    ; PersistVote         : RaftMember option -> unit
    ; PersistTerm         : Term            -> unit
    ; PersistLog          : RaftLogEntry    -> unit
    ; DeleteLog           : RaftLogEntry    -> unit
    ; LogMsg              : RaftMember        -> CallSite -> LogLevel -> String -> unit
    }

    interface IRaftCallbacks with
      member self.SendRequestVote mem req    = self.SendRequestVote mem req
      member self.SendAppendEntries mem ae   = self.SendAppendEntries mem ae
      member self.SendInstallSnapshot mem is = self.SendInstallSnapshot mem is
      member self.PersistSnapshot log         = self.PersistSnapshot log
      member self.PrepareSnapshot raft        = self.PrepareSnapshot raft
      member self.RetrieveSnapshot ()         = self.RetrieveSnapshot ()
      member self.ApplyLog log                = self.ApplyLog log
      member self.MemberAdded mem            = self.MemberAdded mem
      member self.MemberUpdated mem          = self.MemberUpdated mem
      member self.MemberRemoved mem          = self.MemberRemoved mem
      member self.Configured mems            = self.Configured mems
      member self.StateChanged olds news      = self.StateChanged olds news
      member self.PersistVote mem            = self.PersistVote mem
      member self.PersistTerm mem            = self.PersistTerm mem
      member self.PersistLog log              = self.PersistLog log
      member self.DeleteLog log               = self.DeleteLog log

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
    let onSendRequestVote (n: RaftMember) (v: VoteRequest) =
      v
      |> sprintf "%A"
      |> Logger.debug "SendRequestVote"
      None

    let onSendAppendEntries (n: RaftMember) (ae: AppendEntries) =
      string ae
      |> sprintf "%s"
      |> Logger.debug "SendAppendEntries"
      None

    let onSendInstallSnapshot (n: RaftMember) (is: InstallSnapshot) =
      string is
      |> sprintf "%s"
      |> Logger.debug "SendInstall"
      None

    let onApplyLog en =
      sprintf "%A" en
      |> Logger.debug "ApplyLog"

    let onPersistVote (n : RaftMember option) =
      sprintf "%A" n
      |> Logger.debug "PeristVote"

    let onPersistTerm (t: Term) =
      sprintf "%A" t
      |> Logger.debug "PeristVote"

    let onPersistLog (l : RaftLogEntry) =
      l.ToString()
      |> sprintf "%s"
      |> Logger.debug "PersistLog"

    let onDeleteLog (l : RaftLogEntry) =
      l.ToString()
      |> sprintf "%s"
      |> Logger.debug "DeleteLog"

    let onLogMsg (mem: RaftMember) site level str =
      Logger.log level site str

    let onPrepareSnapshot raft =
      Raft.createSnapshot !data raft |> Some

    let onPersistSnapshot (entry: RaftLogEntry) =
      sprintf "Perisisting Snapshot: %A" entry
      |> Logger.debug "PersistSnapshot"

    let onRetrieveSnapshot _ =
      "Asked to retrieve last snapshot"
      |> Logger.debug "RetrieveSnapshot"
      None

    let onMemberAdded mem =
      sprintf "Member added: %A" mem
      |> Logger.debug "MemberAdded"

    let onMemberRemoved mem =
      sprintf "Member removed: %A" mem
      |> Logger.debug "MemberRemoved"

    let onMemberUpdated mem =
      sprintf "Member updated: %A" mem
      |> Logger.debug "MemberUpdated"

    let onConfigured mems =
      sprintf "Cluster configuration applied:\n%A" mems
      |> Logger.debug "Configured"

    let onStateChanged olds news =
      sprintf "state changed"
      |> Logger.debug "StateChanged"

    { SendRequestVote     = onSendRequestVote
      SendAppendEntries   = onSendAppendEntries
      SendInstallSnapshot = onSendInstallSnapshot
      PrepareSnapshot     = onPrepareSnapshot
      PersistSnapshot     = onPersistSnapshot
      RetrieveSnapshot    = onRetrieveSnapshot
      ApplyLog            = onApplyLog
      MemberAdded         = onMemberAdded
      MemberUpdated       = onMemberUpdated
      MemberRemoved       = onMemberRemoved
      Configured          = onConfigured
      StateChanged        = onStateChanged
      PersistVote         = onPersistVote
      PersistTerm         = onPersistTerm
      PersistLog          = onPersistLog
      DeleteLog           = onDeleteLog
      LogMsg              = onLogMsg }

  let defaultServer _ =
    let self : RaftMember = Member.create (Id.Create())
    Raft.mkRaft self

  let runWithCBS cbs action =
    let self = Member.create (Id.Create())
    let raft = Raft.mkRaft self
    runRaft raft cbs action

  let runWithData data action =
    let self = Member.create (Id.Create())
    let raft = Raft.mkRaft self
    let cbs = mkcbs data :> IRaftCallbacks
    runRaft raft cbs action

  let defSM =
    mkTmpDir()
    |> mkState
    |> Either.get
    |> StateMachine.DataSnapshot

  let runWithDefaults action =
    let orv _ _ = None
    let oae _ _ = None
    let ois _ _ = None
    runWithData (ref defSM) action

  type Msg =
    | RequestVote           of sender:MemberId * req:VoteRequest
    | RequestVoteResponse   of sender:MemberId * vote:VoteResponse
    | AppendEntries         of sender:MemberId * ae:AppendEntries
    | AppendEntriesResponse of sender:MemberId * ar:AppendResponse

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

  let senderRequestVote (sender:Sender) (resp: VoteResponse option) (mem: RaftMember) req =
    let msg = RequestVote(mem.Id, req)
    __append_msg sender msg
    resp

  let senderAppendEntries (sender:Sender) (resp: AppendResponse option) (mem: RaftMember) ae =
    let msg = AppendEntries(mem.Id, ae)
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
    | Left (e',_) when e = e' -> ()
    | Left (e',_) when e <> e' ->
      Expecto.Tests.failtestf "Expected error: %A but got: %A" e e'
    | _ as v ->
      Expecto.Tests.failtestf "Expected error but got: %A" v

  let noError = function
    | Left (e,_) -> failwithf "ERROR: %A" e
    | _ -> ()
