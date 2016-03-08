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

  type UserData = IntPtr
  type Server = IntPtr
  type Node = IntPtr

  (* Data to be stored in an Entry *)
  [<Struct>]
  type EntryData =
    val mutable buf : IntPtr
    val mutable len : UInt32

  (* Entry that is stored in the server's entry log. *)
  [<Struct>]
  type Entry =
    val mutable   term   : UInt32 // the entry's term at the point it was created 
    val mutable   id     : UInt32 // the entry's unique ID
    val mutable ``type`` : Int32  // type of entry
    val mutable   data   : EntryData

  (* Message sent from client to server.
     The client sends this message to a server with the intention of having it
     applied to the FSM. *)
  type Msg = Entry

  (* Entry message response.
     Indicates to client if entry was committed or not. *)
  [<Struct>]
  type MsgResponse =
    val mutable id   : UInt32
    val mutable term : Int32
    val mutable idx  : Int32
    
  (* Vote request message.
     Sent to nodes when a server wants to become leader.
     This message could force a leader/candidate to become a follower. *)
  [<Struct>]
  type VoteRequest =
    val mutable term          : Int32 // currentTerm, to force other leader/candidate to step down
    val mutable candidate_id  : Int32 // candidate requesting vote
    val mutable last_log_idx  : Int32 // index of candidate's last log entry
    val mutable last_log_term : Int32 // term of candidate's last log entry

  (* Vote request response message.
     Indicates if node has accepted the server's vote request. *)
  [<Struct>]
  type VoteResponse =
    val mutable term         : Int32 // currentTerm, for candidate to update itself
    val mutable vote_granted : Int32 // true means candidate received vote 

  (* Appendentries message.
     This message is used to tell nodes if it's safe to apply entries to the FSM.
     Can be sent without any entries as a keep alive message.
     This message could force a leader/candidate to become a follower. *)
  [<Struct>]
  type AppendEntries =
    val mutable term          : Int32  // currentTerm, to force other leader/candidate to step down 
    val mutable prev_log_idx  : Int32  // the index of the log just before the newest entry for the node who receives this message 
    val mutable prev_log_term : Int32  // the term of the log just before the newest entry for the node who receives this message
    val mutable leader_commit : Int32  // the index of the entry that has been appended to the majority of the cluster. Entries up to this index will be applied to the FSM 
    val mutable n_entries     : Int32  // number of entries within this message
    val mutable entries       : IntPtr // array of entries within this message

  (* Appendentries response message.
     Can be sent without any entries as a keep alive message.
     This message could force a leader/candidate to become a follower. *)
  [<Struct>]
  type AppendEntriesReponse =
    val mutable term        : Int32 // currentTerm, to force other leader/candidate to step down
    val mutable success     : Int32 // true if follower contained entry matching prevLogidx and prevLogTerm
    val mutable current_idx : Int32 // This is the highest log IDX we've received and appended to our log
    val mutable first_idx   : Int32 // The first idx that we received within the appendentries message


  (** Callback for sending request vote messages.
    * @param[in] raft The Raft server making this callback
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] node The node's ID that we are sending this message to
    * @param[in] msg The request vote message to be sent
    * @return 0 on success *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type SendRequestVote =
    delegate of Server * UserData * Node * IntPtr -> int

  (** Callback for sending append entries messages.
    * @param[in] raft The Raft server making this callback
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] node The node's ID that we are sending this message to
    * @param[in] msg The appendentries message to be sent
    * @return 0 on success *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type SendAppendEntries =
    delegate of Server * UserData * Node * IntPtr -> int

  (** Callback for detecting when non-voting nodes have obtained enough logs.
    * This triggers only when there are no pending configuration changes.
    * @param[in] raft The Raft server making this callback
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] node The node *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type NodeHasSufficientLogs =
    delegate of Server * UserData * Node -> unit

  (** Callback for providing debug logging information.
    * This callback is optional
    * @param[in] raft The Raft server making this callback
    * @param[in] node The node that is the subject of this log. Could be NULL.
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] buf The buffer that was logged *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type LogIt =
    delegate of Server * Node * UserData * String -> unit

  (** Callback for applying this log entry to the state machine.
    * @param[in] raft The Raft server making this callback
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] ety Log entry to be applied
    * @return 0 on success *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type ApplyLog =
    delegate of Server * UserData * Entry byref -> int

  (** Callback for saving who we voted for to disk.
    * For safety reasons this callback MUST flush the change to disk.
    * @param[in] raft The Raft server making this callback
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] voted_for The node we voted for
    * @return 0 on success *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type PersistInt =
    delegate of Server * UserData * Int32 -> int

  (** Callback for saving log entry changes.
    *
    * This callback is used for:
    * <ul>
    *      <li>Adding entries to the log (ie. offer)
    *      <li>Removing the first entry from the log (ie. polling)
    *      <li>Removing the last entry from the log (ie. popping)
    * </ul>
    *
    * For safety reasons this callback MUST flush the change to disk.
    *
    * @param[in] raft The Raft server making this callback
    * @param[in] user_data User data that is passed from Raft server
    * @param[in] entry The entry that the event is happening to.
    *    The user is allowed to change the memory pointed to in the
    *    raft_entry_data_t struct. This MUST be done if the memory is temporary.
    * @param[in] entry_idx The entries index in the log
    * @return 0 on success *)
  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type LogEntryEvent =
    delegate of Server * UserData * Entry * Int32 -> int

  [<Struct>]
  type RaftCallbacks =
    (* Callback for sending request vote messages *)
    val SendRequestVote   : SendRequestVote
    (* Callback for sending appendentries messages *)
    val SendAppendEntries : SendAppendEntries
    (* Callback for finite state machine application *)
    val ApplyLog          : ApplyLog
    (* Callback for persisting vote data
     * For safety reasons this callback MUST flush the change to disk. *)
    val PersistVote       : PersistInt
    (* Callback for persisting term data
     * For safety reasons this callback MUST flush the change to disk. *)
    val PersistTerm       : PersistInt
    (* Callback for adding an entry to the log
     * For safety reasons this callback MUST flush the change to disk. *)
    val LogOffer          : LogEntryEvent    
    (* Callback for removing the oldest entry from the log
     * For safety reasons this callback MUST flush the change to disk.
     * @note If memory was malloc'd in log_offer then this should be the right
     *  time to free the memory. *)
    val LogPoll           : LogEntryEvent

    (* Callback for removing the youngest entry from the log
     * For safety reasons this callback MUST flush the change to disk.
     * @note If memory was malloc'd in log_offer then this should be the right
     *  time to free the memory. *)
    val LogPop            : LogEntryEvent
    (* Callback for detecting when a non-voting node has sufficient logs. *)
    val HasSufficientLogs : NodeHasSufficientLogs
    (* Callback for catching debugging log messages
     * This callback is optional *)
    val Log               : LogIt

  [<Struct>]
  type NodeConfig =
    (* User data pointer for addressing.
     * Examples of what this could be:
     * - void* pointing to implementor's networking data
     * - a (IP,Port) tuple *)
    val udata_address : IntPtr 

  (** Initialise a new Raft server.
    *
    * Request timeout defaults to 200 milliseconds
    * Election timeout defaults to 1000 milliseconds
    *
    * @return newly initialised Raft server *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_new")>]
  extern Server RaftNew()

  (** De-initialise Raft server.
    * Frees all memory *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_free")>]
  extern unit DestroyRaft(Server me)

  (** Set callbacks and user data.
    *
    * @param[in] funcs Callbacks
    * @param[in] user_data "User data" - user's context that's included in a callback *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_callbacks")>]
  extern unit SetCallbacks(Server me, RaftCallbacks& funcs, UserData data)

  (** Add node.
    *
    * @note This library does not yet support membership changes.
    *  Once raft_periodic has been run this will fail.
    *
    * @note The order this call is made is important.
    *  This call MUST be made in the same order as the other raft nodes.
    *  This is because the node ID is assigned depending on when this call is made
    *
    * @param[in] user_data The user data for the node.
    *  This is obtained using raft_node_get_udata.
    *  Examples of what this could be:
    *  - void* pointing to implementor's networking data
    *  - a (IP,Port) tuple
    * @param[in] id The integer ID of this node
    *  This is used for identifying clients across sessions.
    * @param[in] is_self Set to 1 if this "node" is this server
    * @return 0 on success; otherwise -1 *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_add_node")>]
  extern Node AddNode(Server me, UserData data, Int32 id, Int32 is_me)

  (** Add a node which does not participate in voting.
    * Parameters are identical to raft_add_node *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_add_node")>]
  extern Node AddPeer(Server me, UserData data, Int32 id, Int32 is_me)

  (** Remove node.
    * @param node The node to be removed. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_add_non_voting_node")>]
  extern Node AddNonVotingNode(Server me, UserData data, Int32 id, bool is_me)

  (** Set election timeout.
    * The amount of time that needs to elapse before we assume the leader is down
    * @param[in] msec Election timeout in milliseconds *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_remove_node")>]
  extern unit RemoveNode(Server me, Node node)

  (** Set request timeout in milliseconds.
    * The amount of time before we resend an appendentries message
    * @param[in] msec Request timeout in milliseconds *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_election_timeout")>]
  extern unit SetElectionTimeout(Server me, Int32 msec)
  
  (** Process events that are dependent on time passing.
    * @param[in] msec_elapsed Time in milliseconds since the last call
    * @return 0 on success *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_request_timeout")>]
  extern unit SetRequestTimeout(Server me, Int32 msec)

  (** Receive an appendentries message.
    *
    * Will block (ie. by syncing to disk) if we need to append a message.
    *
    * Might call malloc once to increase the log entry array size.
    *
    * The log_offer callback will be called.
    *
    * @note The memory pointer (ie. raft_entry_data_t) for each msg_entry_t is
    *   copied directly. If the memory is temporary you MUST either make the
    *   memory permanent (ie. via malloc) OR re-assign the memory within the
    *   log_offer callback.
    *
    * @param[in] node Index of the node who sent us this message
    * @param[in] ae The appendentries message
    * @param[out] r The resulting response
    * @return 0 on success *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_periodic")>]
  extern Int32 Periodic(Server me, Int32 msec_elapsed)

  (** Receive a response from an appendentries message we sent.
    * @param[in] node Index of the node who sent us this message
    * @param[in] r The appendentries response message
    * @return 0 on success *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_appendentries")>]
  extern Int32 RecvAppendEntries(Server me, Node node, AppendEntries& e, AppendEntriesReponse& r)

  (** Receive a requestvote message.
    * @param[in] node Index of the node who sent us this message
    * @param[in] vr The requestvote message
    * @param[out] r The resulting response
    * @return 0 on success *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_appendentries_response")>]
  extern Int32 RecvAppendEntriesResponse(Server me, Node node, AppendEntriesReponse& r)

  (** Receive a response from a requestvote message we sent.
    * @param[in] node The node this response was sent by
    * @param[in] r The requestvote response message
    * @return 0 on success *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_requestvote")>]
  extern Int32 RecvRequestVote(Server me, Node node, VoteRequest& r1, VoteResponse& r2)
  
  (** Receive an entry message from the client.
    *
    * Append the entry to the log and send appendentries to followers.
    *
    * Will block (ie. by syncing to disk) if we need to append a message.
    *
    * Might call malloc once to increase the log entry array size.
    *
    * The log_offer callback will be called.
    *
    * @note The memory pointer (ie. raft_entry_data_t) in msg_entry_t is
    *  copied directly. If the memory is temporary you MUST either make the
    *  memory permanent (ie. via malloc) OR re-assign the memory within the
    *  log_offer callback.
    *
    * Will fail:
    * <ul>
    *      <li>if the server is not the leader
    * </ul>
    *
    * @param[in] node Index of the node who sent us this message
    * @param[in] ety The entry message
    * @param[out] r The resulting response
    * @return 0 on success, -1 on failure *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_entry")>]
  extern Int32 RecvEntry(Server me, Entry& e1, MsgResponse& e2)

  (** Receive a response from a requestvote message we sent.
    * @param[in] node The node this response was sent by
    * @param[in] r The requestvote response message
    * @return 0 on success *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_recv_requestvote_response")>]
  extern Int32 RecvRequestVoteResponse(Server me, Node node, VoteResponse& r2)
  
  (* @return the server's node ID *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_nodeid")>]
  extern Int32 GetNodeId(Server me)

  (* @return currently configured election timeout in milliseconds *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_election_timeout")>]
  extern Int32 GetElectionTimeout(Server me)

  (* @return number of nodes that this server has *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_num_nodes")>]
  extern Int32 GetNumNodes(Server me)

  (* @return number of items within log *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_log_count")>]
  extern Int32 GetLogCount(Server me)

  (* @return current term *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_term")>]
  extern Int32 GetCurrentTerm(Server me)

  (* @return current log index *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_idx")>]
  extern Int32 GetCurrentIdx(Server me)

  (* @return commit index *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_commit_idx")>]
  extern Int32 GetCommitIdx(Server me)

  (* @return 1 if follower; 0 otherwise *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_is_follower")>]
  extern Int32 IsFollower(Server me)

  (* @return 1 if leader; 0 otherwise *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_is_leader")>]
  extern Int32 IsLeader(Server me)

  (* @return 1 if candidate; 0 otherwise *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_is_candidate")>]
  extern Int32 IsCandidate(Server me)

  (* @return currently elapsed timeout in milliseconds *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_timeout_elapsed")>]
  extern Int32 GetTimeoutElapsed(Server me)

  (* @return request timeout in milliseconds *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_request_timeout")>]
  extern Int32 GetRequestTimeout(Server me)

  (* @return index of last applied entry *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_last_applied_idx")>]
  extern Int32 GetLastAppliedIdx(Server me)

  (* @return 1 if node is leader; 0 otherwise *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_is_leader")>]
  extern Int32 NodeIsLeader(Node node)

  (* @return the node's next index *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_next_index")>]
  extern Int32 NodeGetNextIdx(Node node)

  (* @return this node's user data *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_match_index")>]
  extern Int32 NodeGetMatchIdx(Node node)

  (* @return this node's user data *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_udata")>]
  extern UserData NodeGetUserData(Node node)

  (* Set this node's user data *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_set_udata")>]
  extern UserData NodeSetUserData(Node node, UserData data)

  (**
    * @param[in] idx The entry's index
    * @return entry from index *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_entry_from_idx")>]
  extern Entry* GetEntryFromIdx(Server me, Int32 idx)

  (**
    * @param[in] node The node's ID
    * @return node pointed to by node ID *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_node")>]
  extern Node GetNode(Server me, Int32 node_id)

  (**
    * Used for iterating through nodes
    * @param[in] node The node's idx
    * @return node pointed to by node idx *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_node_from_idx")>]
  extern Node GetNodeFromIdx(Server me, Int32 idx)

  (* @return number of votes this server has received this election *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_nvotes_for_me")>]
  extern Int32 GetNVotesForMe(Server me, Int32 idx)

  (**
    * @return node ID of who I voted for *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_voted_for")>]
  extern Int32 GetVotedFor(Server me)

  (** Get what this node thinks the node ID of the leader is.
    * @return node of what this node thinks is the valid leader;
    *   -1 if the leader is unknown *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_leader")>]
  extern Int32 GetCurrentLeader(Server me)

  (** Get what this node thinks the node of the leader is.
    * @return node of what this node thinks is the valid leader;
    *   NULL if the leader is unknown *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_current_leader_node")>]
  extern Node GetCurrentLeaderNode(Server me)


  (** @return callback user data *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_udata")>]
  extern UserData GetUserData(Server me)

  (** @return this server's node ID *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_my_id")>]
  extern Int32 GetMyId(Server me)

  (** Vote for a server.
    * This should be used to reload persistent state, ie. the voted-for field.
    * @param[in] node The server to vote for *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_vote")>]
  extern unit Vote(Server me, Node node)

  (** Vote for a server.
    * This should be used to reload persistent state, ie. the voted-for field.
    * @param[in] nodeid The server to vote for by nodeid *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_vote_for_nodeid")>]
  extern unit VoteForNodeId(Server me, Int32 node_id)

  (** Set the current term.
    * This should be used to reload persistent state, ie. the current_term field.
    * @param[in] term The new current term *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_current_term")>]
  extern unit SetCurrentTerm(Server me, Int32 term)

  (** Set the commit idx.
    * This should be used to reload persistent state, ie. the commit_idx field.
    * @param[in] commit_idx The new commit index. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_set_commit_idx")>]
  extern unit SetCommitIdx(Server me, Int32 idx)

  (** Add an entry to the server's log.
    * This should be used to reload persistent state, ie. the commit log.
    * @param[in] ety The entry to be appended *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_append_entry")>]
  extern Int32 AppendEntry(Server me, Entry& entry)


  (** Confirm if a msg_entry_response has been committed.
    * @param[in] r The response we want to check *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_msg_entry_response_committed")>]
  extern Int32 MsgEntryResponseCommitted(Server me, MsgResponse& resp)

  (** Get node's ID.
    * @return ID of node *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_get_id")>]
  extern Int32 NodeGetId(Node node)

  (** Tell if we are a leader, candidate or follower.
    * @return get state of type raft_state_e. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_state")>]
  extern State GetState(Server me)

  (** The the most recent log's term
    * @return the last log term *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_get_last_log_term")>]
  extern Int32 GetLastLogTerm(Server me)

  (** Turn a node into a voting node.
    * Voting nodes can take part in elections and in-regards to commiting entries,
    * are counted in majorities. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_set_voting")>]
  extern unit NodeSetVoting(Node node, Int32 voting)

  (** Tell if a node is a voting node or not.
    * @return 1 if this is a voting node. Otherwise 0. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_node_is_voting")>]
  extern Int32 NodeIsVoting(Node node)


  (** Apply all entries up to the commit index *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_apply_all")>]
  extern unit ApplyAll(Server me)

  (** Become leader
    * WARNING: this is a dangerous function call. It could lead to your cluster
    * losing it's consensus guarantees. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_become_leader")>]
  extern unit BecomeLeader(Server me)


  (** Determine if entry is voting configuration change.
    * @param[in] ety The entry to query.
    * @return 1 if this is a voting configuration change. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_entry_is_voting_cfg_change")>]
  extern Int32 EntryIsVotingCfgChange(Entry& entry)

  (** Determine if entry is configuration change.
    * @param[in] ety The entry to query.
    * @return 1 if this is a configuration change. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="raft_entry_is_cfg_change")>]
  extern Int32 EntryIsCfgChange(Entry& entry)
