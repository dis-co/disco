namespace Iris.Raft

// * Imports

open System
open System.Collections
open FlatBuffers
open Iris.Core
open Iris.Serialization.Raft
open SharpYaml.Serialization

// * RaftLog Yaml

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

// * RaftLog

//  ____        __ _   _
// |  _ \ __ _ / _| |_| |    ___   __ _
// | |_) / _` | |_| __| |   / _ \ / _` |
// |  _ < (_| |  _| |_| |__| (_) | (_| |
// |_| \_\__,_|_|  \__|_____\___/ \__, |
//                                |___/

type RaftLog =
  { Data  : RaftLogEntry option
  ; Depth : Long
  ; Index : Index
  }

  // ** ToString

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

  // ** ToOffset

  /// ## ToOffset
  ///
  /// Convert the current RaftLog value into an array of LogEntryFB offsets for use in FlatBuffers
  /// encoding.
  ///
  /// ### Signature:
  /// - builder: FlatBufferBuilder
  ///
  /// Returns: LogEntryFB array
  member self.ToOffset(builder: FlatBufferBuilder) =
    match self.Data with
    | Some entries -> entries.ToOffset(builder)
    | _            -> [| |]

  // ** FromFB

  /// ## FromFB
  ///
  /// Parse the given LogFB array into a RaftLog value.
  ///
  /// ### Signature:
  /// - logs: LogFB array
  ///
  /// Returns: Either<IrisError, RaftLog>
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

  // ** ToYamlObject

  /// ## ToYamlObject
  ///
  /// Convert the current RaftLog into its Yaml POCO.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: RaftLogYamlg
  member self.ToYamlObject () =
    let yaml = new RaftLogYaml()
    let arr = Array.zeroCreate (int self.Depth)

    Option.map
      (fun logdata ->
        LogEntry.iter
         (fun i entry -> arr.[int i] <- Yaml.toYaml entry)
         logdata)
      self.Data
    |> ignore

    yaml.Data  <- arr
    yaml.Depth <- self.Depth
    yaml.Index <- self.Index
    yaml

  // ** ToYaml

  /// ## ToYaml
  ///
  /// Serialize the current RaftLog value into a yaml string
  ///
  /// ### Signature:
  /// - serializer: SharpYaml Serializer
  ///
  /// Returns: string
  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  /// ## FromYamlObject
  ///
  /// Convert the yaml object representation into a RaftLog value.
  ///
  /// ### Signature:
  /// - log: RaftLogYaml value to parse
  ///
  /// Returns: Etiher<IrisError,RaftLog>
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

  // ** FromYaml

  /// ## FromYaml
  ///
  /// Parse the passed string and construct a RaftLog value from it.
  ///
  /// ### Signature:
  /// - str: string to parse
  ///
  /// Returns: Either<IrisError,RaftLog>
  static member FromYaml (str: string) : Either<IrisError,RaftLog> =
    let serializer = new Serializer()
    serializer.Deserialize<RaftLogYaml>(str)
    |> Yaml.fromYaml


// * Log Module

//  _                  __  __           _       _
// | |    ___   __ _  |  \/  | ___   __| |_   _| | ___
// | |   / _ \ / _` | | |\/| |/ _ \ / _` | | | | |/ _ \
// | |__| (_) | (_| | | |  | | (_) | (_| | |_| | |  __/
// |_____\___/ \__, | |_|  |_|\___/ \__,_|\__,_|_|\___|
//             |___/

[<RequireQualifiedAccess>]
module Log =

  // ** Log.empty

  /// ## Construct an empty log.
  ///
  /// Build a new, empty log data structure.
  ///
  /// Returns: RaftLog
  let empty = { Depth = 0u
              ; Index = 0u
              ; Data  = None }

  // ** Log.fromEntries

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

  // ** Log.length

  /// ## Length of logg
  ///
  /// Return the current length of RaftLog value
  ///
  /// ### Signature:
  /// - log: Log to get length for
  ///
  /// Returns: Long
  let length log = log.Depth

  // ** Log.index

  /// ## Return the current Index in the log
  ///
  /// Return the current index in the RaftLog value
  ///
  /// ### Signature:
  /// - log: RaftLog to get index for
  ///
  /// Returns: Long
  let index log = log.Index

  // ** Log.prevIndex

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

  // ** Log.term

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

  // ** Log.prevTerm

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

  // ** Log.previous

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

  // ** Log.prevEntry

  let prevEntry log =
    Option.bind (LogEntry.prevEntry) log.Data

  // ** Log.foldLogL

  let foldLogL f m log =
    match log.Data with
    | Some entries -> LogEntry.foldl f m entries
    | _            -> m

  // ** Log.foldLogR

  let foldLogR f m log =
    match log.Data with
    | Some entries -> LogEntry.foldr f m entries
    | _            -> m

  // ** Log.at

  let at idx log =
    Option.bind (LogEntry.at idx) log.Data

  // ** Log.until

  let until idx log =
    Option.bind (LogEntry.until idx) log.Data

  // ** Log.untilExcluding

  let untilExcluding idx log =
    Option.bind (LogEntry.untilExcluding idx) log.Data

  // ** Log.append

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

  // ** Log.find

  let find id log =
    Option.bind (LogEntry.find id) log.Data

  // ** Log.make

  let make term data = LogEntry.make term data

  // ** Log.mkConfig

  let mkConfig term nodes = LogEntry.mkConfig term nodes

  // ** Log.mkConfigChange

  let mkConfigChange term old newer =
    LogEntry.mkConfigChange term old newer

  // ** Log.entries

  let entries log = log.Data

  // ** Log.aggregate

  let aggregate f m log =
    Option.map (LogEntry.aggregate f m) log.Data

  // ** Log.snapshot

  let snapshot nodes data log =
    match log.Data with
      | Some entries ->
        let snapshot = LogEntry.snapshot nodes data entries
        { Index = LogEntry.index snapshot
          Depth = 1u
          Data = Some snapshot }
      | _ -> log

  // ** Log.head

  let head log =
    Option.map LogEntry.head log.Data

  // ** Log.lastTerm

  let lastTerm log =
    Option.bind LogEntry.lastTerm log.Data

  // ** Log.lastTerm

  let lastIndex log =
    Option.bind LogEntry.lastIndex log.Data

  // ** Log.last

  /// Return the last entry in the chain of logs.
  let last log =
    Option.map LogEntry.last log.Data

  // ** Log.iter

  /// Iterate over log entries, in order of newsest to oldest.
  let iter f log =
    Option.map (LogEntry.iter f) log.Data
    |> ignore

  // ** Log.firstIndex

  /// Retrieve the index of the first log entry for the given term. Return None
  /// if no result was found;
  let firstIndex term log =
    Option.bind (LogEntry.firstIndex term) log.Data

  // ** Log.getn

  let getn count log =
    Option.bind (LogEntry.getn count) log.Data

  // ** Log.contains

  let contains (f: RaftLogEntry -> bool) log : bool =
    match Option.map (LogEntry.contains f) log.Data with
    | Some result -> result
    | _           -> false

  // ** Log.map

  let map f log =
    Option.map (LogEntry.map f) log.Data
