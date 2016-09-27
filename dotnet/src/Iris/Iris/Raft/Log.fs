namespace Iris.Raft

open System
open System.Collections
open FlatBuffers
open Iris.Core
open Iris.Serialization.Raft

//  _
// | |    ___   __ _
// | |   / _ \ / _` |
// | |__| (_) | (_| |
// |_____\___/ \__, |
//             |___/

type Log =
  { Data  : LogEntry option
  ; Depth : Long
  ; Index : Index
  }

  with

    //  _____    ____  _        _
    // |_   _|__/ ___|| |_ _ __(_)_ __   __ _
    //   | |/ _ \___ \| __| '__| | '_ \ / _` |
    //   | | (_) |__) | |_| |  | | | | | (_| |
    //   |_|\___/____/ \__|_|  |_|_| |_|\__, |
    //                                  |___/

    override self.ToString() =
      let logstr =
        match self.Data with
        | Some data -> string data
        | _ -> "<empty>"

      sprintf "Index: %A Depth: %A\n%s"
        self.Index
        self.Depth
        logstr

    //  _____ _       _   ____         __  __
    // |  ___| | __ _| |_| __ ) _   _ / _|/ _| ___ _ __ ___
    // | |_  | |/ _` | __|  _ \| | | | |_| |_ / _ \ '__/ __|
    // |  _| | | (_| | |_| |_) | |_| |  _|  _|  __/ |  \__ \
    // |_|   |_|\__,_|\__|____/ \__,_|_| |_|  \___|_|  |___/

    member self.ToOffset(builder: FlatBufferBuilder) =
      match self.Data with
      | Some entries -> entries.ToOffset(builder)
      | _            -> [| |]

    static member FromFB (logs: LogFB array) : Log option =
      match LogEntry.FromFB logs with
      | Some entries ->
        Some { Data  = Some entries
             ; Depth = LogEntry.depth entries
             ; Index = LogEntry.index entries }
      | _ ->
        Some { Data  = None
             ; Depth = 0u
             ; Index = 0u }


    //  _                  __  __           _       _
    // | |    ___   __ _  |  \/  | ___   __| |_   _| | ___
    // | |   / _ \ / _` | | |\/| |/ _ \ / _` | | | | |/ _ \
    // | |__| (_) | (_| | | |  | | (_) | (_| | |_| | |  __/
    // |_____\___/ \__, | |_|  |_|\___/ \__,_|\__,_|_|\___|
    //             |___/

    /// Construct an empty Log
    static member empty
      with get () = { Depth = 0u
                    ; Index = 0u
                    ; Data  = None }

    static member fromEntries (entries: LogEntry) =
      { Depth = LogEntry.depth entries
      ; Index = LogEntry.index entries
      ; Data  = Some entries }

    /// compute the actual depth of the log (e.g. for compacting)
    static member length log = log.Depth

    /// Return the the current Index in the log
    static member index log = log.Index

    /// Return the index of the previous element
    static member prevIndex log =
      Option.bind LogEntry.prevIndex log.Data

    /// Return the Term of the latest log entry
    ///
    /// ### Complexity: 0(1)
    static member term log =
      match log.Data with
      | Some entries -> LogEntry.term entries
      | _            -> 0u

    /// Return the Term of the previous entry
    ///
    /// ### Complexity: 0(1)
    static member prevTerm log =
      Option.bind LogEntry.prevTerm log.Data

    /// Return the last Entry, if it exists
    ///
    /// ### Complexity: 0(1)
    static member previous log =
      match log.Data with
      | Some entries ->
        match LogEntry.prevEntry entries with
        | Some entry ->
          { Depth = LogEntry.depth entry
          ; Index = LogEntry.index entry
          ; Data  = Some entry }
          |> Some
        | _ -> None
      | _ -> None

    static member prevEntry log =
      Option.bind (LogEntry.prevEntry) log.Data

    static member foldLogL f m log =
      match log.Data with
      | Some entries -> LogEntry.foldl f m entries
      | _            -> m

    static member foldLogR f m log =
      match log.Data with
      | Some entries -> LogEntry.foldr f m entries
      | _            -> m

    static member at idx log =
      Option.bind (LogEntry.at idx) log.Data

    static member until idx log =
      Option.bind (LogEntry.until idx) log.Data

    static member untilExcluding idx log =
      Option.bind (LogEntry.untilExcluding idx) log.Data

    static member append newentries log : Log =
      match log.Data with
      | Some entries ->
        let newlog = LogEntry.append newentries entries
        { Index = LogEntry.index newlog
          Depth = LogEntry.depth newlog
          Data  = Some           newlog }
      | _ ->
        let entries = LogEntry.rewrite newentries
        { Index = LogEntry.index entries
          Depth = LogEntry.depth entries
          Data  = Some           entries }

    static member find id log =
      Option.bind (LogEntry.find id) log.Data

    static member make term data = LogEntry.make term data
    static member mkConfig term nodes = LogEntry.mkConfig term nodes
    static member mkConfigChange term old newer = LogEntry.mkConfigChange term old newer

    static member entries log = log.Data

    static member aggregate f m log =
      Option.map (LogEntry.aggregate f m) log.Data

    static member snapshot nodes data log =
      match log.Data with
        | Some entries ->
          let snapshot = LogEntry.snapshot nodes data entries
          { Index = LogEntry.index snapshot
            Depth = 1u
            Data = Some snapshot }
        | _ -> log

    static member head log =
      Option.map LogEntry.head log.Data

    static member lastTerm log =
      Option.bind LogEntry.lastTerm log.Data

    static member lastIndex log =
      Option.bind LogEntry.lastIndex log.Data

    /// Return the last entry in the chain of logs.
    static member last log =
      Option.map LogEntry.last log.Data

    /// Iterate over log entries, in order of newsest to oldest.
    static member iter f log =
      Option.map (LogEntry.iter f) log.Data
      |> ignore

    /// Retrieve the index of the first log entry for the given term. Return None
    /// if no result was found;
    static member firstIndex term log =
      Option.bind (LogEntry.firstIndex term) log.Data

    static member getn count log =
      Option.bind (LogEntry.getn count) log.Data

    static member contains (f: LogEntry -> bool) log : bool =
      match Option.map (LogEntry.contains f) log.Data with
      | Some result -> result
      | _           -> false

    static member map f log =
      Option.map (LogEntry.map f) log.Data
