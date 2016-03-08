namespace Iris.Raft.FFI

open System
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

[<AutoOpen>]
module internal Raft =

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  [<RequireQualifiedAccess>]
  type State =
    | NONE      = 0
    | FOLLOWER  = 1
    | CANDIDATE = 2
    | LEADER    = 3

  //  _               _____
  // | |    ___   __ |_   _|   _ _ __   ___
  // | |   / _ \ / _` || || | | | '_ \ / _ \
  // | |__| (_) | (_| || || |_| | |_) |  __/
  // |_____\___/ \__, ||_| \__, | .__/ \___|
  //             |___/     |___/|_|

  [<RequireQualifiedAccess>]
  type LogType =
    | NORMAL             = 0
    | ADD_NONVOTING_NODE = 1
    | ADD_NODE           = 2 
    | REMOVE_NODE        = 3
    | NUM                = 4

  type Server = IntPtr
  type Node = IntPtr

  [<Struct>]
  type EntryData =
    val mutable buf : IntPtr
    val mutable len : UInt32

  [<Struct>]
  type Entry =
    val mutable   term   : UInt32
    val mutable   id     : UInt32
    val mutable ``type`` : Int32
    val mutable   data   : EntryData

  type Msg = Entry

  [<Struct>]
  type MsgResponse =
    val mutable id   : UInt32
    val mutable term : Int32
    val mutable idx  : Int32
    

  [<Struct>]
  type VoteRequest =
    val mutable term          : Int32 // currentTerm, to force other leader/candidate to step down
    val mutable candidate_id  : Int32 // candidate requesting vote
    val mutable last_log_idx  : Int32 // index of candidate's last log entry
    val mutable last_log_term : Int32 // term of candidate's last log entry

  [<Struct>]
  type VoteResponse =
    val mutable term         : Int32
    val mutable vote_granted : Int32


  [<Struct>]
  type AppendEntries =
    val mutable term          : Int32  // currentTerm, to force other leader/candidate to step down 
    val mutable prev_log_idx  : Int32  // the index of the log just before the newest entry for the node who receives this message 
    val mutable prev_log_term : Int32  // the term of the log just before the newest entry for the node who receives this message
    val mutable leader_commit : Int32  // the index of the entry that has been appended to the majority of the cluster. Entries up to this index will be applied to the FSM 
    val mutable n_entries     : Int32  // number of entries within this message
    val mutable entries       : IntPtr // array of entries within this message

  [<Struct>]
  type AppendEntriesReponse =
    val mutable term        : Int32 // currentTerm, to force other leader/candidate to step down
    val mutable success     : Int32 // true if follower contained entry matching prevLogidx and prevLogTerm
    val mutable current_idx : Int32 // This is the highest log IDX we've received and appended to our log
    val mutable first_idx   : Int32 // The first idx that we received within the appendentries message

  type UserData = IntPtr

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type SendRequestVote =
    delegate of Server * UserData * Node * IntPtr -> int

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type SendAppendEntries =
    delegate of Server * UserData * Node * IntPtr -> int

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type NodeHasSufficientLogs =
    delegate of Server * UserData * Node -> unit

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type LogIt =
    delegate of Server * Node * UserData * String -> unit

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type ApplyLog =
    delegate of Server * UserData * Entry byref -> int

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type PersistInt =
    delegate of Server * UserData * Int32 -> int

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type LogEntryEvent =
    delegate of Server * UserData * Entry * Int32 -> int

  [<Struct>]
  type RaftCallbacks =
    val SendRequestVote   : SendRequestVote
    val SendAppendEntries : SendAppendEntries
    val ApplyLog          : ApplyLog
    val PersistVote       : PersistInt
    val PersistTerm       : PersistInt
    val LogOffer          : LogEntryEvent    
    val LogPoll           : LogEntryEvent
    val LogPop            : LogEntryEvent
    val HasSufficientLogs : NodeHasSufficientLogs
    val Log               : LogIt

  [<Struct>]
  type NodeConfig =
    val udata_address : IntPtr 

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_new")>]
  extern Server RaftNew()

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_free")>]
  extern unit DestroyRaft(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_callbacks")>]
  extern unit SetCallbacks(Server me, RaftCallbacks& funcs, UserData data)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_add_node")>]
  extern Node AddNode(Server me, UserData data, Int32 id, Int32 is_me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_add_node")>]
  extern Node AddPeer(Server me, UserData data, Int32 id, Int32 is_me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_add_non_voting_node")>]
  extern Node AddNonVotingNode(Server me, UserData data, Int32 id, bool is_me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_remove_node")>]
  extern unit RemoveNode(Server me, Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_election_timeout")>]
  extern unit SetElectionTimeout(Server me, Int32 msec)
  
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_request_timeout")>]
  extern unit SetRequestTimeout(Server me, Int32 msec)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_periodic")>]
  extern Int32 Periodic(Server me, Int32 msec_elapsed)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_appendentries")>]
  extern Int32 RecvAppendEntries(Server me, Node node, AppendEntries& e, AppendEntriesReponse& r)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_appendentries_response")>]
  extern Int32 RecvAppendEntriesResponse(Server me, Node node, AppendEntriesReponse& r)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_requestvote")>]
  extern Int32 RecvRequestVote(Server me, Node node, VoteRequest& r1, VoteResponse& r2)
  
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_requestvote_response")>]
  extern Int32 RecvRequestVoteResponse(Server me, Node node, VoteResponse& r2)
  
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_entry")>]
  extern Int32 RecvEntry(Server me, Entry& e1, MsgResponse& e2)
  
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_nodeid")>]
  extern Int32 GetNodeId(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_election_timeout")>]
  extern Int32 GetElectionTimeout(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_num_nodes")>]
  extern Int32 GetNumNodes(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_log_count")>]
  extern Int32 GetLogCount(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_term")>]
  extern Int32 GetCurrentTerm(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_idx")>]
  extern Int32 GetCurrentIdx(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_commit_idx")>]
  extern Int32 GetCommitIdx(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_is_follower")>]
  extern Int32 IsFollower(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_is_leader")>]
  extern Int32 IsLeader(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_is_candidate")>]
  extern Int32 IsCandidate(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_timeout_elapsed")>]
  extern Int32 GetTimeoutElapsed(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_request_timeout")>]
  extern Int32 GetRequestTimeout(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_last_applied_idx")>]
  extern Int32 GetLastAppliedIdx(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_is_leader")>]
  extern Int32 NodeIsLeader(Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_next_index")>]
  extern Int32 NodeGetNextIdx(Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_match_index")>]
  extern Int32 NodeGetMatchIdx(Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_udata")>]
  extern UserData NodeGetUserData(Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_set_udata")>]
  extern UserData NodeSetUserData(Node node, UserData data)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_entry_from_idx")>]
  extern Entry* GetEntryFromIdx(Server me, Int32 idx)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_node")>]
  extern Node GetNode(Server me, Int32 node_id)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_node_from_idx")>]
  extern Node GetNodeFromIdx(Server me, Int32 idx)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_nvotes_for_me")>]
  extern Int32 GetNVotesForMe(Server me, Int32 idx)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_voted_for")>]
  extern Int32 GetVotedFor(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_leader")>]
  extern Int32 GetCurrentLeader(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_leader_node")>]
  extern Node GetCurrentLeaderNode(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_udata")>]
  extern UserData GetUserData(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_my_id")>]
  extern Int32 GetMyId(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_vote")>]
  extern unit Vote(Server me, Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_vote_for_nodeid")>]
  extern unit VoteForNodeId(Server me, Int32 node_id)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_current_term")>]
  extern unit SetCurrentTerm(Server me, Int32 term)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_commit_idx")>]
  extern unit SetCommitIdx(Server me, Int32 idx)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_append_entry")>]
  extern Int32 AppendEntry(Server me, Entry& entry)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_msg_entry_response_committed")>]
  extern Int32 MsgEntryResponseCommitted(Server me, MsgResponse& resp)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_id")>]
  extern Int32 NodeGetId(Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_state")>]
  extern State GetState(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_last_log_term")>]
  extern Int32 GetLastLogTerm(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_set_voting")>]
  extern unit NodeSetVoting(Node node, Int32 voting)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_is_voting")>]
  extern Int32 NodeIsVoting(Node node)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_apply_all")>]
  extern unit ApplyAll(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_become_leader")>]
  extern unit BecomeLeader(Server me)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_entry_is_voting_cfg_change")>]
  extern Int32 EntryIsVotingCfgChange(Entry& entry)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_entry_is_cfg_change")>]
  extern Int32 EntryIsCfgChange(Entry& entry)
