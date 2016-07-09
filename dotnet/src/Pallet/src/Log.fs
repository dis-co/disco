namespace Pallet.Core

open System
open System.Collections

//  _                 ___         _      __
// | |    ___   __ _ / ( ) __ _  ( )_ __ \ \
// | |   / _ \ / _` / /|/ / _` | |/| '_ \ \ \
// | |__| (_) | (_| \ \  | (_| |_  | | | |/ /
// |_____\___/ \__, |\_\  \__,_( ) |_| |_/_/
//             |___/           |/
//
/// Linked-list like type for fast access to most recent elements and their
/// properties.
///
/// type LogEntry<'a,'n> =
///   // Node Configuration Entry
///   | Configuration of
///     Id       : Id              *        // unique id of configuration entry
///     Index    : Index           *        // index in log
///     Term     : Term            *        // term when entry was added to log
///     Nodes    : Node<'n> array  *        // new node configuration
///     Previous : LogEntry<'a,'n> option   // previous log entry, if applicable
///
///   // Entry type for configuration changes
///   | JointConsensus of
///     Id       : Id                     * // unique identified of entry
///     Index    : Index                  * // index of entry in log
///     Term     : Term                   * // term in which entry was added to the log
///     Changes  : ConfigChange<'n> array * // changes to be applied to node configuration
///     Nodes    : Node<'n> array         * // old configuration the changes will be applied against
///     Previous : LogEntry<'a,'n> option   // previous element, if any
///
///   // Regular Log Entries
///   | LogEntry   of
///     Id       : Id              *        // unique identifier of entry
///     Index    : Index           *        // index of entry in log
///     Term     : Term            *        // temr in which entry was added to log
///     Data     : 'a              *        // state machine data field
///     Previous : LogEntry<'a,'n> option   // previous element, if any
///
///   | Snapshot   of
///     Id        : Id             *        // unique identifier of entry
///     Index     : Index          *        // index of entry in log
///     Term      : Term           *        // term the entry was added in to log
///     LastIndex : Index          *        // last included index
///     LastTerm  : Term           *        // last included term
///     Nodes     : Node<'n> array *        // node configuration
///     Data      : 'a                      // state machine data
///
type LogEntry<'a,'n> =
  // Node Configuration Entry
  | Configuration of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Nodes    : Node<'n> array  *
    Previous : LogEntry<'a,'n> option

  // Entry type for configuration changes
  | JointConsensus of
    Id       : Id                     *
    Index    : Index                  *
    Term     : Term                   *
    Changes  : ConfigChange<'n> array *
    Previous : LogEntry<'a,'n> option

  // Regular Log Entries
  | LogEntry   of
    Id       : Id              *
    Index    : Index           *
    Term     : Term            *
    Data     : 'a              *
    Previous : LogEntry<'a,'n> option

  | Snapshot   of
    Id        : Id             *
    Index     : Index          *
    Term      : Term           *
    LastIndex : Index          *
    LastTerm  : Term           *
    Nodes     : Node<'n> array *
    Data      : 'a

  override self.ToString() =
    match self with
      | Configuration(id,idx,term,nodes,_) ->
        sprintf "Configuration [id: %A] [idx: %A] [term: %A]\nnodes: %s"
          id
          idx
          term
          (Array.fold (fun m n -> m + "\n    " + n.ToString()) "" nodes)

      | JointConsensus(id,idx,term,changes,_) ->
        sprintf "UpdateNode [id: %A] [idx: %A] [term: %A]\nchanges: %s"
          id
          idx
          term
          (Array.fold (fun m n -> m + (sprintf "\n    %A" n)) "" changes)

      | LogEntry(id,idx,term,data,_) ->
        sprintf "LogEntry [id: %A] [idx: %A] [term: %A] [data: %A]"
          id
          idx
          term
          data

      | Snapshot(id,idx,term,lidx,ltrm,_,_) ->
        sprintf "Snapshot [id: %A] [idx: %A] [last idx: %A] [term: %A] [last term: %A]"
          id
          idx
          lidx
          term
          ltrm

type Log<'a,'n> =
  { Data  : LogEntry<'a,'n> option
  ; Depth : Long
  ; Index : Index
  }

[<RequireQualifiedAccess>]
module private LogEntry =

  //   _     _
  //  (_) __| |
  //  | |/ _` |
  //  | | (_| |
  //  |_|\__,_|
  //
  /// Return the current log entry id.

  let id = function
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

  let isConfigChange = function
    | JointConsensus _ -> true
    |                _ -> false

  //  _      ____             __ _                       _   _
  // (_)___ / ___|___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
  // | / __| |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
  // | \__ \ |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
  // |_|___/\____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
  //                               |___/

  let isConfiguration = function
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

  let depth log =
    let rec _depth i thing =
      let inline count i prev =
        let cnt = i + 1UL
        match prev with
          | Some other -> _depth cnt other
          |          _ -> cnt
      match thing with
        | Configuration(_,_,_,_,prev)  -> count i prev
        | JointConsensus(_,_,_,_,prev) -> count i prev
        | LogEntry(_,_,_,_,prev)       -> count i prev
        | Snapshot _                   -> i + 1UL
    _depth 0UL log

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

  //   _
  //  | |_ ___ _ __ _ __ ___
  //  | __/ _ \ '__| '_ ` _ \
  //  | ||  __/ |  | | | | | |
  //   \__\___|_|  |_| |_| |_|
  //
  /// Extract the `Term` field from a LogEntry

  let term = function
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

  let prevTerm = function
    | Configuration(_,_,_,_,Some prev)  -> Some (term prev)
    | JointConsensus(_,_,_,_,Some prev) -> Some (term prev)
    | LogEntry(_,_,_,_,Some prev)       -> Some (term prev)
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

  let prevEntry = function
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

  let data = function
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

  let nodes = function
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

  let changes = function
    | JointConsensus(_,_,_,c,_) -> Some c
    | _                         -> None

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
        | _ when idx = index -> Configuration(id,index,term,nodes,None)           |> Some
        | _ when idx < index -> Configuration(id,index,term,nodes,until idx prev) |> Some
        | _                  -> None

    | JointConsensus(_,index,_,_,None) as curr ->
      match idx with
       | _ when idx = index -> Some curr
       | _                  -> None

    | JointConsensus(id,index,term,changes,Some prev) ->
      match idx with
        | _ when idx = index ->
          JointConsensus(id,index,term,changes,None) |> Some
        | _ when idx < index ->
          JointConsensus(id,index,term,changes,until idx prev) |> Some
        | _                  -> None

    | LogEntry(_,index,_,_,None) as curr ->
      match idx with
        | _ when idx = index -> Some curr
        | _                  -> None

    | LogEntry(id,index,term,data,Some prev) ->
      match idx with
        | _ when idx = index -> LogEntry(id,index,term,data,None)           |> Some
        | _ when idx < index -> LogEntry(id,index,term,data,until idx prev) |> Some
        | _ -> None

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
      if idx >= index then None
      else  Configuration(id,index,term,nodes,untilExcluding idx prev) |> Some

    | JointConsensus(id,index,term,changes,Some prev) ->
      if idx >= index then None
      else JointConsensus(id,index,term,changes,untilExcluding idx prev) |> Some

    | LogEntry(id,index,term,data,Some prev) ->
      if idx >= index then None
      else LogEntry(id,index,term,data,untilExcluding idx prev) |> Some

    | _ -> None

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
          | _ -> None
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

  let make term data =
    LogEntry(RaftId.Create(), 0UL, term, data, None)


  /// Add an Configuration log entry onto the queue
  ///
  /// ### Complexity: 0(1)

  let mkConfig term nodes =
    Configuration(RaftId.Create(), 0UL, term, nodes, None)

  /// Add an intermediate configuration entry for 2-phase commit onto the log queue
  ///
  /// ### Complexity: 0(1)

  let mkConfigChange term oldnodes newnodes =
    let changes =
      let additions =
        Array.fold
          (fun lst newnode ->
            match Array.tryFind (Node.getId >> (=) newnode.Id) oldnodes with
              | Some _ -> lst
              |      _ -> NodeAdded(newnode) :: lst) [] newnodes

      Array.fold
        (fun lst oldnode ->
          match Array.tryFind (Node.getId >> (=) oldnode.Id) newnodes with
            | Some _ -> lst
            | _ -> NodeRemoved(oldnode) :: lst) additions oldnodes
      |> List.toArray

    JointConsensus(RaftId.Create(), 0UL, term, changes, None)

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

  ///                            _           _
  ///  ___ _ __   __ _ _ __  ___| |__   ___ | |_
  /// / __| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  /// \__ \ | | | (_| | |_) \__ \ | | | (_) | |_
  /// |___/_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  ///                 |_|
  ///
  /// Compact the log database

  let snapshot nodes data = function
    | LogEntry(_,idx,term,_,_)       -> Snapshot(RaftId.Create(),idx + 1UL,term,idx,term,nodes,data)
    | Configuration(_,idx,term,_,_)  -> Snapshot(RaftId.Create(),idx + 1UL,term,idx,term,nodes,data)
    | JointConsensus(_,idx,term,_,_) -> Snapshot(RaftId.Create(),idx + 1UL,term,idx,term,nodes,data)
    | Snapshot(_,idx,term,_,_,_,_)   -> Snapshot(RaftId.Create(),idx + 1UL,term,idx,term,nodes,data)

  ///  _ __ ___   __ _ _ __
  /// | '_ ` _ \ / _` | '_ \
  /// | | | | | | (_| | |_) |
  /// |_| |_| |_|\__,_| .__/
  ///                 |_|
  ///
  /// Map over a Logs<'a,'n> and return a list of results

  let rec map (f : LogEntry<_,_> -> 'b) entry =
    let _map curr prev =
      match prev with
        | Some previous -> f curr :: map f previous
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
  /// Fold over a Log<'a,'n> and return an aggregate value

  let rec foldl (f : 'm -> LogEntry<'a,'n> -> 'm) (m : 'm) log =
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

  ///   __       _     _
  ///  / _| ___ | | __| |_ __
  /// | |_ / _ \| |/ _` | '__|
  /// |  _| (_) | | (_| | |
  /// |_|  \___/|_|\__,_|_|
  ///
  /// Fold over a Log<'a,'n> and return an aggregate value

  let rec foldr (f : 'm -> LogEntry<'a,'n> -> 'm) (m : 'm)  = function
    | Configuration(_,_,_,_,Some prev)  as curr -> f (foldr f m prev) curr
    | Configuration(_,_,_,_,None)       as curr -> f m curr
    | JointConsensus(_,_,_,_,Some prev) as curr -> f (foldr f m prev) curr
    | JointConsensus(_,_,_,_,None)      as curr -> f m curr
    | LogEntry(_,_,_,_,Some prev)       as curr -> f (foldr f m prev) curr
    | LogEntry(_,_,_,_,None)            as curr -> f m curr
    | Snapshot _                        as curr -> f m curr

  ///  _ _
  /// (_) |_ ___ _ __
  /// | | __/ _ \ '__|
  /// | | ||  __/ |
  /// |_|\__\___|_|
  ///
  /// Iterate over a log from the newest entry to the oldest.
  let iter (f : uint32 -> LogEntry<'a,'n> -> unit) (log : LogEntry<'a,'n>) =
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

  let aggregate (f : 'm -> LogEntry<'a,'n> -> Continue<'m>) (m : 'm) log =
    // wrap the supplied function such that it takes a value lifted to
    // Continue to proactively stop calculating (what about passing a
    // closure instead?)
    let _folder (m : Continue<'m>) _log =
      match m with
        | Cont v -> f v _log
        |      v -> v

    // short-circuiting inner function
    let rec _resFold (m : Continue<'m>) (_log : LogEntry<'a,'n>) : Continue<'m> =
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

  let next = Continue.next
  let finish = Continue.finish

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

  //                         _ _
  //  _ __ _____      ___ __(_) |_ ___
  // | '__/ _ \ \ /\ / / '__| | __/ _ \
  // | | |  __/\ V  V /| |  | | ||  __/
  // |_|  \___| \_/\_/ |_|  |_|\__\___|

  let rec rewrite entry =
    match entry with
      | Configuration(id, _, _, nodes, None) ->
        Configuration(id, 1UL, 1UL, nodes, None)

      | Configuration(id, _, term, nodes, Some prev) ->
        let previous = rewrite prev
        Configuration(id, index previous + 1UL, term, nodes, Some previous)

      | JointConsensus(id, _, term, changes, None) ->
        JointConsensus(id, 1UL, term, changes, None)

      | JointConsensus(id, _, term, changes, Some prev) ->
        let previous = rewrite prev
        JointConsensus(id, index previous + 1UL, term, changes, Some previous)

      | LogEntry(id, _, term, data, None) ->
        LogEntry(id, 1UL, term, data, None)

      | LogEntry(id, _, term, data, Some prev) ->
        let previous = rewrite prev
        LogEntry(id, index previous + 1UL, term, data, Some previous)

      | Snapshot(id, _, term, _, pterm, nodes, data) ->
        Snapshot(id, 2UL, term, 1UL, pterm, nodes, data)

  ///                                   _
  ///   __ _ _ __  _ __   ___ _ __   __| |
  ///  / _` | '_ \| '_ \ / _ \ '_ \ / _` |
  /// | (_| | |_) | |_) |  __/ | | | (_| |
  ///  \__,_| .__/| .__/ \___|_| |_|\__,_|
  ///       |_|   |_|
  ///
  /// Append newer entries to older entries

  let append (newer : LogEntry<'a,'n>) (older : LogEntry<'a,'n>) =
    let _aggregator (_log : LogEntry<'a,'n>) (_entry : LogEntry<'a,'n>) =
      if id _log = id _entry
      then _log
      else
        let nextIdx = index _log + 1UL
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
    let lcd = find (id last) older

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

  //  _           _   ___           _
  // | | __ _ ___| |_|_ _|_ __   __| | _____  __
  // | |/ _` / __| __|| || '_ \ / _` |/ _ \ \/ /
  // | | (_| \__ \ |_ | || | | | (_| |  __/>  <
  // |_|\__,_|___/\__|___|_| |_|\__,_|\___/_/\_\

  let lastIndex = function
    | Snapshot(_,_,_,idx,_,_,_) -> Some idx
    | _                         -> None

  //  _           _  _____
  // | | __ _ ___| ||_   _|__ _ __ _ __ ___
  // | |/ _` / __| __|| |/ _ \ '__| '_ ` _ \
  // | | (_| \__ \ |_ | |  __/ |  | | | | | |
  // |_|\__,_|___/\__||_|\___|_|  |_| |_| |_|

  let lastTerm = function
    | Snapshot(_,_,_,_,term,_,_) -> Some term
    | _                          -> None

  //   __ _          _   ___           _
  //  / _(_)_ __ ___| |_|_ _|_ __   __| | _____  __
  // | |_| | '__/ __| __|| || '_ \ / _` |/ _ \ \/ /
  // |  _| | |  \__ \ |_ | || | | | (_| |  __/>  <
  // |_| |_|_|  |___/\__|___|_| |_|\__,_|\___/_/\_\

  let rec firstIndex (t: Term) (entry: LogEntry<_,_>) =
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

  //             _
  //   __ _  ___| |_ _ __
  //  / _` |/ _ \ __| '_ \
  // | (_| |  __/ |_| | | |
  //  \__, |\___|\__|_| |_|
  //  |___/
  let rec getn count log =
    if count = 0UL then
      None
    else
      let newcnt = count - 1UL
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


  //                  _        _
  //   ___ ___  _ __ | |_ __ _(_)_ __  ___
  //  / __/ _ \| '_ \| __/ _` | | '_ \/ __|
  // | (_| (_) | | | | || (_| | | | | \__ \
  //  \___\___/|_| |_|\__\__,_|_|_| |_|___/

  let rec contains (f: LogEntry<_,_> -> bool) = function
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

[<RequireQualifiedAccess>]
module Log =
  /// Construct an empty Log
  let empty =
    { Depth = 0UL
    ; Index = 0UL
    ; Data  = None
    }

  let fromEntries (entries: LogEntry<_,_>) =
    { Depth = LogEntry.depth entries
    ; Index = LogEntry.index entries
    ; Data  = Some entries
    }

  /// compute the actual depth of the log (e.g. for compacting)
  let length log = log.Depth

  /// Return the the current Index in the log
  let index log = log.Index

  /// Return the index of the previous element
  let prevIndex log =
    match log.Data with
      | Some entries -> LogEntry.prevIndex entries
      | _            -> None

  /// Return the Term of the latest log entry
  ///
  /// ### Complexity: 0(1)
  let term log =
    match log.Data with
      | Some entries -> LogEntry.term entries
      | _            -> 0UL

  /// Return the Term of the previous entry
  ///
  /// ### Complexity: 0(1)
  let prevTerm log =
    match log.Data with
      | Some entries -> LogEntry.prevTerm entries
      | _            -> None

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
    match log.Data with
      | Some entries -> LogEntry.prevEntry entries
      | _            -> None

  let foldl f m log = LogEntry.foldl f m log
  let foldr f m log = LogEntry.foldr f m log

  let foldLogL f m log =
    match log.Data with
      | Some entries -> LogEntry.foldl f m entries
      | _            -> m

  let foldLogR f m log =
    match log.Data with
      | Some entries -> LogEntry.foldr f m entries
      | _            -> m

  let at idx log =
    match log.Data with
      | Some entries -> LogEntry.at idx entries
      | _            -> None

  let until idx log =
    match log.Data with
      | Some entries -> LogEntry.until idx entries
      | _            -> None

  let untilExcluding idx log =
    match log.Data with
      | Some entries -> LogEntry.untilExcluding idx entries
      | _            -> None

  let append newentries log : Log<_,_> =
    match log.Data with
      | Some entries ->
        let newlog = LogEntry.append newentries entries
        { Index = LogEntry.index newlog
          Depth = LogEntry.depth newlog
          Data  = Some           newlog }
      | _ ->
        let entries' = LogEntry.rewrite newentries
        { Index = LogEntry.index entries'
          Depth = LogEntry.depth entries'
          Data  = Some           entries' }

  let find id log =
    match log.Data with
      | Some entries -> LogEntry.find id entries
      | _ -> None

  let size log = log.Depth

  let depth entries = LogEntry.depth entries

  let make term data = LogEntry.make term data
  let mkConfig term nodes = LogEntry.mkConfig term nodes
  let mkConfigChange term old newer = LogEntry.mkConfigChange term old newer

  let id log = LogEntry.id log

  let data log = LogEntry.data log

  let entries log = log.Data

  let aggregate f m log = LogEntry.aggregate f m log

  let snapshot nodes data log =
    match log.Data with
      | Some entries ->
        let snapshot = LogEntry.snapshot nodes data entries
        { Index = LogEntry.index snapshot
          Depth = 1UL
          Data = Some snapshot }
      | _ -> log

  let entryTerm = LogEntry.term
  let entryIndex = LogEntry.index
  let next = LogEntry.next
  let finish = LogEntry.finish
  let map = LogEntry.map

  let head log =
    match log.Data with
      | Some entries -> Some (LogEntry.head entries)
      | _            -> None

  let lastTerm log =
    match log.Data with
      | Some data -> LogEntry.lastTerm data
      | _ -> None

  let lastIndex log =
    match log.Data with
      | Some data -> LogEntry.lastIndex data
      | _ -> None

  let last log = LogEntry.last log

  /// Return the last entry in the chain of logs.
  let lastEntry log =
    match log.Data with
      | Some  entries -> LogEntry.last entries |> Some
      | _             -> None

  /// Make sure the current log entry is a singleton (followed by no entries).
  let sanitize term = function
    | Configuration(id,_,term,nodes,_)    -> Configuration(id,0UL,term,nodes,None)
    | JointConsensus(id,_,term,changes,_) -> JointConsensus(id,0UL,term,changes,None)
    | LogEntry(id,_,_,data,_)             -> LogEntry(id,0UL,term,data,None)
    | Snapshot _ as snapshot              -> snapshot

  /// Iterate over log entries, in order of newsest to oldest.
  let iter f log = LogEntry.iter f log

  /// Retrieve the index of the first log entry for the given term. Return None
  /// if no result was found;
  let firstIndex term log =
    match log.Data with
      | Some log -> LogEntry.firstIndex term log
      | _        -> None

  let getn count log =
    match log.Data with
      | Some log -> LogEntry.getn count log
      | _        -> None

  let containsEntry (f: LogEntry<_,_> -> bool) log =
    LogEntry.contains f log

  let contains (f: LogEntry<_,_> -> bool) log =
    match log.Data with
      | Some entries -> LogEntry.contains f entries
      | _ -> false

  let isConfiguration log = LogEntry.isConfiguration log
