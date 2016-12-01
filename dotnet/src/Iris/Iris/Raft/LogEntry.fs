namespace Iris.Raft

// * Imports

open System
open System.Collections
open Iris.Core
open Iris.Serialization.Raft

#if FABLE_COMPILER

open Iris.Core.FlatBuffers

#else

open FlatBuffers

#endif

// * RAftLogEntry Yaml

type RaftLogEntryYaml() =

  [<DefaultValue>] val mutable LogType   : string
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable Index     : Index
  [<DefaultValue>] val mutable Term      : Term
  [<DefaultValue>] val mutable LastIndex : Index
  [<DefaultValue>] val mutable LastTerm  : Term
  [<DefaultValue>] val mutable Changes   : ConfigChangeYaml array
  [<DefaultValue>] val mutable Nodes     : RaftNodeYaml array
  [<DefaultValue>] val mutable Data      : StateMachineYaml
  [<DefaultValue>] val mutable Previous  : string

  // ** Configuration

  static member Configuration (id, idx, term, nodes, prev) =
    let yaml = new RaftLogEntryYaml()
    yaml.LogType  <- "Configuration"
    yaml.Id       <- id
    yaml.Index    <- idx
    yaml.Term     <- term
    yaml.Nodes    <- nodes
    yaml.Previous <- prev
    yaml

  // ** JointConsensus

  static member JointConsensus (id, idx, term, changes, prev) =
    let yaml = new RaftLogEntryYaml()
    yaml.LogType <- "JointConsensus"
    yaml.Id <- id
    yaml.Index <- idx
    yaml.Term <- term
    yaml.Changes <- changes
    yaml.Previous <- prev
    yaml

  // ** LogEntry

  static member LogEntry (id, idx, term, sm, prev) =
    let yaml = new RaftLogEntryYaml()
    yaml.LogType <- "LogEntry"
    yaml.Id <- id
    yaml.Index <- idx
    yaml.Term <- term
    yaml.Data <- sm
    yaml.Previous <- prev
    yaml

  // ** Snapshot

  static member Snapshot (id, idx, term, lidx, lterm, nodes, sm) =
    let yaml = new RaftLogEntryYaml()
    yaml.LogType <- "Snapshot"
    yaml.Id <- id
    yaml.Index <- idx
    yaml.Term <- term
    yaml.LastIndex <- lidx
    yaml.LastTerm <- lterm
    yaml.Nodes <- nodes
    yaml.Data <- sm
    yaml

  // ** Equals

  override self.Equals(obj) =
    match obj with
    | :? RaftLogEntryYaml as other ->
      (self :> System.IEquatable<RaftLogEntryYaml>).Equals(other)
    | _ -> false

  // ** GetHashCode

  override self.GetHashCode() =
    hash (self.LogType
         ,self.Id
         ,self.Index
         ,self.Term
         ,self.LastIndex
         ,self.LastTerm
         ,self.Changes
         ,self.Nodes
         ,self.Data
         ,self.Previous)

  // ** IEquatable Interface

  interface System.IEquatable<RaftLogEntryYaml> with
    member self.Equals(other: RaftLogEntryYaml) =
      self.LogType   = other.LogType   &&
      self.Id        = other.Id        &&
      self.Index     = other.Index     &&
      self.Term      = other.Term      &&
      self.LastIndex = other.LastIndex &&
      self.LastTerm  = other.LastTerm  &&
      self.Changes   = other.Changes   &&
      self.Nodes     = other.Nodes     &&
      self.Data      = other.Data      &&
      self.Previous  = other.Previous

  // ** IComparable Interface

  interface System.IComparable<RaftLogEntryYaml> with
    member self.CompareTo (other: RaftLogEntryYaml) =
      match self.Index, other.Index with
      | x, y when x > y -> -1
      | x, y when x = y ->  0
      |             _   ->  1

  interface System.IComparable with
    member self.CompareTo (obj) =
      match obj with
      | :? RaftLogEntryYaml as other ->
        (self :> System.IComparable<RaftLogEntryYaml>).CompareTo(other)
      | _ -> failwith "Cannot compare Apples with Pears"

// * RaftLogEntry

///  _                _____       _
/// | |    ___   __ _| ____|_ __ | |_ _ __ _   _
/// | |   / _ \ / _` |  _| | '_ \| __| '__| | | |
/// | |__| (_) | (_| | |___| | | | |_| |  | |_| |
/// |_____\___/ \__, |_____|_| |_|\__|_|   \__, |
///             |___/                      |___/
///
/// Linked-list like type for fast access to most recent elements and their
/// properties.
///
/// type LogEntry =
///   // Node Configuration Entry
///   | Configuration of
///     Id       : Id              *        // unique id of configuration entry
///     Index    : Index           *        // index in log
///     Term     : Term            *        // term when entry was added to log
///     Nodes    : RaftNode array  *        // new node configuration
///     Previous : LogEntry option          // previous log entry, if applicable
///
///   // Entry type for configuration changes
///   | JointConsensus of
///     Id       : Id                 *     // unique identified of entry
///     Index    : Index              *     // index of entry in log
///     Term     : Term               *     // term when entry was added to log
///     Changes  : ConfigChange array *     // changes to node configuration
///     Previous : LogEntry option          // previous element, if any
///
///   // Regular Log Entries
///   | LogEntry   of
///     Id       : Id              *        // unique identifier of entry
///     Index    : Index           *        // index of entry in log
///     Term     : Term            *        // term when entry was added to log
///     Data     : StateMachine    *        // state machine data field
///     Previous : LogEntry option          // previous element, if any
///
///   | Snapshot   of
///     Id        : Id             *        // unique identifier of entry
///     Index     : Index          *        // index of entry in log
///     Term      : Term           *        // term when was added in to log
///     LastIndex : Index          *        // last included index
///     LastTerm  : Term           *        // last included term
///     Nodes     : RaftNode array *        // node configuration
///     Data      : StateMachine            // state machine data
///

and RaftLogEntry =

  // Node Configuration Entry
  | Configuration of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Nodes    : RaftNode array  *
    Previous : RaftLogEntry option

  // Entry type for configuration changes
  | JointConsensus of
    Id       : Id                     *
    Index    : Index                  *
    Term     : Term                   *
    Changes  : ConfigChange array     *
    Previous : RaftLogEntry option

  // Regular Log Entries
  | LogEntry   of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Data     : StateMachine    *
    Previous : RaftLogEntry option

  | Snapshot   of
    Id        : Id             *
    Index     : Index          *
    Term      : Term           *
    LastIndex : Index          *
    LastTerm  : Term           *
    Nodes     : RaftNode array *
    Data      : StateMachine


  // ** ToString

  //  _____    ____  _        _
  // |_   _|__/ ___|| |_ _ __(_)_ __   __ _
  //   | |/ _ \___ \| __| '__| | '_ \ / _` |
  //   | | (_) |__) | |_| |  | | | | | (_| |
  //   |_|\___/____/ \__|_|  |_|_| |_|\__, |
  //                                  |___/
  override self.ToString() =
    match self with
      | Configuration(id,idx,term,nodes,Some prev) ->
        sprintf "Configuration(id: %s idx: %A term: %A nodes: %s)\n%s"
          (string id)
          idx
          term
          (Array.fold (fun m (n: RaftNode) -> sprintf "%s, %s" m (string n.Id)) "" nodes)
          (string prev)

      | Configuration(id,idx,term,nodes,_) ->
        sprintf "Configuration(id: %s idx: %A term: %A nodes: %s)"
          (string id)
          idx
          term
          (Array.fold (fun m (n: RaftNode) -> sprintf "%s, %s" m (string n.Id)) "" nodes)

      | JointConsensus(id,idx,term,changes,Some prev) ->
        sprintf "JointConsensus(id: %s idx: %A term: %A changes: %s)\n%s"
          (string id)
          idx
          term
          (Array.fold (fun m n -> sprintf "%s, %s" m (string n)) "" changes)
          (string prev)

      | JointConsensus(id,idx,term,changes,_) ->
        sprintf "JointConsensus(id: %s idx: %A term: %A changes: %s)"
          (string id)
          idx
          term
          (Array.fold (fun m n -> sprintf "%s, %s" m (string n)) "" changes)

      | LogEntry(id,idx,term,_,Some prev) ->
        sprintf "LogEntry(id: %s idx: %A term: %A)\n%s"
          (string id)
          idx
          term
          (string prev)

      | LogEntry(id,idx,term,_,_) ->
        sprintf "LogEntry(id: %s idx: %A term: %A)"
          (string id)
          idx
          term

      | Snapshot(id,idx,term,lidx,ltrm,_,_) ->
        sprintf "Snapshot(id: %s idx: %A lidx: %A term: %A lterm: %A)"
          (string id)
          idx
          lidx
          term
          ltrm

  // ** Id

  /// ## Id
  ///
  /// Get the current log's Id.
  ///
  /// Returns: Id
  member self.Id
    with get () =
      match self with
      | Configuration(id,_,_,_,_)  -> id
      | JointConsensus(id,_,_,_,_) -> id
      | LogEntry(id,_,_,_,_)       -> id
      | Snapshot(id,_,_,_,_,_,_)   -> id

  // ** Depth

  /// ## Depth
  ///
  /// Compute the depth of the current log.
  ///
  /// Returns: uint32
  member self.Depth
    with get () =
      let rec _depth i thing =
        let inline count i prev =
          let cnt = i + 1u
          match prev with
            | Some other -> _depth cnt other
            |          _ -> cnt
        match thing with
          | Configuration(_,_,_,_,prev)  -> count i prev
          | JointConsensus(_,_,_,_,prev) -> count i prev
          | LogEntry(_,_,_,_,prev)       -> count i prev
          | Snapshot _                   -> i + 1u
      _depth 0u self

  // ** Iter

  /// ## Iter
  ///
  /// Iterate over the entire log sequence and apply `f` to every element.
  ///
  /// ### Signature:
  /// - f: uint32 -> RaftLogEntry -> unit
  ///
  /// Returns: unit
  member self.Iter (f : uint32 -> RaftLogEntry -> unit) =
    let rec impl start = function
      | Configuration(_,_,_,_,Some prev)  as curr ->
        f start curr; impl (start + 1u) prev

      | Configuration(_,_,_,_,None)       as curr ->
        f start curr

      | JointConsensus(_,_,_,_,Some prev) as curr ->
        f start curr; impl (start + 1u) prev

      | JointConsensus(_,_,_,_,None)      as curr ->
        f start curr

      | LogEntry(_,_,_,_,Some prev)       as curr ->
        f start curr; impl (start + 1u) prev

      | LogEntry(_,_,_,_,None)            as curr ->
        f start curr

      | Snapshot _                        as curr ->
        f start curr

    impl 0u self

  // ** ToOffset

  //  _____ _       _   ____         __  __
  // |  ___| | __ _| |_| __ ) _   _ / _|/ _| ___ _ __ ___
  // | |_  | |/ _` | __|  _ \| | | | |_| |_ / _ \ '__/ __|
  // |  _| | | (_| | |_| |_) | |_| |  _|  _|  __/ |  \__ \
  // |_|   |_|\__,_|\__|____/ \__,_|_| |_|  \___|_|  |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let buildLogFB tipe value =
      LogFB.StartLogFB(builder)
      LogFB.AddEntryType(builder, tipe)
      LogFB.AddEntry(builder, value)
      LogFB.EndLogFB(builder)

    let toOffset (log: RaftLogEntry) =
      match log with
      //   ____             __ _                       _   _
      //  / ___|___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
      // | |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
      // | |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
      //  \____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
      //                         |___/
      | Configuration(id,index,term,nodes,_)          ->
        let id = string id |> builder.CreateString
        let nodes = Array.map (Binary.toOffset builder) nodes
        let nvec = ConfigurationFB.CreateNodesVector(builder, nodes)

        ConfigurationFB.StartConfigurationFB(builder)
        ConfigurationFB.AddId(builder, id)
        ConfigurationFB.AddIndex(builder, index)
        ConfigurationFB.AddTerm(builder, term)
        ConfigurationFB.AddNodes(builder, nvec)

        let entry = ConfigurationFB.EndConfigurationFB(builder)

        buildLogFB LogTypeFB.ConfigurationFB entry.Value

      //      _       _       _    ____
      //     | | ___ (_)_ __ | |_ / ___|___  _ __  ___  ___ _ __  ___ _   _ ___
      //  _  | |/ _ \| | '_ \| __| |   / _ \| '_ \/ __|/ _ \ '_ \/ __| | | / __|
      // | |_| | (_) | | | | | |_| |__| (_) | | | \__ \  __/ | | \__ \ |_| \__ \
      //  \___/ \___/|_|_| |_|\__|\____\___/|_| |_|___/\___|_| |_|___/\__,_|___/
      | JointConsensus(id,index,term,changes,_) ->
        let id = string id |> builder.CreateString
        let changes = Array.map (Binary.toOffset builder) changes
        let chvec = JointConsensusFB.CreateChangesVector(builder, changes)

        JointConsensusFB.StartJointConsensusFB(builder)
        JointConsensusFB.AddId(builder, id)
        JointConsensusFB.AddIndex(builder, index)
        JointConsensusFB.AddTerm(builder, term)
        JointConsensusFB.AddChanges(builder, chvec)

        let entry = JointConsensusFB.EndJointConsensusFB(builder)

        buildLogFB LogTypeFB.JointConsensusFB entry.Value

      //  _                _____       _
      // | |    ___   __ _| ____|_ __ | |_ _ __ _   _
      // | |   / _ \ / _` |  _| | '_ \| __| '__| | | |
      // | |__| (_) | (_| | |___| | | | |_| |  | |_| |
      // |_____\___/ \__, |_____|_| |_|\__|_|   \__, |
      //             |___/                      |___/
      | LogEntry(id,index,term,data,_) ->
        let id = string id |> builder.CreateString
        let data = data.ToOffset(builder)

        LogEntryFB.StartLogEntryFB(builder)
        LogEntryFB.AddId(builder, id)
        LogEntryFB.AddIndex(builder, index)
        LogEntryFB.AddTerm(builder, term)
        LogEntryFB.AddData(builder, data)

        let entry = LogEntryFB.EndLogEntryFB(builder)

        buildLogFB LogTypeFB.LogEntryFB entry.Value

      //  ____                        _           _
      // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
      // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                   |_|
      | Snapshot(id,index,term,lidx,lterm,nodes,data) ->
        let id = string id |> builder.CreateString
        let nodes = Array.map (Binary.toOffset builder) nodes
        let nvec = SnapshotFB.CreateNodesVector(builder, nodes)
        let data = data.ToOffset(builder)

        SnapshotFB.StartSnapshotFB(builder)
        SnapshotFB.AddId(builder, id)
        SnapshotFB.AddIndex(builder, index)
        SnapshotFB.AddTerm(builder, term)
        SnapshotFB.AddLastIndex(builder, lidx)
        SnapshotFB.AddLastTerm(builder, lterm)
        SnapshotFB.AddNodes(builder, nvec)
        SnapshotFB.AddData(builder, data)

        let entry = SnapshotFB.EndSnapshotFB(builder)

        buildLogFB LogTypeFB.SnapshotFB entry.Value

    let arr = Array.zeroCreate (self.Depth |> int)
    self.Iter (fun i (log: RaftLogEntry) -> arr.[int i] <- toOffset log)
    arr

  // ** ParseLogFB

  /// ## Parse a single log entry, adding its sibling node
  ///
  /// Parses a single log entry, adding the passed sibling node, if any. If an error occurs, the
  /// entire parsing process fails. With the first error.
  ///
  /// ### Signature:
  /// - fb: LogFB FlatBuffer object
  /// - sibling: an sibling (None also legal, for the first node), or the previous error
  ///
  /// Returns: Either<IrisError, RaftLogEntry option>
  static member ParseLogFB (fb: LogFB)
                           (sibling: Either<IrisError,RaftLogEntry option>)
                           : Either<IrisError,RaftLogEntry option> =
      match fb.EntryType with
      | LogTypeFB.ConfigurationFB -> either {
          // the previous log entry. An error, if occurred previously
          let! previous = sibling

          // parse the log entry
          let entry = fb.Entry<ConfigurationFB>()
          if entry.HasValue then
            let logentry = entry.Value

            // parse all nodes in this log entry. if this fails, the error will be propagated up the
            // call chain
            let! nodes =
              let arr = Array.zeroCreate logentry.NodesLength
              Array.fold
                (fun (m: Either<IrisError,int * RaftNode array>) _ -> either {
                  let! (i, arr) = m
                  let! node =
                    let value = logentry.Nodes(i)
                    if value.HasValue then
                      value.Value
                      |> RaftNode.FromFB
                    else
                      "Could not parse empty NodeFB value"
                      |> ParseError
                      |> Either.fail
                  arr.[i] <- node
                  return (i + i, arr)
                })
                (Right (0, arr))
                arr
              |> Either.map snd

            // successfully parsed this LogEntry, so return it wrapped in an option
            return Configuration(Id logentry.Id,
                                 logentry.Index,
                                 logentry.Term,
                                 nodes,
                                 previous)
                   |> Some
          else
            return! "Could not parse empty LogTypeFB.ConfigurationFB"
                    |> ParseError
                    |> Either.fail
        }

      | LogTypeFB.JointConsensusFB -> either {
          // the previous entry, or an error. short-circuits here on error.
          let! previous = sibling

          // start parsing the entry
          let entry = fb.Entry<JointConsensusFB>()
          if entry.HasValue then
            let logentry = entry.Value

            // parse the ConfigChange entries
            let! changes =
              let arr = Array.zeroCreate logentry.ChangesLength
              Array.fold
                (fun (m: Either<IrisError, int * ConfigChange array>) _ -> either {
                  let! (i, changes) = m // pull the index and array out
                  let! change =
                    let value = logentry.Changes(i)
                    if value.HasValue then
                      value.Value
                      |> ConfigChange.FromFB
                    else
                      "Could not parse empty ConfigChangeFB value"
                      |> ParseError
                      |> Either.fail
                  changes.[i] <- change
                  return (i + 1, changes)
                })
                (Right (0, arr))
                arr
              |> Either.map snd

            return JointConsensus(Id logentry.Id,
                                  logentry.Index,
                                  logentry.Term,
                                  changes,
                                  previous)
                   |> Some
          else
            return!
              "Could not parse empty LogTypeFB.JointConsensusFB"
              |> ParseError
              |> Either.fail

        }

      | LogTypeFB.LogEntryFB -> either {
          let! previous = sibling

          let entry = fb.Entry<LogEntryFB>()
          if entry.HasValue then
            let logentry = entry.Value
            let data = logentry.Data
            if data.HasValue then
              let! command = StateMachine.FromFB data.Value

              return LogEntry(Id logentry.Id,
                              logentry.Index,
                              logentry.Term,
                              command,
                              previous)
                     |> Some
            else
              return!
                "Could not parse empty StateMachineFB"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty LogTypeFB.LogEntry"
              |> ParseError
              |> Either.fail
        }

      | LogTypeFB.SnapshotFB -> either {
          // Snapshots don't have ancestors, so move ahead right away
          let entry = fb.Entry<SnapshotFB>()
          if entry.HasValue then
            let logentry = entry.Value
            let data = logentry.Data

            if data.HasValue then
              let id = Id logentry.Id
              let! state = StateMachine.FromFB data.Value

              let! nodes =
                let arr = Array.zeroCreate logentry.NodesLength
                Array.fold
                  (fun (m: Either<IrisError, int * RaftNode array>) _ -> either {
                    let! (i, nodes) = m

                    let! node =
                      let value = logentry.Nodes(i)
                      if value.HasValue then
                        value.Value
                        |> RaftNode.FromFB
                      else
                        "Could not parse empty RaftNodeFB"
                        |> ParseError
                        |> Either.fail

                    nodes.[i] <- node
                    return (i + 1, nodes)
                  })
                  (Right (0, arr))
                  arr
                |> Either.map snd

              return Snapshot(id,
                              logentry.Index,
                              logentry.Term,
                              logentry.LastIndex,
                              logentry.LastTerm,
                              nodes,
                              state)
                     |> Some
            else
              return!
                "Could not parse empty StateMachineFB"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty LogTypeFB.SnapshotFB"
              |> ParseError
              |> Either.fail
        }

      | x ->
        sprintf "Could not parse unknown LogTypeFB; %A" x
        |> ParseError
        |> Either.fail

  // ** FromFB

  /// ## Decode a FlatBuffer into a Log structure
  ///
  /// Decodes a single FlatBuffer encoded log entry into its
  /// corresponding Raft RaftLogEntry type and adds passed-in `RaftLogEntry
  /// option` as previous field value. Indicates failure by returning
  /// None.
  ///
  /// ### Signature:
  /// - fb: LogFB FlatBuffer object to parse
  /// - log: previous RaftLogEntry value to reconstruct the chain of events
  ///
  /// Returns: RaftLogEntry option
  static member FromFB (logs: LogFB array) : Either<IrisError, RaftLogEntry option> =
    Array.foldBack RaftLogEntry.ParseLogFB logs (Right None)

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  /// ## Convert the log entry to Yaml
  ///
  /// Convert the RaftLogEntry into a Yaml-serializable POCO.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: RaftLogEntryYaml
  member self.ToYamlObject () =
    match self with
    | Configuration(id, idx, term, nodes, Some prev) ->
      let lid = string id
      let previd = prev.Id |> string
      let nids = Array.map Yaml.toYaml nodes
      RaftLogEntryYaml.Configuration(lid, idx, term, nids, previd)

    | Configuration(id, idx, term, nodes, None) ->
      let lid = string id
      let previd = null
      let nids = Array.map Yaml.toYaml nodes
      RaftLogEntryYaml.Configuration(lid, idx, term, nids, previd)

    | JointConsensus(id, idx, term, changes, Some prev) ->
      let lid = string id
      let previd = prev.Id |> string
      let ymls = Array.map Yaml.toYaml changes
      RaftLogEntryYaml.JointConsensus(lid, idx, term, ymls, previd)

    | JointConsensus(id, idx, term, changes, None) ->
      let lid = string id
      let previd = null
      let ymls = Array.map Yaml.toYaml changes
      RaftLogEntryYaml.JointConsensus(lid, idx, term, ymls, previd)

    | LogEntry(id, idx, term, smentry, None) ->
      let lid = string id
      let previd = null
      let yml = Yaml.toYaml smentry
      RaftLogEntryYaml.LogEntry(lid, idx, term, yml, previd)

    | LogEntry(id, idx, term, smentry, Some prev) ->
      let lid = string id
      let previd = prev.Id |> string
      RaftLogEntryYaml.LogEntry(lid, idx, term, Yaml.toYaml smentry, previd)

    | Snapshot(id, idx, term, lidx, lterm, nodes, smentry) ->
      let lid = string id
      let nids = Array.map Yaml.toYaml nodes
      let yml = Yaml.toYaml smentry
      RaftLogEntryYaml.Snapshot(lid, idx, term, lidx, lterm, nids, yml)

  // ** FromYamlObject

  /// ## FromYamlObject
  ///
  /// Deserialize a Yaml object to a log
  ///
  /// ### Signature:
  /// - yaml: RaftLogEntryYaml to deserialize
  ///
  /// Returns: Either<IrisError, RaftLogEntry>
  static member FromYamlObject (yaml: RaftLogEntryYaml) =
    match yaml.LogType with
    | "LogEntry" -> either {
        let id = Id yaml.Id
        let! data = Yaml.fromYaml yaml.Data
        return LogEntry(id, yaml.Index, yaml.Term, data, None)
      }
    | "Configuration" -> either {
        let id = Id yaml.Id
        let! nodes =
          Array.fold
            (fun (m: Either<IrisError, int * RaftNode array>) (yml: RaftNodeYaml) -> either {
              let! (i, nodes) = m
              let! node = Yaml.fromYaml yml
              nodes.[i] <- node
              return (i + 1, nodes)
            })
            (Right (0, Array.zeroCreate yaml.Nodes.Length))
            yaml.Nodes
          |> Either.map snd
        return Configuration(id, yaml.Index, yaml.Term, nodes, None)
      }
    | "JointConsensus" -> either {
        let id = Id yaml.Id
        let! changes =
          Array.fold
            (fun (m: Either<IrisError, int * ConfigChange array>) yml -> either {
              let! (i, changes) = m
              let! change = Yaml.fromYaml yml
              changes.[i] <- change
              return (i + 1, changes)
            })
            (Right (0, Array.zeroCreate yaml.Changes.Length))
            yaml.Changes
          |> Either.map snd
        return JointConsensus(id, yaml.Index, yaml.Term, changes, None)
      }
    | "Snapshot" -> either {
        let id = Id yaml.Id
        let idx = yaml.Index
        let term = yaml.Term
        let lidx = yaml.LastIndex
        let lterm = yaml.LastTerm
        let! data = Yaml.fromYaml yaml.Data
        let! nodes =
          Array.fold
            (fun (m: Either<IrisError, int * RaftNode array>) yml -> either {
              let! (i, nodes) = m
              let! node = Yaml.fromYaml yml
              nodes.[i] <- node
              return (i + 1, nodes)
            })
            (Right (0, Array.zeroCreate yaml.Nodes.Length))
            yaml.Nodes
          |> Either.map snd
        return Snapshot(id, idx, term, lidx, lterm, nodes, data)
      }
    | x ->
      sprintf "Could not parse unknow LogType: %s" x
      |> ParseError
      |> Either.fail


// * LogEntry Module

[<RequireQualifiedAccess>]
module LogEntry =

  // ** LogEntry.getId

  //   _     _
  //  (_) __| |
  //  | |/ _` |
  //  | | (_| |
  //  |_|\__,_|
  //

  /// ## Get the Id of a log entry
  ///
  /// Get the unique identifier of the top-most log entry
  ///
  /// ### Signature:
  /// - log: RaftLogEntry to get Id ofg
  ///
  /// Returns: Id
  let getId (log: RaftLogEntry) = log.Id

  // ** LogEntry.isConfigChange

  //  _      ____             __ _        ____ _
  // (_)___ / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
  // | / __| |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
  // | \__ \ |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
  // |_|___/\____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
  //                               |___/                         |___/

  let isConfigChange = function
    | JointConsensus _ -> true
    |                _ -> false

  // ** LogEntry.isConfiguration

  //  _      ____             __ _                       _   _
  // (_)___ / ___|___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
  // | / __| |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
  // | \__ \ |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
  // |_|___/\____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
  //                               |___/

  let isConfiguration = function
    | Configuration _ -> true
    |               _ -> false

  // ** LogEntry.depth

  //       _            _   _
  //    __| | ___ _ __ | |_| |__
  //   / _` |/ _ \ '_ \| __| '_ \
  //  | (_| |  __/ |_) | |_| | | |
  //   \__,_|\___| .__/ \__|_| |_|
  //             |_|
  //
  /// compute the actual depth of the log (e.g. for compacting)

  let depth (log: RaftLogEntry) =
    log.Depth

  // ** LogEntry.index

  //   _           _
  //  (_)_ __   __| | _____  __
  //  | | '_ \ / _` |/ _ \ \/ /
  //  | | | | | (_| |  __/>  <
  //  |_|_| |_|\__,_|\___/_/\_\
  //
  /// Return the index of the current log entry.

  let index = function
    | Configuration(_,idx,_,_,_)  -> idx
    | JointConsensus(_,idx,_,_,_) -> idx
    | LogEntry(_,idx,_,_,_)       -> idx
    | Snapshot(_,idx,_,_,_,_,_)   -> idx

  // ** LogEntry.prevIndex

  //                        ___           _
  //   _ __  _ __ _____   _|_ _|_ __   __| | _____  __
  //  | '_ \| '__/ _ \ \ / /| || '_ \ / _` |/ _ \ \/ /
  //  | |_) | | |  __/\ V / | || | | | (_| |  __/>  <
  //  | .__/|_|  \___| \_/ |___|_| |_|\__,_|\___/_/\_\
  //  |_|
  //
  /// Return the index of the previous element if present.

  let prevIndex = function
    | Configuration(_,_,_,_,Some prev)  -> Some (index prev)
    | JointConsensus(_,_,_,_,Some prev) -> Some (index prev)
    | LogEntry(_,_,_,_,Some prev)       -> Some (index prev)
    | Snapshot(_,_,_,idx,_,_,_)         -> Some idx
    | _                                 -> None

  // ** LogEntry.term

  //   _
  //  | |_ ___ _ __ _ __ ___
  //  | __/ _ \ '__| '_ ` _ \
  //  | ||  __/ |  | | | | | |
  //   \__\___|_|  |_| |_| |_|
  //
  /// Extract the `Term` field from a RaftLogEntry

  let term = function
    | Configuration(_,_,term,_,_)  -> term
    | JointConsensus(_,_,term,_,_) -> term
    | LogEntry(_,_,term,_,_)       -> term
    | Snapshot(_,_,term,_,_,_,_)   -> term

  // ** LogEntry.prevTerm

  //                        _____
  //   _ __  _ __ _____   _|_   _|__ _ __ _ __ ___
  //  | '_ \| '__/ _ \ \ / / | |/ _ \ '__| '_ ` _ \
  //  | |_) | | |  __/\ V /  | |  __/ |  | | | | | |
  //  | .__/|_|  \___| \_/   |_|\___|_|  |_| |_| |_|
  //  |_|
  //
  /// Return the previous elements' term, if present.

  let prevTerm = function
    | Configuration(_,_,_,_,Some prev)  -> Some (term prev)
    | JointConsensus(_,_,_,_,Some prev) -> Some (term prev)
    | LogEntry(_,_,_,_,Some prev)       -> Some (term prev)
    | Snapshot(_,_,_,_,term,_,_)        -> Some term
    | _                                 -> None

  // ** LogEntry.prevEntry

  //                        _____       _
  //   _ __  _ __ _____   _| ____|_ __ | |_ _ __ _   _
  //  | '_ \| '__/ _ \ \ / /  _| | '_ \| __| '__| | | |
  //  | |_) | | |  __/\ V /| |___| | | | |_| |  | |_| |
  //  | .__/|_|  \___| \_/ |_____|_| |_|\__|_|   \__, |
  //  |_|                                        |___/
  //
  /// Return the previous entry, should there be one.

  let prevEntry = function
    | Configuration(_,_,_,_,prev)  -> prev
    | JointConsensus(_,_,_,_,prev) -> prev
    | LogEntry(_,_,_,_,prev)       -> prev
    | Snapshot _                   -> None

  // ** LogEntry.data

  //       _       _
  //    __| | __ _| |_ __ _
  //   / _` |/ _` | __/ _` |
  //  | (_| | (_| | || (_| |
  //   \__,_|\__,_|\__\__,_|
  //
  /// Get the data payload from log entry

  let data = function
    | LogEntry(_,_,_,d,_)     -> Some d
    | Snapshot(_,_,_,_,_,_,d) -> Some d
    | _                       -> None

  // ** LogEntry.nodes

  //                   _
  //   _ __   ___   __| | ___  ___
  //  | '_ \ / _ \ / _` |/ _ \/ __|
  //  | | | | (_) | (_| |  __/\__ \
  //  |_| |_|\___/ \__,_|\___||___/
  //
  /// Return the current log entry's nodes property, should it have one

  let nodes = function
    | Configuration(_,_,_,d,_)  -> Some d
    | Snapshot(_,_,_,_,_,d,_)   -> Some d
    | _                         -> None

  // ** LogEntry.changes

  //        _
  //    ___| |__   __ _ _ __   __ _  ___  ___
  //   / __| '_ \ / _` | '_ \ / _` |/ _ \/ __|
  //  | (__| | | | (_| | | | | (_| |  __/\__ \
  //   \___|_| |_|\__,_|_| |_|\__, |\___||___/
  //                          |___/
  //
  /// Return the old nodes property (or nothing) of the current JointConsensus
  /// log entry.

  let changes = function
    | JointConsensus(_,_,_,c,_) -> Some c
    | _                         -> None


  // ** LogEntry.at

  //         _
  //    __ _| |_
  //   / _` | __|
  //  | (_| | |_
  //   \__,_|\__|
  //
  /// ### Complexity: 0(n)

  let rec at idx log =
    let _extract idx' curr' prev' =
      match idx' with
      | _  when idx > idx' -> None
      | _  when idx = idx' -> Some curr'
      | _  ->
        match prev' with
        | Some more -> at idx more
        | _         -> None

    match log with
    | Configuration(_,idx',_,_,prev)  as curr -> _extract idx' curr prev
    | JointConsensus(_,idx',_,_,prev) as curr -> _extract idx' curr prev
    | LogEntry(_,idx',_,_,prev)       as curr -> _extract idx' curr prev
    | Snapshot(_,idx',_,lidx',_,_,_)  as curr ->
      match (idx',lidx') with
      | _ when idx <= idx'  -> Some curr
      | _ when idx <= lidx' -> Some curr
      | _                   -> None

  // ** LogEntry.until

  //               _   _ _
  //   _   _ _ __ | |_(_) |
  //  | | | | '_ \| __| | |
  //  | |_| | | | | |_| | |
  //   \__,_|_| |_|\__|_|_|
  //
  /// ### Complexity: 0(n)

  let rec until idx = function
    | Snapshot _ as curr -> Some curr

    | Configuration(_,idx',_,_,None) as curr ->
      match idx with
      | _ when idx = idx' -> Some curr
      | _                 -> None

    | Configuration(id,index,term,nodes,Some prev) ->
      match idx with
      | _ when idx = index ->
        Configuration(id,index,term,nodes,None)
        |> Some

      | _ when idx < index ->
        Configuration(id,index,term,nodes,until idx prev)
        |> Some

      | _ -> None

    | JointConsensus(_,index,_,_,None) as curr ->
      match idx with
      | _ when idx = index -> Some curr
      | _                  -> None

    | JointConsensus(id,index,term,changes,Some prev) ->
      match idx with
      | _ when idx = index ->
        JointConsensus(id,index,term,changes,None)
        |> Some

      | _ when idx < index ->
        JointConsensus(id,index,term,changes,until idx prev)
        |> Some

      | _ -> None

    | LogEntry(_,index,_,_,None) as curr ->
      match idx with
      | _ when idx = index -> Some curr
      | _                  -> None

    | LogEntry(id,index,term,data,Some prev) ->
      match idx with
      | _ when idx = index ->
        LogEntry(id,index,term,data,None)
        |> Some

      | _ when idx < index ->
        LogEntry(id,index,term,data,until idx prev)
        |> Some

      | _ -> None

  // ** LogEntry.untilExcluding

  ///              _   _ _ _____          _           _ _
  ///  _   _ _ __ | |_(_) | ____|_  _____| |_   _  __| (_)_ __   __ _
  /// | | | | '_ \| __| | |  _| \ \/ / __| | | | |/ _` | | '_ \ / _` |
  /// | |_| | | | | |_| | | |___ >  < (__| | |_| | (_| | | | | | (_| |
  ///  \__,_|_| |_|\__|_|_|_____/_/\_\___|_|\__,_|\__,_|_|_| |_|\__, |
  ///                                                           |___/
  /// ### Complextiy: O(n)

  let rec untilExcluding idx = function
    | Snapshot _ as curr -> Some curr

    | Configuration(id,index,term,nodes,Some prev) ->
      if idx >= index then
        None
      else
        Configuration(id,index,term,nodes,untilExcluding idx prev)
        |> Some

    | JointConsensus(id,index,term,changes,Some prev) ->
      if idx >= index then
        None
      else
        JointConsensus(id,index,term,changes,untilExcluding idx prev)
        |> Some

    | LogEntry(id,index,term,data,Some prev) ->
      if idx >= index then
        None
      else
        LogEntry(id,index,term,data,untilExcluding idx prev)
        |> Some

    | _ -> None

  // ** LogEntry.find

  ///  _____ _           _
  /// |  ___(_)_ __   __| |
  /// | |_  | | '_ \ / _` |
  /// |  _| | | | | | (_| |
  /// |_|   |_|_| |_|\__,_|
  ///
  /// Find an entry by its ID. Returns an option value.

  let rec find id log =
    let _extract id' curr' prev' =
      if id <> id' then
        match prev' with
        | Some other -> find id other
        | _          -> None
      else Some curr'

    match log with
    | LogEntry(id',_,_,_,prev)       as curr -> _extract id' curr prev
    | Configuration(id',_,_,_,prev)  as curr -> _extract id' curr prev
    | JointConsensus(id',_,_,_,prev) as curr -> _extract id' curr prev
    | Snapshot(id',_,_,_,_,_,_)      as curr ->
      if id' <> id then None else Some curr

  // ** LogEntry.make

  ///  __  __       _
  /// |  \/  | __ _| | _____
  /// | |\/| |/ _` | |/ / _ \
  /// | |  | | (_| |   <  __/
  /// |_|  |_|\__,_|_|\_\___|

  let make term data =
    LogEntry(Id.Create(), 0u, term, data, None)

  // ** LogEntry.mkConfig

  /// Add an Configuration log entry onto the queue
  ///
  /// ### Complexity: 0(1)

  let mkConfig term nodes =
    Configuration(Id.Create(), 0u, term, nodes, None)

  // ** LogEntry.mkConfigChange

  /// Add an intermediate configuration entry for 2-phase commit onto
  /// the log queue
  ///
  /// ### Complexity: 0(1)

  let mkConfigChange term changes =
    JointConsensus(Id.Create(), 0u, term, changes, None)

  let calculateChanges oldnodes newnodes =
    let changes =
      let additions =
        Array.fold
          (fun lst (newnode: RaftNode) ->
            match Array.tryFind (Node.getId >> (=) newnode.Id) oldnodes with
            | Some _ -> lst
            |      _ -> NodeAdded(newnode) :: lst) [] newnodes

      Array.fold
        (fun lst (oldnode: RaftNode) ->
          match Array.tryFind (Node.getId >> (=) oldnode.Id) newnodes with
          | Some _ -> lst
          | _ -> NodeRemoved(oldnode) :: lst) additions oldnodes
      |> List.toArray

    changes

  // ** LogEntry.pop

  ///  _ __   ___  _ __
  /// | '_ \ / _ \| '_ \
  /// | |_) | (_) | |_) |
  /// | .__/ \___/| .__/
  /// |_|         |_|
  ///
  /// Remove the latest entry on the log. Returns an option value (empty if no
  /// previous log value could be extracted).
  ///
  /// ### Complexity: 0(1)

  let pop = function
    | Configuration(_,_,_,_,prev)  -> prev
    | JointConsensus(_,_,_,_,prev) -> prev
    | LogEntry(_,_,_,_,prev)       -> prev
    | Snapshot _                   -> None

  // ** LogEntry.snapshot

  ///                            _           _
  ///  ___ _ __   __ _ _ __  ___| |__   ___ | |_
  /// / __| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  /// \__ \ | | | (_| | |_) \__ \ | | | (_) | |_
  /// |___/_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  ///                 |_|
  ///
  /// Compact the log database

  let snapshot nodes data log =
    let idx, term =
      match log with
      | LogEntry(_,idx,term,_,_)       -> idx,term
      | Configuration(_,idx,term,_,_)  -> idx,term
      | JointConsensus(_,idx,term,_,_) -> idx,term
      | Snapshot(_,idx,term,_,_,_,_)   -> idx,term
    in
      Snapshot(Id.Create(),idx + 1u,term,idx,term,nodes,data)

  // ** LogEntry.map

  ///  _ __ ___   __ _ _ __
  /// | '_ ` _ \ / _` | '_ \
  /// | | | | | | (_| | |_) |
  /// |_| |_| |_|\__,_| .__/
  ///                 |_|
  ///
  /// Map over a Logs<'a,'n> and return a list of results

  let rec map (f : RaftLogEntry -> 'b) entry =
    let _map curr prev =
      match prev with
      | Some previous -> f curr :: map f previous
      | _             -> [f curr]

    match entry with
    | Configuration(_,_,_,_,prev)  as curr -> _map curr prev
    | JointConsensus(_,_,_,_,prev) as curr -> _map curr prev
    | LogEntry(_,_,_,_,prev)       as curr -> _map curr prev
    | Snapshot _                   as curr -> _map curr None

  // ** LogEntry.foldl

  ///   __       _     _ _
  ///  / _| ___ | | __| | |
  /// | |_ / _ \| |/ _` | |
  /// |  _| (_) | | (_| | |
  /// |_|  \___/|_|\__,_|_|
  ///
  /// Fold over a Log and return an aggregate value

  let rec foldl (f : 'm -> RaftLogEntry -> 'm) (m : 'm) log =
    let _fold m curr prev =
      let _m = f m curr
      match prev with
      | Some previous -> foldl f _m previous
      | _             -> _m

    match log with
    | JointConsensus(_,_,_,_,prev) as curr -> _fold m curr prev
    | Configuration(_,_,_,_,prev)  as curr -> _fold m curr prev
    | LogEntry(_,_,_,_,prev)       as curr -> _fold m curr prev
    | Snapshot _                   as curr -> f m curr

  // ** LogEntry.foldr

  ///   __       _     _
  ///  / _| ___ | | __| |_ __
  /// | |_ / _ \| |/ _` | '__|
  /// |  _| (_) | | (_| | |
  /// |_|  \___/|_|\__,_|_|
  ///
  /// Fold over a Log and return an aggregate value

  let rec foldr (f : 'm -> RaftLogEntry -> 'm) (m : 'm)  = function
    | Configuration(_,_,_,_,Some prev)  as curr -> f (foldr f m prev) curr
    | Configuration(_,_,_,_,None)       as curr -> f m curr
    | JointConsensus(_,_,_,_,Some prev) as curr -> f (foldr f m prev) curr
    | JointConsensus(_,_,_,_,None)      as curr -> f m curr
    | LogEntry(_,_,_,_,Some prev)       as curr -> f (foldr f m prev) curr
    | LogEntry(_,_,_,_,None)            as curr -> f m curr
    | Snapshot _                        as curr -> f m curr

  // ** LogEntry.iter

  ///  _ _
  /// (_) |_ ___ _ __
  /// | | __/ _ \ '__|
  /// | | ||  __/ |
  /// |_|\__\___|_|
  ///
  /// Iterate over a log from the newest entry to the oldest.
  let iter (f : uint32 -> RaftLogEntry -> unit) (log : RaftLogEntry) =
    log.Iter f

  // ** LogEntry.aggregate

  ///     _                                    _
  ///    / \   __ _  __ _ _ __ ___  __ _  __ _| |_ ___
  ///   / _ \ / _` |/ _` | '__/ _ \/ _` |/ _` | __/ _ \
  ///  / ___ \ (_| | (_| | | |  __/ (_| | (_| | ||  __/
  /// /_/   \_\__, |\__, |_|  \___|\__, |\__,_|\__\___|
  ///         |___/ |___/          |___/
  ///
  /// Version of left-fold that implements short-circuiting by requiring the
  /// return value to be wrapped in `Continue<'a>`.

  let inline aggregate< ^m > (f : ^m -> RaftLogEntry -> Continue< ^m >) (m : ^m) log =
    // wrap the supplied function such that it takes a value lifted to
    // Continue to proactively stop calculating (what about passing a
    // closure instead?)
    let _folder (m : Continue< ^m >) _log =
      match m with
      | Cont v -> f v _log
      |      v -> v

    // short-circuiting inner function
    let rec _resFold (m : Continue< ^m >) (_log : RaftLogEntry) : Continue< ^m > =
      let _do curr prev =
        match m with
        | Cont _ ->
          // This is the typical left-fold: calculate the outter value
          // first, then the inner one (but stop once Ret comes along).
          match _folder m curr with
            | Cont _ as a -> _resFold a prev
            |           a -> a
        | _ -> m

      match _log with
      | Configuration(_,_,_,_,Some prev)  as curr -> _do curr prev
      | JointConsensus(_,_,_,_,Some prev) as curr -> _do curr prev
      | LogEntry(_,_,_,_,Some prev)       as curr -> _do curr prev
      | _  -> m

    // run and extract inner value
    match _resFold (Cont m) log with
    | Cont v -> v
    | Ret  v -> v

  // ** LogEntry.next

  let inline next< ^m > (m: ^m) : Continue< ^m > =
    Continue.next m

  // ** LogEntry.finish

  let inline finish< ^m > (m: ^m) : Continue< ^m > =
    Continue.finish m

  // ** LogEntry.last

  ///  _           _
  /// | | __ _ ___| |_
  /// | |/ _` / __| __|
  /// | | (_| \__ \ |_
  /// |_|\__,_|___/\__|
  ///
  /// Return the last (oldest) element of a log.

  let rec last = function
    | LogEntry(_,_,_,_,None)          as curr -> curr
    | LogEntry(_,_,_,_,Some prev)             -> last prev
    | Configuration(_,_,_,_,None)     as curr -> curr
    | Configuration(_,_,_,_,Some prev)        -> last prev
    | JointConsensus(_,_,_,_,None)    as curr -> curr
    | JointConsensus(_,_,_,_,Some prev)       -> last prev
    | Snapshot _                      as curr -> curr

  // ** LogEntry.head

  //  _                    _
  // | |__   ___  __ _  __| |
  // | '_ \ / _ \/ _` |/ _` |
  // | | | |  __/ (_| | (_| |
  // |_| |_|\___|\__,_|\__,_|

  let head = function
    | LogEntry(id,idx,term,data,Some _) ->
      LogEntry(id,idx,term,data,None)

    | Configuration(id,idx,term,nodes,Some _) ->
      Configuration(id,idx,term,nodes,None)

    | JointConsensus(id,idx,term,changes,Some _) ->
      JointConsensus(id,idx,term,changes,None)

    | curr -> curr

  // ** LogEntry.rewrite

  //                         _ _
  //  _ __ _____      ___ __(_) |_ ___
  // | '__/ _ \ \ /\ / / '__| | __/ _ \
  // | | |  __/\ V  V /| |  | | ||  __/
  // |_|  \___| \_/\_/ |_|  |_|\__\___|

  let rec rewrite entry =
    match entry with
    | Configuration(id, _, _, nodes, None) ->
      Configuration(id, 1u, 1u, nodes, None)

    | Configuration(id, _, term, nodes, Some prev) ->
      let previous = rewrite prev
      Configuration(id, index previous + 1u, term, nodes, Some previous)

    | JointConsensus(id, _, term, changes, None) ->
      JointConsensus(id, 1u, term, changes, None)

    | JointConsensus(id, _, term, changes, Some prev) ->
      let previous = rewrite prev
      JointConsensus(id, index previous + 1u, term, changes, Some previous)

    | LogEntry(id, _, term, data, None) ->
      LogEntry(id, 1u, term, data, None)

    | LogEntry(id, _, term, data, Some prev) ->
      let previous = rewrite prev
      LogEntry(id, index previous + 1u, term, data, Some previous)

    | Snapshot(id, _, term, _, pterm, nodes, data) ->
      Snapshot(id, 2u, term, 1u, pterm, nodes, data)

  // ** LogEntry.append

  ///                                   _
  ///   __ _ _ __  _ __   ___ _ __   __| |
  ///  / _` | '_ \| '_ \ / _ \ '_ \ / _` |
  /// | (_| | |_) | |_) |  __/ | | | (_| |
  ///  \__,_| .__/| .__/ \___|_| |_|\__,_|
  ///       |_|   |_|
  ///
  /// Append newer entries to older entries

  let append (newer : RaftLogEntry) (older : RaftLogEntry) =
    let _aggregator (_log : RaftLogEntry) (_entry : RaftLogEntry) =
      if getId _log = getId _entry then
        _log
      else
        let nextIdx = index _log + 1u
        match _entry with
        | Configuration(id, _, term, nodes, _) ->
          Configuration(id, nextIdx, term, nodes, Some _log)

        | JointConsensus(id, _, term, changes, _) ->
          JointConsensus(id, nextIdx, term, changes, Some _log)

        | LogEntry(id, _, term, data, _)    ->
          LogEntry(id, nextIdx, term, data, Some _log)

        | Snapshot(id, _, term, lidx, lterm, nodes, data) ->
          Snapshot(id, nextIdx, term, lidx, lterm, nodes, data)

    // find the last shared ancestor
    let last = last newer
    let lcd = find (getId last) older

    match lcd with
    | Some ancestor ->
      match pop ancestor with
      | Some entry ->
        // there is a least common denominator entry
        foldr _aggregator entry newer
      | _ -> newer
    | None ->
      // no overlap found
      foldr _aggregator older newer

  // ** LogEntry.lastIndex

  //  _           _   ___           _
  // | | __ _ ___| |_|_ _|_ __   __| | _____  __
  // | |/ _` / __| __|| || '_ \ / _` |/ _ \ \/ /
  // | | (_| \__ \ |_ | || | | | (_| |  __/>  <
  // |_|\__,_|___/\__|___|_| |_|\__,_|\___/_/\_\

  let lastIndex = function
    | Snapshot(_,_,_,idx,_,_,_) -> Some idx
    | _                         -> None

  // ** LogEntry.lastTerm

  //  _           _  _____
  // | | __ _ ___| ||_   _|__ _ __ _ __ ___
  // | |/ _` / __| __|| |/ _ \ '__| '_ ` _ \
  // | | (_| \__ \ |_ | |  __/ |  | | | | | |
  // |_|\__,_|___/\__||_|\___|_|  |_| |_| |_|

  let lastTerm = function
    | Snapshot(_,_,_,_,term,_,_) -> Some term
    | _                          -> None

  // ** LogEntry.firstIndex

  //   __ _          _   ___           _
  //  / _(_)_ __ ___| |_|_ _|_ __   __| | _____  __
  // | |_| | '__/ __| __|| || '_ \ / _` |/ _ \ \/ /
  // |  _| | |  \__ \ |_ | || | | | (_| |  __/>  <
  // |_| |_|_|  |___/\__|___|_| |_|\__,_|\___/_/\_\

  let rec firstIndex (t: Term) (entry: RaftLogEntry) =
    let getIdx idx term prev =
      match prev with
      | Some log ->
        if term = t then
          match firstIndex t log with
          | Some _ as result -> result
          | _                -> Some idx
        elif term > t then
          match firstIndex t log with
          | Some _ as result -> result
          | _                -> None
        else None
      | None ->
        if term = t then
          Some idx
        else
          None

    match entry with
    | LogEntry(_,idx,term,_, prev)        -> getIdx idx term prev
    | Configuration(_,idx,term,_, prev)   -> getIdx idx term prev
    | JointConsensus(_,idx,term,_,prev)   -> getIdx idx term prev
    | Snapshot(_,idx,term,lidx,lterm,_,_) ->
      if term = t then
        Some idx
      elif lterm = t then
        Some lidx
      else
        None

  // ** LogEntry.getn

  //             _
  //   __ _  ___| |_ _ __
  //  / _` |/ _ \ __| '_ \
  // | (_| |  __/ |_| | | |
  //  \__, |\___|\__|_| |_|
  //  |___/
  let rec getn count log =
    if count = 0u then
      None
    else
      let newcnt = count - 1u
      match log with
      | Configuration(_,_,_,_, None) as curr -> Some curr
      | JointConsensus(_,_,_,_,None) as curr -> Some curr
      | LogEntry(_,_,_,_, None)      as curr -> Some curr
      | Snapshot _                   as curr -> Some curr

      | Configuration(id,idx,term,nodes, Some prev) ->
        Configuration(id,idx,term,nodes, getn newcnt prev)
        |> Some

      | JointConsensus(id,idx,term,changes,Some prev) ->
        JointConsensus(id,idx,term,changes,getn newcnt prev)
        |> Some

      | LogEntry(id,idx,term,data, Some prev) ->
        LogEntry(id,idx,term,data, getn newcnt prev)
        |> Some

  // ** LogEntry.contains

  //                  _        _
  //   ___ ___  _ __ | |_ __ _(_)_ __  ___
  //  / __/ _ \| '_ \| __/ _` | | '_ \/ __|
  // | (_| (_) | | | | || (_| | | | | \__ \
  //  \___\___/|_| |_|\__\__,_|_|_| |_|___/

  let rec contains (f: RaftLogEntry -> bool) = function
    | LogEntry(_,_,_,_,Some prev) as this ->
      if f this then true else contains f prev

    | LogEntry(_,_,_,_,None) as this -> f this

    | Configuration(_,_,_,_,Some prev) as this ->
      if f this then true else contains f prev

    | Configuration(_,_,_,_,None) as this -> f this

    | JointConsensus(_,_,_,_,Some prev) as this ->
      if f this then true else contains f prev

    | JointConsensus(_,_,_,_,None) as this -> f this

    | Snapshot _ as this -> f this

  // ** LogEntry.sanitize

  //  ____              _ _   _
  // / ___|  __ _ _ __ (_) |_(_)_______
  // \___ \ / _` | '_ \| | __| |_  / _ \
  //  ___) | (_| | | | | | |_| |/ /  __/
  // |____/ \__,_|_| |_|_|\__|_/___\___|

  /// Make sure the current log entry is a singleton (followed by no entries).
  let sanitize term = function
    | Configuration(id,_,term,nodes,_)    -> Configuration(id,0u,term,nodes,None)
    | JointConsensus(id,_,term,changes,_) -> JointConsensus(id,0u,term,changes,None)
    | LogEntry(id,_,_,data,_)             -> LogEntry(id,0u,term,data,None)
    | Snapshot _ as snapshot              -> snapshot
