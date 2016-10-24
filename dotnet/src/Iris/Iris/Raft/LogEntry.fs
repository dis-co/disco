namespace Iris.Raft

open System
open System.Collections
open Iris.Core
open Iris.Serialization.Raft

#if JAVASCRIPT

open Iris.Core.FlatBuffers

#else

open FlatBuffers

#endif

type LogYaml(tipe, id, idx, term, lidx, lterm, changes, nodes, data, prev)
  as self =

  [<DefaultValue>] val mutable LogType   : string
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable Index     : Index
  [<DefaultValue>] val mutable Term      : Term
  [<DefaultValue>] val mutable LastIndex : Index
  [<DefaultValue>] val mutable LastTerm  : Term
  [<DefaultValue>] val mutable Changes   : ConfigChangeYaml array
  [<DefaultValue>] val mutable Nodes     : string array // only RaftNode Id's
  [<DefaultValue>] val mutable Data      : string       // commit Id of snapshot
  [<DefaultValue>] val mutable Previous  : string

  new () = new LogYaml(null, null, 0u, 0u, 0u, 0u, [| |], [| |], null, null)

  do
    self.LogType   <- tipe
    self.Id        <- id
    self.Index     <- idx
    self.Term      <- term
    self.LastIndex <- lidx
    self.LastTerm  <- lterm
    self.Changes   <- changes
    self.Nodes     <- nodes
    self.Data      <- data
    self.Previous  <- prev

  member self.ToLogEntry (nodes: RaftNode array) =
    match self.LogType with
    | "Configuration"  -> failwith "in a moment"
    | "JointConsensus" -> failwith "in a moment"
    | "LogEntry"       -> failwith "in a moment"
    | "Snapshot"       -> failwith "in a moment"
    | _                -> None

  static member Configuration (id, idx, term, nodes, prev) =
    new LogYaml("Configuration"
               , string id
               , idx
               , term
               , 0u
               , 0u
               , [| |]
               , nodes
               , null
               , prev)

  static member JointConsensus (id, idx, term, changes, prev) =
    new LogYaml("JointConsensus"
               , string id
               , idx
               , term
               , 0u
               , 0u
               , changes
               , [| |]
               , null
               , prev)

  static member LogEntry (id, idx, term, sm, prev) =
    new LogYaml("LogEntry"
               , string id
               , idx
               , term
               , 0u
               , 0u
               , [| |]
               , [| |]
               , sm
               , prev)

  static member Snapshot (id, idx, term, lidx, lterm, nodes, sm) =
    new LogYaml("LogEntry"
               , string id
               , idx
               , term
               , 0u
               , 0u
               , [| |]
               , nodes
               , sm
               , null)

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

and LogEntry =

  // Node Configuration Entry
  | Configuration of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Nodes    : RaftNode array  *
    Previous : LogEntry option

  // Entry type for configuration changes
  | JointConsensus of
    Id       : Id                     *
    Index    : Index                  *
    Term     : Term                   *
    Changes  : ConfigChange array     *
    Previous : LogEntry option

  // Regular Log Entries
  | LogEntry   of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Data     : StateMachine    *
    Previous : LogEntry option

  | Snapshot   of
    Id        : Id             *
    Index     : Index          *
    Term      : Term           *
    LastIndex : Index          *
    LastTerm  : Term           *
    Nodes     : RaftNode array *
    Data      : StateMachine

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


  //   _     _
  //  (_) __| |
  //  | |/ _` |
  //  | | (_| |
  //  |_|\__,_|
  //
  /// Return the current log entry id.

  static member getId = function
    | Configuration(id,_,_,_,_)    -> id
    | JointConsensus(id,_,_,_,_) -> id
    | LogEntry(id,_,_,_,_)         -> id
    | Snapshot(id,_,_,_,_,_,_)     -> id

  //  _      ____             __ _        ____ _
  // (_)___ / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
  // | / __| |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
  // | \__ \ |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
  // |_|___/\____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
  //                               |___/                         |___/

  static member isConfigChange = function
    | JointConsensus _ -> true
    |                _ -> false

  //  _      ____             __ _                       _   _
  // (_)___ / ___|___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
  // | / __| |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
  // | \__ \ |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
  // |_|___/\____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
  //                               |___/

  static member isConfiguration = function
    | Configuration _ -> true
    |               _ -> false

  //       _            _   _
  //    __| | ___ _ __ | |_| |__
  //   / _` |/ _ \ '_ \| __| '_ \
  //  | (_| |  __/ |_) | |_| | | |
  //   \__,_|\___| .__/ \__|_| |_|
  //             |_|
  //
  /// compute the actual depth of the log (e.g. for compacting)

  static member depth (log: LogEntry) =
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
    _depth 0u log

  //   _           _
  //  (_)_ __   __| | _____  __
  //  | | '_ \ / _` |/ _ \ \/ /
  //  | | | | | (_| |  __/>  <
  //  |_|_| |_|\__,_|\___/_/\_\
  //
  /// Return the index of the current log entry.

  static member index = function
    | Configuration(_,idx,_,_,_)  -> idx
    | JointConsensus(_,idx,_,_,_) -> idx
    | LogEntry(_,idx,_,_,_)       -> idx
    | Snapshot(_,idx,_,_,_,_,_)   -> idx

  //                        ___           _
  //   _ __  _ __ _____   _|_ _|_ __   __| | _____  __
  //  | '_ \| '__/ _ \ \ / /| || '_ \ / _` |/ _ \ \/ /
  //  | |_) | | |  __/\ V / | || | | | (_| |  __/>  <
  //  | .__/|_|  \___| \_/ |___|_| |_|\__,_|\___/_/\_\
  //  |_|
  //
  /// Return the index of the previous element if present.

  static member prevIndex = function
    | Configuration(_,_,_,_,Some prev)  -> Some (LogEntry.index prev)
    | JointConsensus(_,_,_,_,Some prev) -> Some (LogEntry.index prev)
    | LogEntry(_,_,_,_,Some prev)       -> Some (LogEntry.index prev)
    | Snapshot(_,_,_,idx,_,_,_)         -> Some idx
    | _                                 -> None

  //   _
  //  | |_ ___ _ __ _ __ ___
  //  | __/ _ \ '__| '_ ` _ \
  //  | ||  __/ |  | | | | | |
  //   \__\___|_|  |_| |_| |_|
  //
  /// Extract the `Term` field from a LogEntry

  static member term = function
    | Configuration(_,_,term,_,_)  -> term
    | JointConsensus(_,_,term,_,_) -> term
    | LogEntry(_,_,term,_,_)       -> term
    | Snapshot(_,_,term,_,_,_,_)   -> term

  //                        _____
  //   _ __  _ __ _____   _|_   _|__ _ __ _ __ ___
  //  | '_ \| '__/ _ \ \ / / | |/ _ \ '__| '_ ` _ \
  //  | |_) | | |  __/\ V /  | |  __/ |  | | | | | |
  //  | .__/|_|  \___| \_/   |_|\___|_|  |_| |_| |_|
  //  |_|
  //
  /// Return the previous elements' term, if present.

  static member prevTerm = function
    | Configuration(_,_,_,_,Some prev)  -> Some (LogEntry.term prev)
    | JointConsensus(_,_,_,_,Some prev) -> Some (LogEntry.term prev)
    | LogEntry(_,_,_,_,Some prev)       -> Some (LogEntry.term prev)
    | Snapshot(_,_,_,_,term,_,_)        -> Some term
    | _                                 -> None

  //                        _____       _
  //   _ __  _ __ _____   _| ____|_ __ | |_ _ __ _   _
  //  | '_ \| '__/ _ \ \ / /  _| | '_ \| __| '__| | | |
  //  | |_) | | |  __/\ V /| |___| | | | |_| |  | |_| |
  //  | .__/|_|  \___| \_/ |_____|_| |_|\__|_|   \__, |
  //  |_|                                        |___/
  //
  /// Return the previous entry, should there be one.

  static member prevEntry = function
    | Configuration(_,_,_,_,prev)  -> prev
    | JointConsensus(_,_,_,_,prev) -> prev
    | LogEntry(_,_,_,_,prev)       -> prev
    | Snapshot _                   -> None

  //       _       _
  //    __| | __ _| |_ __ _
  //   / _` |/ _` | __/ _` |
  //  | (_| | (_| | || (_| |
  //   \__,_|\__,_|\__\__,_|
  //
  /// Get the data payload from log entry

  static member data = function
    | LogEntry(_,_,_,d,_)     -> Some d
    | Snapshot(_,_,_,_,_,_,d) -> Some d
    | _                       -> None

  //                   _
  //   _ __   ___   __| | ___  ___
  //  | '_ \ / _ \ / _` |/ _ \/ __|
  //  | | | | (_) | (_| |  __/\__ \
  //  |_| |_|\___/ \__,_|\___||___/
  //
  /// Return the current log entry's nodes property, should it have one

  static member nodes = function
    | Configuration(_,_,_,d,_)  -> Some d
    | Snapshot(_,_,_,_,_,d,_)   -> Some d
    | _                         -> None

  //        _
  //    ___| |__   __ _ _ __   __ _  ___  ___
  //   / __| '_ \ / _` | '_ \ / _` |/ _ \/ __|
  //  | (__| | | | (_| | | | | (_| |  __/\__ \
  //   \___|_| |_|\__,_|_| |_|\__, |\___||___/
  //                          |___/
  //
  /// Return the old nodes property (or nothing) of the current JointConsensus
  /// log entry.

  static member changes = function
    | JointConsensus(_,_,_,c,_) -> Some c
    | _                         -> None

  //         _
  //    __ _| |_
  //   / _` | __|
  //  | (_| | |_
  //   \__,_|\__|
  //
  /// ### Complexity: 0(n)

  static member at idx log =
    let _extract idx' curr' prev' =
      match idx' with
        | _  when idx > idx' -> None
        | _  when idx = idx' -> Some curr'
        | _  ->
          match prev' with
            | Some more -> LogEntry.at idx more
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

  //               _   _ _
  //   _   _ _ __ | |_(_) |
  //  | | | | '_ \| __| | |
  //  | |_| | | | | |_| | |
  //   \__,_|_| |_|\__|_|_|
  //
  /// ### Complexity: 0(n)

  static member until idx = function
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
          Configuration(id,index,term,nodes,LogEntry.until idx prev)
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
          JointConsensus(id,index,term,changes,LogEntry.until idx prev)
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
          LogEntry(id,index,term,data,LogEntry.until idx prev)
          |> Some

        | _ -> None

  ///              _   _ _ _____          _           _ _
  ///  _   _ _ __ | |_(_) | ____|_  _____| |_   _  __| (_)_ __   __ _
  /// | | | | '_ \| __| | |  _| \ \/ / __| | | | |/ _` | | '_ \ / _` |
  /// | |_| | | | | |_| | | |___ >  < (__| | |_| | (_| | | | | | (_| |
  ///  \__,_|_| |_|\__|_|_|_____/_/\_\___|_|\__,_|\__,_|_|_| |_|\__, |
  ///                                                           |___/
  /// ### Complextiy: O(n)

  static member untilExcluding idx = function
    | Snapshot _ as curr -> Some curr

    | Configuration(id,index,term,nodes,Some prev) ->
      if idx >= index
      then None
      else
        Configuration(id,index,term,nodes,LogEntry.untilExcluding idx prev)
        |> Some

    | JointConsensus(id,index,term,changes,Some prev) ->
      if idx >= index
      then None
      else
        JointConsensus(id,index,term,changes,LogEntry.untilExcluding idx prev)
        |> Some

    | LogEntry(id,index,term,data,Some prev) ->
      if idx >= index
      then None
      else
        LogEntry(id,index,term,data,LogEntry.untilExcluding idx prev)
        |> Some

    | _ -> None

  ///  _____ _           _
  /// |  ___(_)_ __   __| |
  /// | |_  | | '_ \ / _` |
  /// |  _| | | | | | (_| |
  /// |_|   |_|_| |_|\__,_|
  ///
  /// Find an entry by its ID. Returns an option value.

  static member find id log =
    let _extract id' curr' prev' =
      if id <> id' then
        match prev' with
          | Some other -> LogEntry.find id other
          | _          -> None
      else Some curr'

    match log with
    | LogEntry(id',_,_,_,prev)       as curr -> _extract id' curr prev
    | Configuration(id',_,_,_,prev)  as curr -> _extract id' curr prev
    | JointConsensus(id',_,_,_,prev) as curr -> _extract id' curr prev
    | Snapshot(id',_,_,_,_,_,_)      as curr ->
      if id' <> id then None else Some curr

  ///  __  __       _
  /// |  \/  | __ _| | _____
  /// | |\/| |/ _` | |/ / _ \
  /// | |  | | (_| |   <  __/
  /// |_|  |_|\__,_|_|\_\___|

  static member make term data =
    LogEntry(Id.Create(), 0u, term, data, None)


  /// Add an Configuration log entry onto the queue
  ///
  /// ### Complexity: 0(1)

  static member mkConfig term nodes =
    Configuration(Id.Create(), 0u, term, nodes, None)

  /// Add an intermediate configuration entry for 2-phase commit onto
  /// the log queue
  ///
  /// ### Complexity: 0(1)

  static member mkConfigChange term oldnodes newnodes =
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

    JointConsensus(Id.Create(), 0u, term, changes, None)

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

  static member pop = function
    | Configuration(_,_,_,_,prev)  -> prev
    | JointConsensus(_,_,_,_,prev) -> prev
    | LogEntry(_,_,_,_,prev)       -> prev
    | Snapshot _                   -> None

  ///                            _           _
  ///  ___ _ __   __ _ _ __  ___| |__   ___ | |_
  /// / __| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  /// \__ \ | | | (_| | |_) \__ \ | | | (_) | |_
  /// |___/_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  ///                 |_|
  ///
  /// Compact the log database

  static member snapshot nodes data = function
    | LogEntry(_,idx,term,_,_)       -> Snapshot(Id.Create(),idx + 1u,term,idx,term,nodes,data)
    | Configuration(_,idx,term,_,_)  -> Snapshot(Id.Create(),idx + 1u,term,idx,term,nodes,data)
    | JointConsensus(_,idx,term,_,_) -> Snapshot(Id.Create(),idx + 1u,term,idx,term,nodes,data)
    | Snapshot(_,idx,term,_,_,_,_)   -> Snapshot(Id.Create(),idx + 1u,term,idx,term,nodes,data)

  ///  _ __ ___   __ _ _ __
  /// | '_ ` _ \ / _` | '_ \
  /// | | | | | | (_| | |_) |
  /// |_| |_| |_|\__,_| .__/
  ///                 |_|
  ///
  /// Map over a Logs<'a,'n> and return a list of results

  static member map (f : LogEntry -> 'b) entry =
    let _map curr prev =
      match prev with
        | Some previous -> f curr :: LogEntry.map f previous
        | _             -> [f curr]

    match entry with
      | Configuration(_,_,_,_,prev)  as curr -> _map curr prev
      | JointConsensus(_,_,_,_,prev) as curr -> _map curr prev
      | LogEntry(_,_,_,_,prev)       as curr -> _map curr prev
      | Snapshot _                   as curr -> _map curr None

  ///   __       _     _ _
  ///  / _| ___ | | __| | |
  /// | |_ / _ \| |/ _` | |
  /// |  _| (_) | | (_| | |
  /// |_|  \___/|_|\__,_|_|
  ///
  /// Fold over a Log and return an aggregate value

  static member foldl (f : 'm -> LogEntry -> 'm) (m : 'm) log =
    let _fold m curr prev =
      let _m = f m curr
      match prev with
      | Some previous -> LogEntry.foldl f _m previous
      | _             -> _m

    match log with
    | JointConsensus(_,_,_,_,prev) as curr -> _fold m curr prev
    | Configuration(_,_,_,_,prev)  as curr -> _fold m curr prev
    | LogEntry(_,_,_,_,prev)       as curr -> _fold m curr prev
    | Snapshot _                   as curr -> f m curr

  ///   __       _     _
  ///  / _| ___ | | __| |_ __
  /// | |_ / _ \| |/ _` | '__|
  /// |  _| (_) | | (_| | |
  /// |_|  \___/|_|\__,_|_|
  ///
  /// Fold over a Log and return an aggregate value

  static member foldr (f : 'm -> LogEntry -> 'm) (m : 'm)  = function
    | Configuration(_,_,_,_,Some prev)  as curr -> f (LogEntry.foldr f m prev) curr
    | Configuration(_,_,_,_,None)       as curr -> f m curr
    | JointConsensus(_,_,_,_,Some prev) as curr -> f (LogEntry.foldr f m prev) curr
    | JointConsensus(_,_,_,_,None)      as curr -> f m curr
    | LogEntry(_,_,_,_,Some prev)       as curr -> f (LogEntry.foldr f m prev) curr
    | LogEntry(_,_,_,_,None)            as curr -> f m curr
    | Snapshot _                        as curr -> f m curr

  ///  _ _
  /// (_) |_ ___ _ __
  /// | | __/ _ \ '__|
  /// | | ||  __/ |
  /// |_|\__\___|_|
  ///
  /// Iterate over a log from the newest entry to the oldest.
  static member iter (f : uint32 -> LogEntry -> unit) (log : LogEntry) =
    let rec _iter  _start _log =
      match _log with
        | Configuration(_,_,_,_,Some prev)  as curr -> f _start curr; _iter (_start + 1u) prev
        | Configuration(_,_,_,_,None)       as curr -> f _start curr
        | JointConsensus(_,_,_,_,Some prev) as curr -> f _start curr; _iter (_start + 1u) prev
        | JointConsensus(_,_,_,_,None)      as curr -> f _start curr
        | LogEntry(_,_,_,_,Some prev)       as curr -> f _start curr; _iter (_start + 1u) prev
        | LogEntry(_,_,_,_,None)            as curr -> f _start curr
        | Snapshot _                        as curr -> f _start curr
    _iter 0u log


  ///     _                                    _
  ///    / \   __ _  __ _ _ __ ___  __ _  __ _| |_ ___
  ///   / _ \ / _` |/ _` | '__/ _ \/ _` |/ _` | __/ _ \
  ///  / ___ \ (_| | (_| | | |  __/ (_| | (_| | ||  __/
  /// /_/   \_\__, |\__, |_|  \___|\__, |\__,_|\__\___|
  ///         |___/ |___/          |___/
  ///
  /// Version of left-fold that implements short-circuiting by requiring the
  /// return value to be wrapped in `Continue<'a>`.

  static member inline aggregate< ^m > (f : ^m -> LogEntry -> Continue< ^m >) (m : ^m) log =
    // wrap the supplied function such that it takes a value lifted to
    // Continue to proactively stop calculating (what about passing a
    // closure instead?)
    let _folder (m : Continue< ^m >) _log =
      match m with
        | Cont v -> f v _log
        |      v -> v

    // short-circuiting inner function
    let rec _resFold (m : Continue< ^m >) (_log : LogEntry) : Continue< ^m > =
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

  static member inline next< ^m > (m: ^m) : Continue< ^m > =
    Continue.next m

  static member inline finish< ^m > (m: ^m) : Continue< ^m > =
    Continue.finish m

  ///  _           _
  /// | | __ _ ___| |_
  /// | |/ _` / __| __|
  /// | | (_| \__ \ |_
  /// |_|\__,_|___/\__|
  ///
  /// Return the last (oldest) element of a log.

  static member last = function
    | LogEntry(_,_,_,_,None)          as curr -> curr
    | LogEntry(_,_,_,_,Some prev)             -> LogEntry.last prev
    | Configuration(_,_,_,_,None)     as curr -> curr
    | Configuration(_,_,_,_,Some prev)        -> LogEntry.last prev
    | JointConsensus(_,_,_,_,None)    as curr -> curr
    | JointConsensus(_,_,_,_,Some prev)       -> LogEntry.last prev
    | Snapshot _                      as curr -> curr

  //  _                    _
  // | |__   ___  __ _  __| |
  // | '_ \ / _ \/ _` |/ _` |
  // | | | |  __/ (_| | (_| |
  // |_| |_|\___|\__,_|\__,_|

  static member head = function
    | LogEntry(id,idx,term,data,Some _) ->
      LogEntry(id,idx,term,data,None)

    | Configuration(id,idx,term,nodes,Some _) ->
      Configuration(id,idx,term,nodes,None)

    | JointConsensus(id,idx,term,changes,Some _) ->
      JointConsensus(id,idx,term,changes,None)

    | curr -> curr

  //                         _ _
  //  _ __ _____      ___ __(_) |_ ___
  // | '__/ _ \ \ /\ / / '__| | __/ _ \
  // | | |  __/\ V  V /| |  | | ||  __/
  // |_|  \___| \_/\_/ |_|  |_|\__\___|

  static member rewrite entry =
    match entry with
    | Configuration(id, _, _, nodes, None) ->
      Configuration(id, 1u, 1u, nodes, None)

    | Configuration(id, _, term, nodes, Some prev) ->
      let previous = LogEntry.rewrite prev
      Configuration(id, LogEntry.index previous + 1u, term, nodes, Some previous)

    | JointConsensus(id, _, term, changes, None) ->
      JointConsensus(id, 1u, term, changes, None)

    | JointConsensus(id, _, term, changes, Some prev) ->
      let previous = LogEntry.rewrite prev
      JointConsensus(id, LogEntry.index previous + 1u, term, changes, Some previous)

    | LogEntry(id, _, term, data, None) ->
      LogEntry(id, 1u, term, data, None)

    | LogEntry(id, _, term, data, Some prev) ->
      let previous = LogEntry.rewrite prev
      LogEntry(id, LogEntry.index previous + 1u, term, data, Some previous)

    | Snapshot(id, _, term, _, pterm, nodes, data) ->
      Snapshot(id, 2u, term, 1u, pterm, nodes, data)

  ///                                   _
  ///   __ _ _ __  _ __   ___ _ __   __| |
  ///  / _` | '_ \| '_ \ / _ \ '_ \ / _` |
  /// | (_| | |_) | |_) |  __/ | | | (_| |
  ///  \__,_| .__/| .__/ \___|_| |_|\__,_|
  ///       |_|   |_|
  ///
  /// Append newer entries to older entries

  static member append (newer : LogEntry) (older : LogEntry) =
    let _aggregator (_log : LogEntry) (_entry : LogEntry) =
      if LogEntry.getId _log = LogEntry.getId _entry
      then _log
      else
        let nextIdx = LogEntry.index _log + 1u
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
    let last = LogEntry.last newer
    let lcd = LogEntry.find (LogEntry.getId last) older

    match lcd with
      | Some ancestor ->
        match LogEntry.pop ancestor with
          | Some entry ->
            // there is a least common denominator entry
            LogEntry.foldr _aggregator entry newer
          | _ -> newer
      | None ->
        // no overlap found
        LogEntry.foldr _aggregator older newer

  //  _           _   ___           _
  // | | __ _ ___| |_|_ _|_ __   __| | _____  __
  // | |/ _` / __| __|| || '_ \ / _` |/ _ \ \/ /
  // | | (_| \__ \ |_ | || | | | (_| |  __/>  <
  // |_|\__,_|___/\__|___|_| |_|\__,_|\___/_/\_\

  static member lastIndex = function
    | Snapshot(_,_,_,idx,_,_,_) -> Some idx
    | _                         -> None

  //  _           _  _____
  // | | __ _ ___| ||_   _|__ _ __ _ __ ___
  // | |/ _` / __| __|| |/ _ \ '__| '_ ` _ \
  // | | (_| \__ \ |_ | |  __/ |  | | | | | |
  // |_|\__,_|___/\__||_|\___|_|  |_| |_| |_|

  static member lastTerm = function
    | Snapshot(_,_,_,_,term,_,_) -> Some term
    | _                          -> None

  //   __ _          _   ___           _
  //  / _(_)_ __ ___| |_|_ _|_ __   __| | _____  __
  // | |_| | '__/ __| __|| || '_ \ / _` |/ _ \ \/ /
  // |  _| | |  \__ \ |_ | || | | | (_| |  __/>  <
  // |_| |_|_|  |___/\__|___|_| |_|\__,_|\___/_/\_\

  static member firstIndex (t: Term) (entry: LogEntry) =
    let getIdx idx term prev =
      match prev with
      | Some log ->
        if term = t then
          match LogEntry.firstIndex t log with
          | Some _ as result -> result
          | _                -> Some idx
        elif term > t then
          match LogEntry.firstIndex t log with
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

  //             _
  //   __ _  ___| |_ _ __
  //  / _` |/ _ \ __| '_ \
  // | (_| |  __/ |_| | | |
  //  \__, |\___|\__|_| |_|
  //  |___/
  static member getn count log =
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
        Configuration(id,idx,term,nodes, LogEntry.getn newcnt prev)
        |> Some

      | JointConsensus(id,idx,term,changes,Some prev) ->
        JointConsensus(id,idx,term,changes,LogEntry.getn newcnt prev)
        |> Some

      | LogEntry(id,idx,term,data, Some prev) ->
        LogEntry(id,idx,term,data, LogEntry.getn newcnt prev)
        |> Some

  //                  _        _
  //   ___ ___  _ __ | |_ __ _(_)_ __  ___
  //  / __/ _ \| '_ \| __/ _` | | '_ \/ __|
  // | (_| (_) | | | | || (_| | | | | \__ \
  //  \___\___/|_| |_|\__\__,_|_|_| |_|___/

  static member contains (f: LogEntry -> bool) = function
    | LogEntry(_,_,_,_,Some prev) as this ->
      if f this then true else LogEntry.contains f prev
    | LogEntry(_,_,_,_,None) as this -> f this

    | Configuration(_,_,_,_,Some prev) as this ->
      if f this then true else LogEntry.contains f prev
    | Configuration(_,_,_,_,None) as this -> f this

    | JointConsensus(_,_,_,_,Some prev) as this ->
      if f this then true else LogEntry.contains f prev
    | JointConsensus(_,_,_,_,None) as this -> f this

    | Snapshot _ as this -> f this

  //  ____              _ _   _
  // / ___|  __ _ _ __ (_) |_(_)_______
  // \___ \ / _` | '_ \| | __| |_  / _ \
  //  ___) | (_| | | | | | |_| |/ /  __/
  // |____/ \__,_|_| |_|_|\__|_/___\___|

  /// Make sure the current log entry is a singleton (followed by no entries).
  static member sanitize term = function
    | Configuration(id,_,term,nodes,_)    -> Configuration(id,0u,term,nodes,None)
    | JointConsensus(id,_,term,changes,_) -> JointConsensus(id,0u,term,changes,None)
    | LogEntry(id,_,_,data,_)             -> LogEntry(id,0u,term,data,None)
    | Snapshot _ as snapshot              -> snapshot

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

    let toOffset (log: LogEntry) =
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

    let arr = Array.zeroCreate (LogEntry.depth self |> int)
    LogEntry.iter (fun i (log: LogEntry) -> arr.[int i] <- toOffset log) self
    arr

  /// ## Decode a FlatBuffer into a Log structure
  ///
  /// Decodes a single FlatBuffer encoded log entry into its
  /// corresponding Raft LogEntry type and adds passed-in `LogEntry
  /// option` as previous field value. Indicates failure by returning
  /// None.
  ///
  /// ### Signature:
  /// - fb: LogFB FlatBuffer object to parse
  /// - log: previous LogEntry value to reconstruct the chain of events
  ///
  /// Returns: LogEntry option
  static member FromFB (logs: LogFB array) : LogEntry option =
    let fb2Log (fb: LogFB) (sibling: LogEntry option) : LogEntry option =
      match fb.EntryType with
      | LogTypeFB.ConfigurationFB ->
        let entry = fb.Entry<ConfigurationFB>()
        if entry.HasValue then
          let logentry = entry.Value
          let nodes = Array.zeroCreate logentry.NodesLength

          for i in 0 .. (logentry.NodesLength - 1) do
            let node = logentry.Nodes(i)
            if node.HasValue then
              node.Value
              |> RaftNode.FromFB
              |> Option.map (fun node -> nodes.[i] <- node)
              |> ignore

          Configuration(Id logentry.Id,
                        logentry.Index,
                        logentry.Term,
                        nodes,
                        sibling)
          |> Some
        else None

      | LogTypeFB.JointConsensusFB ->
        let entry = fb.Entry<JointConsensusFB>()
        if entry.HasValue then
          let logentry = entry.Value
          let changes = Array.zeroCreate logentry.ChangesLength

          for i in 0 .. (logentry.ChangesLength - 1) do
            let change = logentry.Changes(i)
            if change.HasValue then
              change.Value
              |> ConfigChange.FromFB
              |> Option.map (fun change -> changes.[i] <- change)
              |> ignore

          JointConsensus(Id logentry.Id,
                         logentry.Index,
                         logentry.Term,
                         changes,
                         sibling)
          |> Some
        else None

      | LogTypeFB.LogEntryFB ->
        let entry = fb.Entry<LogEntryFB>()
        if entry.HasValue then
          let logentry = entry.Value
          let data = logentry.Data
          if data.HasValue then
            StateMachine.FromFB data.Value
            |> Option.map
              (fun sm -> LogEntry(Id logentry.Id,
                               logentry.Index,
                               logentry.Term,
                               sm,
                               sibling))
          else None
        else None

      | LogTypeFB.SnapshotFB ->
        let entry = fb.Entry<SnapshotFB>()
        if entry.HasValue then
          let logentry = entry.Value
          let data = logentry.Data

          if data.HasValue then
            let nodes = Array.zeroCreate logentry.NodesLength
            let id = Id logentry.Id

            for i in 0..(logentry.NodesLength - 1) do
              let node = logentry.Nodes(i)
              if node.HasValue then
                node.Value
                |> RaftNode.FromFB
                |> Option.map (fun node -> nodes.[i] <- node)
                |> ignore

            StateMachine.FromFB data.Value
            |> Option.map
              (fun sm -> Snapshot(id,
                               logentry.Index,
                               logentry.Term,
                               logentry.LastIndex,
                               logentry.LastTerm,
                               nodes,
                               sm))
          else None
        else None

      | _ -> None

    Array.foldBack fb2Log logs None

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  /// ## Convert the log entry to Yaml
  ///
  /// Convert the LogEntry into a Yaml-serializable POCO.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: LogYaml
  member self.ToYamlObject () =
    match self with
    | Configuration(id, idx, term, nodes, Some prev) ->
      let lid = string id
      let previd = LogEntry.getId prev |> string
      let nids = Array.map (Node.getId >> string) nodes
      LogYaml.Configuration(lid, idx, term, nids, previd)

    | Configuration(id, idx, term, nodes, None) ->
      let lid = string id
      let previd = null
      let nids = Array.map (Node.getId >> string) nodes
      LogYaml.Configuration(lid, idx, term, nids, previd)

    | JointConsensus(id, idx, term, changes, Some prev) ->
      let lid = string id
      let previd = LogEntry.getId prev |> string
      let ymls = Array.map Yaml.toYaml changes
      LogYaml.JointConsensus(lid, idx, term, ymls, previd)

    | JointConsensus(id, idx, term, changes, None) ->
      let lid = string id
      let previd = null
      let ymls = Array.map Yaml.toYaml changes
      LogYaml.JointConsensus(lid, idx, term, ymls, previd)

    | LogEntry(id, idx, term, smentry, Some prev) ->
      let lid = string id
      let previd = LogEntry.getId prev |> string
      LogYaml.LogEntry(lid, idx, term, Yaml.toYaml smentry, previd)

    | LogEntry(id, idx, term, smentry, None) ->
      let lid = string id
      let previd = null
      LogYaml.LogEntry(lid, idx, term, Yaml.toYaml smentry, previd)

    | Snapshot(id, idx, term, lidx, lterm, nodes, smentry) ->
      let lid = string id
      let nids = Array.map (Node.getId >> string) nodes
      LogYaml.Snapshot(id, idx, term, lidx, lterm, nids, Yaml.toYaml smentry)
