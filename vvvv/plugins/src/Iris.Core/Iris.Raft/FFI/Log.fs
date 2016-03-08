namespace Iris.Raft.FFI

open System
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

[<AutoOpen>]
module internal Log =
  
  type Log = IntPtr

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_new")>]
  extern Log LogNew()

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_set_callbacks")>]
  extern unit SetCallbacks(Log log, RaftCallbacks& funcs, Server srv)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_free")>]
  extern unit LogFree(Log log)

  (**
    * Add entry to log.
    * Don't add entry if we've already added this entry (based off ID)
    * Don't add entries with ID=0 
    * @return 0 if unsucessful; 1 otherwise *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_append_entry")>]
  extern Int32 LogAppendEntry(Log log, Entry& entry)

  (**
    * @return number of entries held within log *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_count")>]
  extern Int32 LogCount(Log log)

  (**
    * Delete all logs from this log onwards *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_delete")>]
  extern void LogDelete(Log log, Int32 idx)

  (**
    * Empty the queue. *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_empty")>]
  extern void LogEmpty(Log log)

  (**
    * Remove oldest entry
    * @return oldest entry *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_poll")>]
  extern void LogPoll(Log log)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_get_from_idx")>]
  extern Entry* LogGetFromIdx(Log log, Int32 idx, Int32 n_entries)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_get_at_idx")>]
  extern Entry* LogGetAtIdx(Log log, Int32 idx)

  (**
    * @return youngest entry *)
  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_peektail")>]
  extern Entry* LogPeekTail(Log log)

  [<DllImport(@"NativeBinaries/linux/amd64/libcraft.so", EntryPoint="log_get_current_idx")>]
  extern Int32 LogGetCurrentIdx(Log log)
