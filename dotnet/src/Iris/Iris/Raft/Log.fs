namespace Iris.Raft

open System
open System.Collections
open FlatBuffers
open Iris.Core
open Iris.Serialization.Raft
open SharpYaml.Serialization

// __   __              _    ___  _     _           _
// \ \ / /_ _ _ __ ___ | |  / _ \| |__ (_) ___  ___| |_
//  \ V / _` | '_ ` _ \| | | | | | '_ \| |/ _ \/ __| __|
//   | | (_| | | | | | | | | |_| | |_) | |  __/ (__| |_
//   |_|\__,_|_| |_| |_|_|  \___/|_.__// |\___|\___|\__|
//                                   |__/

type RaftLogYaml() =
  [<DefaultValue>] val mutable Data  : RaftLogEntryYaml array
  [<DefaultValue>] val mutable Depth : Long
  [<DefaultValue>] val mutable Index : Index

//  _
// | |    ___   __ _
// | |   / _ \ / _` |
// | |__| (_) | (_| |
// |_____\___/ \__, |
//             |___/

type RaftLog =
  { Data  : RaftLogEntry option
  ; Depth : Long
  ; Index : Index
  }

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

  static member FromFB (logs: LogFB array) : Either<IrisError, RaftLog> =
    either {
      let! entries = RaftLogEntry.FromFB logs
      match entries with
      | Some entries as value ->
        return { Data  = value
                 Depth = LogEntry.depth entries
                 Index = LogEntry.index entries }
      | _ ->
        return { Data  = None
                 Depth = 0u
                 Index = 0u }
    }

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let yaml = new RaftLogYaml()
    let arr = Array.zeroCreate (int self.Depth)
    Option.map
      (fun logdata ->
        LogEntry.iter (fun i entry -> arr.[int i] <- Yaml.toYaml entry))
      self.Data
    |> ignore
    yaml.Data  <- arr
    yaml.Depth <- self.Depth
    yaml.Index <- self.Index
    yaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (log: RaftLogYaml) : Either<IrisError, RaftLog> =
    let folder (yaml: RaftLogEntryYaml) (sibling: Either<IrisError,RaftLogEntry option>) =
      either {
        let! previous = sibling
        let! parsed = Yaml.fromYaml yaml

        return
          match parsed with
          | LogEntry(id, idx, term, data, _) ->
            LogEntry(id, idx, term, data, previous)
          | Configuration(id, idx, term, nodes, _)->
            Configuration(id, idx, term, nodes, previous)
          | JointConsensus(id, idx, term, changes, _) ->
            JointConsensus(id, idx, term, changes, previous)
          | Snapshot _ as value -> value
          |> Some
      }

    either {
      let! logs =
        Array.foldBack
          folder
          (Array.sort log.Data)
          (Right None)

      return
        match logs with
        | Some entries as values ->
          { Data = values
            Depth = LogEntry.depth entries
            Index = LogEntry.index entries }
        | _ ->
          { Data = None; Depth = 0u; Index = 0u }
    }

  static member FromYaml (str: string) : Either<IrisError,RaftLog> =
    let serializer = new Serializer()
    serializer.Deserialize<RaftLogYaml>(str)
    |> Yaml.fromYaml

//  _                  __  __           _       _
// | |    ___   __ _  |  \/  | ___   __| |_   _| | ___
// | |   / _ \ / _` | | |\/| |/ _ \ / _` | | | | |/ _ \
// | |__| (_) | (_| | | |  | | (_) | (_| | |_| | |  __/
// |_____\___/ \__, | |_|  |_|\___/ \__,_|\__,_|_|\___|
//             |___/

[<RequireQualifiedAccess>]
module Log =

  /// ## Construct an empty log.
  ///
  /// Build a new, empty log data structure.
  ///
  /// Returns: RaftLog
  let empty = { Depth = 0u
              ; Index = 0u
              ; Data  = None }

  /// ## Construct a new log value from entries
  ///
  /// Build a new RaftLog value from the passed entries.
  ///
  /// ### Signature:
  /// - entries: LogEntry's to construct RaftLog from
  ///
  /// Returns: RaftLog
  let fromEntries (entries: RaftLogEntry) =
    { Depth = LogEntry.depth entries
    ; Index = LogEntry.index entries
    ; Data  = Some entries }

  /// ## Length of logg
  ///
  /// Return the current length of RaftLog value
  ///
  /// ### Signature:
  /// - log: Log to get length for
  ///
  /// Returns: Long
  let length log = log.Depth

  /// ## Return the current Index in the log
  ///
  /// Return the current index in the RaftLog value
  ///
  /// ### Signature:
  /// - log: RaftLog to get index for
  ///
  /// Returns: Long
  let index log = log.Index

  /// ## Return the index of the previous element
  ///
  /// Return the index of the previous element
  ///
  /// ### Signature:
  /// - log: RaftLog to return previous index for
  ///
  /// Returns: Long
  let prevIndex log =
    Option.bind LogEntry.prevIndex log.Data

  /// ## Return the Term of the latest log entry
  ///
  /// Return the Term of the latest log entry
  ///
  /// ### Signature:
  /// - log: RaftLog to return term for
  ///
  /// Returns: Long
  let term log =
    match log.Data with
    | Some entries -> LogEntry.term entries
    | _            -> 0u

  /// ## Return the Term of the previous entry
  ///
  /// Return the Term of the previous entry
  ///
  /// ### Signature:
  /// - log: RaftLog to return previous term for
  ///
  /// Returns: Long
  let prevTerm log =
    Option.bind LogEntry.prevTerm log.Data

  /// Return the last Entry, if it exists
  ///
  /// ### Complexity: 0(1)
  let previous log =
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

  let prevEntry log =
    Option.bind (LogEntry.prevEntry) log.Data

  let foldLogL f m log =
    match log.Data with
    | Some entries -> LogEntry.foldl f m entries
    | _            -> m

  let foldLogR f m log =
    match log.Data with
    | Some entries -> LogEntry.foldr f m entries
    | _            -> m

  let at idx log =
    Option.bind (LogEntry.at idx) log.Data

  let until idx log =
    Option.bind (LogEntry.until idx) log.Data

  let untilExcluding idx log =
    Option.bind (LogEntry.untilExcluding idx) log.Data

  let append newentries log : RaftLog =
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

  let find id log =
    Option.bind (LogEntry.find id) log.Data

  let make term data = LogEntry.make term data
  let mkConfig term nodes = LogEntry.mkConfig term nodes
  let mkConfigChange term old newer =
    LogEntry.mkConfigChange term old newer

  let entries log = log.Data

  let aggregate f m log =
    Option.map (LogEntry.aggregate f m) log.Data

  let snapshot nodes data log =
    match log.Data with
      | Some entries ->
        let snapshot = LogEntry.snapshot nodes data entries
        { Index = LogEntry.index snapshot
          Depth = 1u
          Data = Some snapshot }
      | _ -> log

  let head log =
    Option.map LogEntry.head log.Data

  let lastTerm log =
    Option.bind LogEntry.lastTerm log.Data

  let lastIndex log =
    Option.bind LogEntry.lastIndex log.Data

  /// Return the last entry in the chain of logs.
  let last log =
    Option.map LogEntry.last log.Data

  /// Iterate over log entries, in order of newsest to oldest.
  let iter f log =
    Option.map (LogEntry.iter f) log.Data
    |> ignore

  /// Retrieve the index of the first log entry for the given term. Return None
  /// if no result was found;
  let firstIndex term log =
    Option.bind (LogEntry.firstIndex term) log.Data

  let getn count log =
    Option.bind (LogEntry.getn count) log.Data

  let contains (f: RaftLogEntry -> bool) log : bool =
    match Option.map (LogEntry.contains f) log.Data with
    | Some result -> result
    | _           -> false

  let map f log =
    Option.map (LogEntry.map f) log.Data
