namespace Iris.Raft

open System
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

module private RawRaft =

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
  type Log =
    delegate of Server * Node * UserData * String -> unit

  [<UnmanagedFunctionPointer(CallingConvention.Cdecl)>]
  type ApplyLog =
    delegate of Server * UserData * Entry -> int

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
    val Log               : Log

  [<Struct>]
  type NodeConfig =
    val udata_address : IntPtr 
  

  [<RequireQualifiedAccess>]
  type State =
    | NONE      = 0
    | FOLLOWER  = 1
    | CANDIDATE = 2
    | LEADER    = 3

  [<RequireQualifiedAccess>]
  type LogType =
    | NORMAL             = 0
    | ADD_NONVOTING_NODE = 1
    | ADD_NODE           = 2 
    | REMOVE_NODE        = 3
    | NUM                = 4

  [<DllImport(@"libcraft.so", EntryPoint="raft_new")>]
  extern Server CreateServer()

  [<DllImport(@"libcraft.so", EntryPoint="raft_free")>]
  extern unit DestroyServer(Server me)

  [<DllImport(@"libcraft.so", EntryPoint="raft_add_node")>]
  extern Node AddNode(Server me, IntPtr user_data, Int32 id, bool is_me)

[<AutoOpen>]
module Raft =

  type Raft = string

  let Create() : Raft = "thing"
