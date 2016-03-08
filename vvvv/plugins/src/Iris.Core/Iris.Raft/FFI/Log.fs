namespace Iris.Raft.FFI

open System
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

[<AutoOpen>]
module internal Log =
  
  type Log = IntPtr

  [<DllImport(@"libcraft.so", EntryPoint="log_new")>]
  extern Log LogNew()

  [<DllImport(@"libcraft.so", EntryPoint="log_set_callbacks")>]
  extern unit SetCallbacks(Log log, RaftCallbacks funcs, Server srv)

  [<DllImport(@"libcraft.so", EntryPoint="log_free")>]
  extern unit LogFree(Log log)

  [<DllImport(@"libcraft.so", EntryPoint="log_append_entry")>]
  extern Int32 AppendLogEntry(Log log, Entry entry)

  [<DllImport(@"libcraft.so", EntryPoint="log_count")>]
  extern Int32 LogCount(Log log)

  [<DllImport(@"libcraft.so", EntryPoint="log_delete")>]
  extern void LogDelete(Log log, Int32 idx)

  [<DllImport(@"libcraft.so", EntryPoint="log_empty")>]
  extern void LogEmpty(Log log)

  [<DllImport(@"libcraft.so", EntryPoint="log_poll")>]
  extern void LogPoll(Log log)

  [<DllImport(@"libcraft.so", EntryPoint="log_get_from_idx")>]
  extern Entry LogGetFromIdx(Log log, Int32 idx, Int32 n_entries)

  [<DllImport(@"libcraft.so", EntryPoint="log_get_at_idx")>]
  extern Entry LogGetAtIdx(Log log, Int32 idx)

  [<DllImport(@"libcraft.so", EntryPoint="log_peektail")>]
  extern Entry LogPeekTail(Log log)

  [<DllImport(@"libcraft.so", EntryPoint="log_get_current_idx")>]
  extern Int32 LogGetCurrentIdx(Log log)
