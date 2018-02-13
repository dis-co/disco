(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Raft

// * Imports

open System
open System.Collections
open FlatBuffers
open Disco.Core
open Disco.Serialization

// * Log

//  _
// | |    ___   __ _
// | |   / _ \ / _` |
// | |__| (_) | (_| |
// |_____\___/ \__, |
//             |___/

type Log =
  { Data:  LogEntry option
    Depth: int
    Index: Index }

  // ** optics

  static member Data_ =
    (fun (log:Log) -> log.Data),
    (fun data (log:Log) -> { log with Data = data })

  static member Depth_ =
    (fun (log:Log) -> log.Depth),
    (fun depth (log:Log) -> { log with Depth = depth })

  static member Index_ =
    (fun (log:Log) -> log.Index),
    (fun index (log:Log) -> { log with Index = index })

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
      | _ -> Constants.EMPTY

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
  /// Returns: DiscoResult<RaftLog>
  static member FromFB (logs: LogFB array) : DiscoResult<Log> =
    result {
      let! entries = LogEntry.FromFB logs
      match entries with
      | Some entries as value ->
        return { Data  = value
                 Depth = LogEntry.depth entries
                 Index = LogEntry.index entries }
      | _ ->
        return { Data  = None
                 Depth = 0
                 Index = 0<index> }
    }

// * Log Module

//  _                  __  __           _       _
// | |    ___   __ _  |  \/  | ___   __| |_   _| | ___
// | |   / _ \ / _` | | |\/| |/ _ \ / _` | | | | |/ _ \
// | |__| (_) | (_| | | |  | | (_) | (_| | |_| | |  __/
// |_____\___/ \__, | |_|  |_|\___/ \__,_|\__,_|_|\___|
//             |___/

[<RequireQualifiedAccess>]
module Log =

  open Aether

  // ** getters

  let depth = Optic.get Log.Depth_
  let data = Optic.get Log.Data_
  let index = Optic.get Log.Index_

  // ** setters

  let setDepth = Optic.set Log.Depth_
  let setData = Optic.set Log.Data_
  let setIndex = Optic.set Log.Index_

  // ** empty

  /// ## Construct an empty log.
  ///
  /// Build a new, empty log data structure.
  ///
  /// Returns: RaftLog
  let empty =
    { Depth = 0
      Index = 0<index>
      Data  = None }

  // ** fromEntries

  /// ## Construct a new log value from entries
  ///
  /// Build a new RaftLog value from the passed entries.
  ///
  /// ### Signature:
  /// - entries: LogEntry's to construct RaftLog from
  ///
  /// Returns: RaftLog
  let fromEntries (entries: LogEntry) =
    { Depth = LogEntry.depth entries
      Index = LogEntry.index entries
      Data  = Some entries }

  // ** length

  /// ## Length of logg
  ///
  /// Return the current length of RaftLog value
  ///
  /// ### Signature:
  /// - log: Log to get length for
  ///
  /// Returns: Long
  let length log = log.Depth

  // ** prevIndex

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

  // ** term

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
    | _            -> 0<term>

  // ** prevTerm

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

  // ** previous

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

  // ** prevEntry

  let prevEntry log =
    Option.bind (LogEntry.prevEntry) log.Data

  // ** foldLogL

  let foldLogL f m log =
    match log.Data with
    | Some entries -> LogEntry.foldl f m entries
    | _            -> m

  // ** foldLogR

  let foldLogR f m log =
    match log.Data with
    | Some entries -> LogEntry.foldr f m entries
    | _            -> m

  // ** at

  let at idx log =
    Option.bind (LogEntry.at idx) log.Data

  // ** until

  let until idx log =
    Option.bind (LogEntry.until idx) log.Data

  // ** untilExcluding

  let untilExcluding idx log =
    Option.bind (LogEntry.untilExcluding idx) log.Data

  // ** append

  let append newentries log: Log =
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

  // ** find

  let find id log =
    Option.bind (LogEntry.find id) log.Data

  // ** make

  let make term data = LogEntry.make term data

  // ** configuration

  let configuration = LogEntry.configuration

  // ** jointConsensus

  let jointConsensus = LogEntry.jointConsensus

  let calculateChanges oldnodes newnodes =
    LogEntry.calculateChanges oldnodes newnodes

  // ** entries

  let entries log = log.Data

  // ** aggregate

  let aggregate f m log =
    Option.map (LogEntry.aggregate f m) log.Data

  // ** snapshot

  let snapshot nodes data log =
    match log.Data with
      | Some entries ->
        let snapshot = LogEntry.snapshot nodes data entries
        { Index = LogEntry.index snapshot
          Depth = 1
          Data = Some snapshot }
      | _ -> log

  // ** head

  let head log =
    Option.map LogEntry.head log.Data

  // ** lastTerm

  let lastTerm log =
    Option.bind LogEntry.lastTerm log.Data

  // ** lastTerm

  let lastIndex log =
    Option.bind LogEntry.lastIndex log.Data

  // ** last

  /// Return the last entry in the chain of logs.
  let last log =
    Option.map LogEntry.last log.Data

  // ** iter

  /// Iterate over log entries, in order of newsest to oldest.
  let iter f log =
    Option.map (LogEntry.iter f) log.Data
    |> ignore

  // ** firstIndex

  /// Retrieve the index of the first log entry for the given term. Return None
  /// if no result was found;
  let firstIndex term log =
    Option.bind (LogEntry.firstIndex term) log.Data

  // ** getn

  let getn count log =
    Option.bind (LogEntry.getn count) log.Data

  // ** contains

  let contains (f: LogEntry -> bool) log : bool =
    match Option.map (LogEntry.contains f) log.Data with
    | Some result -> result
    | _           -> false

  // ** map

  let map f log =
    Option.map (LogEntry.map f) log.Data
