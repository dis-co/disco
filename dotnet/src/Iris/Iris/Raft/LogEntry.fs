namespace Iris.Raft

// * Imports

open System
open System.Collections
open Iris.Core
open Iris.Serialization

#if FABLE_COMPILER

open Iris.Core.FlatBuffers

#else

open FlatBuffers
open SharpYaml
open SharpYaml.Serialization

type SnapshotYaml() =
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable Index     : Index
  [<DefaultValue>] val mutable Term      : Term
  [<DefaultValue>] val mutable LastIndex : Index
  [<DefaultValue>] val mutable LastTerm  : Term
  [<DefaultValue>] val mutable Members   : RaftMemberYaml array
  [<DefaultValue>] val mutable Commit    : string

#endif

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
///   // Member Configuration Entry
///   | Configuration of
///     Id       : Id              *        // unique id of configuration entry
///     Index    : Index           *        // index in log
///     Term     : Term            *        // term when entry was added to log
///     Members    : RaftMember array  *        // new mem configuration
///     Previous : LogEntry option          // previous log entry, if applicable
///
///   // Entry type for configuration changes
///   | JointConsensus of
///     Id       : Id                 *     // unique identified of entry
///     Index    : Index              *     // index of entry in log
///     Term     : Term               *     // term when entry was added to log
///     Changes  : ConfigChange array *     // changes to mem configuration
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
///     Members     : RaftMember array *        // mem configuration
///     Data      : StateMachine            // state machine data
///

type RaftLogEntry =

  // Member Configuration Entry
  | Configuration of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Members  : RaftMember array  *
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
    Members   : RaftMember array *
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
      | Configuration(id,idx,term,mems,Some prev) ->
        sprintf "Configuration(id: %s idx: %A term: %A mems: %s)\n%s"
          (string id)
          idx
          term
          (Array.fold (fun m (n: RaftMember) -> sprintf "%s, %s" m (string n.Id)) "" mems)
          (string prev)

      | Configuration(id,idx,term,mems,_) ->
        sprintf "Configuration(id: %s idx: %A term: %A mems: %s)"
          (string id)
          idx
          term
          (Array.fold (fun m (n: RaftMember) -> sprintf "%s, %s" m (string n.Id)) "" mems)

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
      | Configuration(id,index,term,mems,_)          ->
        let id = string id |> builder.CreateString
        let mems = Array.map (Binary.toOffset builder) mems
        let nvec = ConfigurationFB.CreateMembersVector(builder, mems)

        ConfigurationFB.StartConfigurationFB(builder)
        ConfigurationFB.AddId(builder, id)
        ConfigurationFB.AddIndex(builder, index)
        ConfigurationFB.AddTerm(builder, term)
        ConfigurationFB.AddMembers(builder, nvec)

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
      | Snapshot(id,index,term,lidx,lterm,mems,data) ->
        let id = string id |> builder.CreateString
        let mems = Array.map (Binary.toOffset builder) mems
        let nvec = SnapshotFB.CreateMembersVector(builder, mems)
        let data = data.ToOffset(builder)

        SnapshotFB.StartSnapshotFB(builder)
        SnapshotFB.AddId(builder, id)
        SnapshotFB.AddIndex(builder, index)
        SnapshotFB.AddTerm(builder, term)
        SnapshotFB.AddLastIndex(builder, lidx)
        SnapshotFB.AddLastTerm(builder, lterm)
        SnapshotFB.AddMembers(builder, nvec)
        SnapshotFB.AddData(builder, data)

        let entry = SnapshotFB.EndSnapshotFB(builder)

        buildLogFB LogTypeFB.SnapshotFB entry.Value

    let arr = Array.zeroCreate (self.Depth |> int)
    self.Iter (fun i (log: RaftLogEntry) -> arr.[int i] <- toOffset log)
    arr

  // ** ParseLogFB

  /// ## Parse a single log entry, adding its sibling mem
  ///
  /// Parses a single log entry, adding the passed sibling mem, if any. If an error occurs, the
  /// entire parsing process fails. With the first error.
  ///
  /// ### Signature:
  /// - fb: LogFB FlatBuffer object
  /// - sibling: an sibling (None also legal, for the first mem), or the previous error
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

            // parse all mems in this log entry. if this fails, the error will be propagated up the
            // call chain
            let! mems =
              let arr = Array.zeroCreate logentry.MembersLength
              Array.fold
                (fun (m: Either<IrisError,int * RaftMember array>) _ -> either {
                  let! (i, arr) = m
                  let! mem =
                    let value = logentry.Members(i)
                    if value.HasValue then
                      value.Value
                      |> RaftMember.FromFB
                    else
                      "Could not parse empty MemberFB value"
                      |> Error.asParseError "StateMachine.ParseLogFB"
                      |> Either.fail
                  arr.[i] <- mem
                  return (i + i, arr)
                })
                (Right (0, arr))
                arr
              |> Either.map snd

            // successfully parsed this LogEntry, so return it wrapped in an option
            return Configuration(Id logentry.Id,
                                 logentry.Index,
                                 logentry.Term,
                                 mems,
                                 previous)
                   |> Some
          else
            return! "Could not parse empty LogTypeFB.ConfigurationFB"
                    |> Error.asParseError "StateMachine.ParseLogFB"
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
                      |> Error.asParseError "StateMachine.FromFB"
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
              |> Error.asParseError "StateMachine.ParseLogFB"
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
                |> Error.asParseError "StateMachine.ParseLogFB"
                |> Either.fail
          else
            return!
              "Could not parse empty LogTypeFB.LogEntry"
              |> Error.asParseError "StateMachine.ParseLogFB"
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

              let! mems =
                let arr = Array.zeroCreate logentry.MembersLength
                Array.fold
                  (fun (m: Either<IrisError, int * RaftMember array>) _ -> either {
                    let! (i, mems) = m

                    let! mem =
                      let value = logentry.Members(i)
                      if value.HasValue then
                        value.Value
                        |> RaftMember.FromFB
                      else
                        "Could not parse empty RaftMemberFB"
                        |> Error.asParseError "StateMachine.ParseLogFB"
                        |> Either.fail

                    mems.[i] <- mem
                    return (i + 1, mems)
                  })
                  (Right (0, arr))
                  arr
                |> Either.map snd

              return Snapshot(id,
                              logentry.Index,
                              logentry.Term,
                              logentry.LastIndex,
                              logentry.LastTerm,
                              mems,
                              state)
                     |> Some
            else
              return!
                "Could not parse empty StateMachineFB"
                |> Error.asParseError "StateMachine.ParseLogFB"
                |> Either.fail
          else
            return!
              "Could not parse empty LogTypeFB.SnapshotFB"
              |> Error.asParseError "StateMachine.ParseLogFB"
              |> Either.fail
        }

      | x ->
        sprintf "Could not parse unknown LogTypeFB; %A" x
        |> Error.asParseError "StateMachine.ParseLogFB"
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

  //     _                 _
  //    / \   ___ ___  ___| |_
  //   / _ \ / __/ __|/ _ \ __|
  //  / ___ \\__ \__ \  __/ |_
  // /_/   \_\___/___/\___|\__|

  member log.AssetPath
    with get () =
      Constants.RAFT_DIRECTORY </> Constants.SNAPSHOT_FILENAME + Constants.ASSET_EXTENSION

  member log.Save (basePath: FilePath) =
    match log with
    | Snapshot(id, idx, term, lastidx, lastterm, mems, _) ->
      either {
        let serializer = new Serializer()
        let path = basePath </> Asset.path log
        use! repo = Git.Repo.repository basePath
        let! last = Git.Repo.commits repo |> Git.Repo.elementAt 0
        let yaml = SnapshotYaml()
        yaml.Id <- string id
        yaml.Index <- idx
        yaml.Term <- term
        yaml.LastIndex <- lastidx
        yaml.LastTerm <- lastterm
        yaml.Members <- Array.map Yaml.toYaml mems
        yaml.Commit <- last.Sha
        let data = serializer.Serialize yaml
        let! _ = Asset.write path (Payload data)
        return ()
      }
    | _ ->
      "Only snapshots can be saved"
      |> Error.asAssetError "LogEntry.Save"
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

  // ** LogEntry.mems

  //                   _
  //   _ __   ___   __| | ___  ___
  //  | '_ \ / _ \ / _` |/ _ \/ __|
  //  | | | | (_) | (_| |  __/\__ \
  //  |_| |_|\___/ \__,_|\___||___/
  //
  /// Return the current log entry's mems property, should it have one

  let mems = function
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
  /// Return the old mems property (or nothing) of the current JointConsensus
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

    | Configuration(id,index,term,mems,Some prev) ->
      match idx with
      | _ when idx = index ->
        Configuration(id,index,term,mems,None)
        |> Some

      | _ when idx < index ->
        Configuration(id,index,term,mems,until idx prev)
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

    | Configuration(id,index,term,mems,Some prev) ->
      if idx >= index then
        None
      else
        Configuration(id,index,term,mems,untilExcluding idx prev)
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

  let mkConfig term mems =
    Configuration(Id.Create(), 0u, term, mems, None)

  // ** LogEntry.mkConfigChange

  /// Add an intermediate configuration entry for 2-phase commit onto
  /// the log queue
  ///
  /// ### Complexity: 0(1)

  let mkConfigChange term changes =
    JointConsensus(Id.Create(), 0u, term, changes, None)

  let calculateChanges oldmems newmems =
    let changes =
      let additions =
        Array.fold
          (fun lst (newmem: RaftMember) ->
            match Array.tryFind (Member.getId >> (=) newmem.Id) oldmems with
            | Some _ -> lst
            |      _ -> MemberAdded(newmem) :: lst) [] newmems

      Array.fold
        (fun lst (oldmem: RaftMember) ->
          match Array.tryFind (Member.getId >> (=) oldmem.Id) newmems with
          | Some _ -> lst
          | _ -> MemberRemoved(oldmem) :: lst) additions oldmems
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

  let snapshot mems data log =
    let idx, term =
      match log with
      | LogEntry(_,idx,term,_,_)       -> idx,term
      | Configuration(_,idx,term,_,_)  -> idx,term
      | JointConsensus(_,idx,term,_,_) -> idx,term
      | Snapshot(_,idx,term,_,_,_,_)   -> idx,term
    in
      Snapshot(Id.Create(),idx + 1u,term,idx,term,mems,data)

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

    | Configuration(id,idx,term,mems,Some _) ->
      Configuration(id,idx,term,mems,None)

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
    | Configuration(id, _, _, mems, None) ->
      Configuration(id, 1u, 1u, mems, None)

    | Configuration(id, _, term, mems, Some prev) ->
      let previous = rewrite prev
      Configuration(id, index previous + 1u, term, mems, Some previous)

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

    | Snapshot(id, _, term, _, pterm, mems, data) ->
      Snapshot(id, 2u, term, 1u, pterm, mems, data)

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
        | Configuration(id, _, term, mems, _) ->
          Configuration(id, nextIdx, term, mems, Some _log)

        | JointConsensus(id, _, term, changes, _) ->
          JointConsensus(id, nextIdx, term, changes, Some _log)

        | LogEntry(id, _, term, data, _)    ->
          LogEntry(id, nextIdx, term, data, Some _log)

        | Snapshot(id, _, term, lidx, lterm, mems, data) ->
          Snapshot(id, nextIdx, term, lidx, lterm, mems, data)

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

      | Configuration(id,idx,term,mems, Some prev) ->
        Configuration(id,idx,term,mems, getn newcnt prev)
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
    | Configuration(id,_,term,mems,_)    -> Configuration(id,0u,term,mems,None)
    | JointConsensus(id,_,term,changes,_) -> JointConsensus(id,0u,term,changes,None)
    | LogEntry(id,_,_,data,_)             -> LogEntry(id,0u,term,data,None)
    | Snapshot _ as snapshot              -> snapshot
