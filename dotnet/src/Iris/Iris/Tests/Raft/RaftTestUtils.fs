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

  /// Callbacks fired when instantiating the Raft
  [<NoEquality;NoComparison>]
  type Callbacks =
    { SendRequestVote     : RaftMember        -> VoteRequest     -> unit
      SendAppendEntries   : RaftMember        -> AppendEntries   -> unit
      SendInstallSnapshot : RaftMember        -> InstallSnapshot -> unit
      PersistSnapshot     : RaftLogEntry      -> unit
      PrepareSnapshot     : RaftValue         -> RaftLog option
      RetrieveSnapshot    : unit              -> RaftLogEntry option
      ApplyLog            : StateMachine      -> unit
      MemberAdded         : RaftMember        -> unit
      MemberUpdated       : RaftMember        -> unit
      MemberRemoved       : RaftMember        -> unit
      Configured          : RaftMember array  -> unit
      StateChanged        : RaftState         -> RaftState -> unit
      PersistVote         : RaftMember option -> unit
      PersistTerm         : Term              -> unit
      PersistLog          : RaftLogEntry      -> unit
      DeleteLog           : RaftLogEntry      -> unit
      LogMsg              : RaftMember        -> CallSite -> LogLevel -> String -> unit }

    interface IRaftCallbacks with
      member self.SendRequestVote mem req    = self.SendRequestVote mem req
      member self.SendAppendEntries mem ae   = self.SendAppendEntries mem ae
      member self.SendInstallSnapshot mem is = self.SendInstallSnapshot mem is
      member self.PersistSnapshot log        = self.PersistSnapshot log
      member self.PrepareSnapshot raft       = self.PrepareSnapshot raft
      member self.RetrieveSnapshot ()        = self.RetrieveSnapshot ()
      member self.ApplyLog log               = self.ApplyLog log
      member self.MemberAdded mem            = self.MemberAdded mem
      member self.MemberUpdated mem          = self.MemberUpdated mem
      member self.MemberRemoved mem          = self.MemberRemoved mem
      member self.Configured mems            = self.Configured mems
      member self.StateChanged olds news     = self.StateChanged olds news
      member self.PersistVote mem            = self.PersistVote mem
      member self.PersistTerm mem            = self.PersistTerm mem
      member self.PersistLog log             = self.PersistLog log
      member self.DeleteLog log              = self.DeleteLog log

    static member Create (data: StateMachine ref) =
      { SendRequestVote = fun n v ->
          v |> sprintf "%A" |> Logger.debug "SendRequestVote"

        SendAppendEntries = fun n ae ->
          string ae |> sprintf "%s" |> Logger.debug "SendAppendEntries"

        SendInstallSnapshot = fun (n: RaftMember) (is: InstallSnapshot) ->
          string is |> sprintf "%s" |> Logger.debug "SendInstall"

        PrepareSnapshot = fun raft ->
          Raft.createSnapshot raft !data |> Some

        PersistSnapshot = fun (entry: RaftLogEntry) ->
          sprintf "Perisisting Snapshot: %A" entry |> Logger.debug "PersistSnapshot"

        RetrieveSnapshot = fun () ->
          "Asked to retrieve last snapshot" |> Logger.debug "RetrieveSnapshot"
          None

        ApplyLog = fun en ->
          sprintf "%A" en |> Logger.debug "ApplyLog"

        MemberAdded = fun mem ->
          sprintf "Member added: %A" mem |> Logger.debug "MemberAdded"

        MemberUpdated = fun mem ->
          sprintf "Member updated: %A" mem |> Logger.debug "MemberUpdated"

        MemberRemoved = fun mem ->
          sprintf "Member removed: %A" mem |> Logger.debug "MemberRemoved"

        Configured = fun mems ->
          sprintf "Cluster configuration applied:\n%A" mems |> Logger.debug "Configured"

        StateChanged = fun _ _ ->
          sprintf "state changed" |> Logger.debug "StateChanged"

        PersistVote = fun n ->
          sprintf "%A" n |> Logger.debug "PeristVote"

        PersistTerm = fun t ->
          sprintf "%A" t |> Logger.debug "PeristVote"

        PersistLog = fun l ->
          l.ToString() |> sprintf "%s" |> Logger.debug "PersistLog"

        DeleteLog =  fun l ->
          l.ToString() |> sprintf "%s" |> Logger.debug "DeleteLog"

        LogMsg = fun (mem: RaftMember) site level str ->
          Logger.log level site str }

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
    let cbs = Callbacks.Create data :> IRaftCallbacks
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
    { Inbox: List<Msg> ref
      Outbox: List<Msg> ref }

    static member create =
      { Inbox  = ref List.empty<Msg>
        Outbox = ref List.empty<Msg> }

  let __append_msg (sender:Sender) (msg:Msg) =
    sender.Inbox  := msg :: !sender.Inbox
    sender.Outbox := msg :: !sender.Outbox

  let senderRequestVote (sender:Sender) (resp: VoteResponse option) (mem: RaftMember) req =
    let msg = RequestVote(mem.Id, req)
    __append_msg sender msg

  let senderAppendEntries (sender:Sender) (resp: AppendResponse option) (mem: RaftMember) ae =
    let msg = AppendEntries(mem.Id, ae)
    __append_msg sender msg

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
