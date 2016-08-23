module Iris.Service.Raft.Db

// -------------------------------------------------------------------------------------------- //
//                                        ____  ____                                            //
//                                       |  _ \| __ )                                           //
//                                       | | | |  _ \                                           //
//                                       | |_| | |_) |                                          //
//                                       |____/|____/                                           //
// -------------------------------------------------------------------------------------------- //

open System
open LiteDB
open Iris.Core
open Iris.Raft
open Nessos.FsPickler
open FSharpx.Functional

[<Literal>]
let DB_NAME = "iris.raft.db"

[<Literal>]
let METADATA_ID = "iris.raft.metadata"

//  _                  ____        _
// | |    ___   __ _  |  _ \  __ _| |_ __ _
// | |   / _ \ / _` | | | | |/ _` | __/ _` |
// | |__| (_) | (_| | | |_| | (_| | || (_| |
// |_____\___/ \__, | |____/ \__,_|\__\__,_|
//             |___/

[<RequireQualifiedAccess>]
type LogDataType =
  | Config    = 0
  | Consensus = 1
  | Entry     = 2
  | Snapshot  = 3

[<AllowNullLiteral>]
type LogData() =
  let mutable log_type  : int        = int LogDataType.Entry
  let mutable id        : string     = null
  let mutable idx       : int64      = 0L
  let mutable term      : int64      = 0L
  let mutable last_idx  : int64      = 0L
  let mutable last_term : int64      = 0L
  let mutable nodes     : byte array = null
  let mutable changes   : byte array = null
  let mutable data      : byte array = null
  let mutable prev      : string     = null

  member __._id
    with get () = id
    and  set v  = id <- v

  member __.LogType
    with get () = log_type
    and  set v  = log_type <- v

  member __.Index
    with get () = idx
    and  set v  = idx <- v

  member __.Term
    with get () = term
    and  set v  = term <- v

  member __.LastIndex
    with get () = last_idx
    and  set v  = last_idx <- v

  member __.LastTerm
    with get () = last_term
    and  set v  = last_term <- v

  member __.Nodes
    with get () = nodes
    and  set v  = nodes <- v

  member __.Changes
    with get () = changes
    and  set v  = changes <- v

  member __.Data
    with get () = data
    and  set v  = data <- v

  member __.Previous
    with get () = prev
    and  set v  = prev <- v

  //  ____
  // |  _ \ __ _ _ __ ___  ___
  // | |_) / _` | '__/ __|/ _ \
  // |  __/ (_| | |  \__ \  __/
  // |_|   \__,_|_|  |___/\___|

  member self.ToLog () : LogEntry =
    let coder = FsPickler.CreateBinarySerializer()

    let id   = Id self._id
    let idx  = uint64 self.Index
    let term = uint64 self.Term

    match self.LogType with
      | t when t = int LogDataType.Config ->
        let nodes : Node array = coder.UnPickle(self.Nodes)
        Configuration(id,idx,term,nodes,None)

      | t when t = int LogDataType.Consensus ->
        let changes : ConfigChange array = coder.UnPickle(self.Changes)
        JointConsensus(id,idx,term,changes,None)

      | t when t = int LogDataType.Entry ->
        let data : StateMachine = coder.UnPickle(self.Data)
        LogEntry(id,idx,term,data,None)

      | t when t = int LogDataType.Snapshot ->
        let nodes : Node array = coder.UnPickle(self.Nodes)
        let data : StateMachine = coder.UnPickle(self.Data)
        let lidx = uint64 self.LastIndex
        let lterm = uint64 self.LastTerm
        Snapshot(id,idx,term,lidx,lterm,nodes,data)

      | _ -> failwithf "Could not parse log entry: [id: %A] [idx: %A]" id idx

  //  ____            _       _ _
  // / ___|  ___ _ __(_) __ _| (_)_______
  // \___ \ / _ \ '__| |/ _` | | |_  / _ \
  //  ___) |  __/ |  | | (_| | | |/ /  __/
  // |____/ \___|_|  |_|\__,_|_|_/___\___|

  static member FromLog (log: LogEntry) : LogData array =
    let toLogData (coder: BinarySerializer) (log: LogEntry) =
      let logdata = new LogData()
      match log with
        | Configuration(id,idx,term,nodes,None) ->
          logdata._id      <- string id
          logdata.LogType  <- int LogDataType.Config
          logdata.Index    <- int64 idx
          logdata.Term     <- int64 term
          logdata.Nodes    <- coder.Pickle(nodes)

        | Configuration(id,idx,term,nodes,Some prev) ->
          logdata._id      <- string id
          logdata.LogType  <- int LogDataType.Config
          logdata.Index    <- int64 idx
          logdata.Term     <- int64 term
          logdata.Nodes    <- coder.Pickle(nodes)
          logdata.Previous <- string <| Log.id prev

        | JointConsensus(id,idx,term,changes,None) ->
          logdata._id      <- string id
          logdata.LogType  <- int LogDataType.Consensus
          logdata.Index    <- int64 idx
          logdata.Term     <- int64 term
          logdata.Changes  <- coder.Pickle(changes)

        | JointConsensus(id,idx,term,changes,Some prev) ->
          logdata._id      <- string id
          logdata.LogType  <- int LogDataType.Consensus
          logdata.Index    <- int64 idx
          logdata.Term     <- int64 term
          logdata.Changes  <- coder.Pickle(changes)
          logdata.Previous <- string <| Log.id prev

        | LogEntry(id,idx,term,data,None) ->
          logdata._id      <- string id
          logdata.LogType  <- int LogDataType.Entry
          logdata.Index    <- int64 idx
          logdata.Term     <- int64 term
          logdata.Data     <- coder.Pickle(data)

        | LogEntry(id,idx,term,data,Some prev) ->
          logdata._id      <- string id
          logdata.LogType  <- int LogDataType.Entry
          logdata.Index    <- int64 idx
          logdata.Term     <- int64 term
          logdata.Data     <- coder.Pickle(data)
          logdata.Previous <- string <| Log.id prev

        | Snapshot(id,idx,term,lidx,lterm,nodes,data) ->
          logdata._id       <- string id
          logdata.LogType   <- int LogDataType.Snapshot
          logdata.Index     <- int64 idx
          logdata.Term      <- int64 term
          logdata.LastIndex <- int64 lidx
          logdata.LastTerm  <- int64 lterm
          logdata.Nodes     <- coder.Pickle(nodes)
          logdata.Data      <- coder.Pickle(data)
      logdata
    let coder = FsPickler.CreateBinarySerializer()
    Log.foldr (fun lst lg -> toLogData coder lg :: lst) [] log
    |> Array.ofList

  interface IComparable<LogData> with
    member self.CompareTo (other: LogData) =
      if self.Index < other.Index then
        -1
      elif self.Index = other.Index then
        0
      else
        1

  interface IComparable with
    member self.CompareTo obj =
      match obj with
      | :? LogData as other -> (self :> IComparable<_>).CompareTo other
      | _                   -> failwith "obj not a LogData"


  interface IEquatable<LogData> with
    member self.Equals (other: LogData) =
      self._id = other._id && self.Index = other.Index && self.Term = other.Term

  override self.Equals obj =
    match obj with
    | :? LogData as other -> (self :> IEquatable<_>).Equals other
    | _                   -> failwith "obj not a LogData"

  override self.GetHashCode () =
    hash (self._id, self.Index, self.Term)

//  _   _           _        __  __      _            _       _
// | \ | | ___   __| | ___  |  \/  | ___| |_ __ _  __| | __ _| |_ __ _
// |  \| |/ _ \ / _` |/ _ \ | |\/| |/ _ \ __/ _` |/ _` |/ _` | __/ _` |
// | |\  | (_) | (_| |  __/ | |  | |  __/ || (_| | (_| | (_| | || (_| |
// |_| \_|\___/ \__,_|\___| |_|  |_|\___|\__\__,_|\__,_|\__,_|\__\__,_|

[<AllowNullLiteral>]
type NodeMetaData() =
  let mutable id        : string     = null
  let mutable voting    : bool       = false
  let mutable voted     : bool       = false
  let mutable state     : string     = null
  let mutable data      : byte array = null
  let mutable next_idx  : int64      = 0L
  let mutable match_idx : int64      = 0L

  member __._id
    with get () = id
    and  set v  = id <- v

  member __.State
    with get () = state
    and  set v  = state <- v

  member __.Data
    with get () = data
    and  set v  = data <- v

  member __.Voting
    with get () = voting
    and  set v  = voting <- v

  member __.VotedForMe
    with get () = voted
    and  set v  = voted <- v

  member __.NextIndex
    with get () = next_idx
    and  set v  = next_idx <- v

  member __.MatchIndex
    with get () = match_idx
    and  set v  = match_idx <- v

  //  ____
  // |  _ \ __ _ _ __ ___  ___
  // | |_) / _` | '__/ __|/ _ \
  // |  __/ (_| | |  \__ \  __/
  // |_|   \__,_|_|  |___/\___|

  static member FromNode (node: Node) =
    let coder = FsPickler.CreateBinarySerializer()
    let meta = new NodeMetaData()
    meta._id        <- string node.Id
    meta.State      <- string node.State
    meta.Data       <- coder.Pickle(node.Data)
    meta.Voting     <- node.Voting
    meta.VotedForMe <- node.VotedForMe
    meta.NextIndex  <- int64 node.NextIndex
    meta.MatchIndex <- int64 node.MatchIndex
    meta

  //  ____            _       _ _
  // / ___|  ___ _ __(_) __ _| (_)_______
  // \___ \ / _ \ '__| |/ _` | | |_  / _ \
  //  ___) |  __/ |  | | (_| | | |/ /  __/
  // |____/ \___|_|  |_|\__,_|_|_/___\___|

  member self.ToNode () : Node =
    let coder = FsPickler.CreateBinarySerializer()
    { Id         = Id self._id
    ; Data       = coder.UnPickle<IrisNode>(self.Data)
    ; Voting     = self.Voting
    ; VotedForMe = self.VotedForMe
    ; State      = NodeState.Parse self.State
    ; NextIndex  = uint64 self.NextIndex
    ; MatchIndex = uint64 self.MatchIndex }

//  ____        __ _     __  __      _            _       _
// |  _ \ __ _ / _| |_  |  \/  | ___| |_ __ _  __| | __ _| |_ __ _
// | |_) / _` | |_| __| | |\/| |/ _ \ __/ _` |/ _` |/ _` | __/ _` |
// |  _ < (_| |  _| |_  | |  | |  __/ || (_| | (_| | (_| | || (_| |
// |_| \_\__,_|_|  \__| |_|  |_|\___|\__\__,_|\__,_|\__,_|\__\__,_|

[<AllowNullLiteral>]
type RaftMetaData() =
  let mutable raftid           : string       = null
  let mutable state            : string       = null
  let mutable term             : int64        = 0L
  let mutable leaderid         : string       = null
  let mutable oldpeers         : string array = null
  let mutable votedfor         : string       = null
  let mutable timeout_elapsed  : int64        = 0L
  let mutable election_timeout : int64        = 0L
  let mutable request_timeout  : int64        = 0L
  let mutable max_log_depth    : int64        = 0L
  let mutable commitidx        : int64        = 0L
  let mutable last_applied_idx : int64        = 0L
  let mutable config_change    : string       = null

  [<BsonId>]
  member __.Id
    with get () = METADATA_ID
     and set (_: string) = ()

  member __.NodeId
    with get () = raftid
     and set s  = raftid <- s

  member __.State
    with get () = state
     and set s  = state <- s

  member __.Term
    with get () = term
     and set t  = term <- t

  member __.LeaderId
    with get () = leaderid
     and set t  = leaderid <- t

  member __.OldPeers
    with get () = oldpeers
     and set t  = oldpeers <- t

  member __.VotedFor
    with get () = votedfor
     and set v  = votedfor <- v

  member __.CommitIndex
    with get () = commitidx
     and set v  = commitidx <- v

  member __.LastAppliedIndex
    with get () = last_applied_idx
     and set v  = last_applied_idx <- v

  member __.TimeoutElapsed
    with get () = timeout_elapsed
     and set t  = timeout_elapsed <- t

  member __.ElectionTimeout
    with get () = election_timeout
     and set t  = election_timeout <- t

  member __.RequestTimeout
    with get () = request_timeout
     and set t  = request_timeout <- t

  member __.MaxLogDepth
    with get () = max_log_depth
     and set t  = max_log_depth <- t

  member __.ConfigChangeEntry
    with get () = config_change
     and set t  = config_change <- t

  static member Guid = METADATA_ID

/// ## Open a LevelDB on disk
///
/// Open a LevelDB stored at given file path.
///
/// ### Signature:
/// - filepath: path to database file
///
/// Returns: LevelDB.DB
let openDB filepath =
  match IO.File.Exists filepath with
    | true -> new LiteDatabase(filepath) |> Some
    | _    ->
      match IO.File.Exists (filepath </> DB_NAME) with
        | true -> new LiteDatabase(filepath </> DB_NAME) |> Some
        | _    -> None

/// ## Close a database
///
/// Dispose of the passed database instance.
///
/// ### Signature:
/// - db: LiteDatabase to close
///
/// Returns: unit
let closeDB (db: LiteDatabase) =
  dispose db

/// ## Create a new database
///
/// Create a new database at the location specified.
///
/// ### Signature:
/// - path: FilePath to the Directory to contain database File
///
/// Returns: LiteDatabase option
let createDB (path: FilePath) =
  match openDB path with
    | Some _ as db -> db
    |      _       ->
      match IO.Directory.Exists path with
        | true -> new LiteDatabase(path </> DB_NAME) |> Some
        | _    ->
          try
            let info = IO.Directory.CreateDirectory path
            if info.Exists then
              new LiteDatabase(path </> DB_NAME) |> Some
            else None
          with
            | _ -> None

/// ## Get a collection for a type
///
/// Get a collection for storing/querying a perticular type.
///
/// ### Signature:
/// - name: the name of the collection
/// - db: the LiteDatabase holding the collection
///
/// Returns: LiteCollection<'t>
let getCollection<'t when 't : (new : unit -> 't)> (name: string) (db: LiteDatabase) =
  db.GetCollection<'t> name

/// ## Add/update an index
///
/// Add an index to a collection (or update if it already exists).
///
/// ### Signature:
/// - field: string name of field to index
/// - collection: collection to create index on
///
/// Returns: LiteCollection<'t>
let ensureIndex field (collection: LiteCollection<'t>) =
  collection.EnsureIndex(field) |> ignore
  collection

/// ## Count entries in a collection
///
/// Count all entries in a collection
///
/// ### Signature:
/// - collection: collection to count entries for
///
/// Returns: int
let countEntries (collection: LiteCollection<'t>) =
  collection.Count()

/// ## Insert a new record into a collection
///
/// Insert a new document into the collection specified.
///
/// ### Signature:
/// - thing: document to insert
/// - collection: collection to add document to
///
/// Returns: unit
let insert<'t when 't : (new : unit -> 't)>
          (thing: 't)
          (collection: LiteCollection<'t>) =
  collection.Insert thing |> ignore

/// ## Insert many documents in bulk
///
/// Insert an array of documents in bulk in the collection supplied.
///
/// ### Signature:
/// - things: array of documents to add
/// - collection: collection to add documents to
///
/// Returns: unit
let insertMany<'t when 't : (new : unit -> 't)>
              (things: 't array)
              (collection: LiteCollection<'t>) =
  collection.InsertBulk things |> ignore


/// ## Update many
///
/// Description
///
/// ### Signature:
/// - arg: arg
/// - arg: arg
/// - arg: arg
///
/// Returns: Type
let updateMany<'t when 't : (new : unit -> 't)>
              (things: 't array)
              (collection: LiteCollection<'t>) =
  Array.iter (collection.Update >> ignore) things

/// ## Update a document
///
/// Update a document in the given collection.
///
/// ### Signature:
/// - thing: document to update
/// - collection: collection the document lives in
///
/// Returns: unit
let update<'t when 't : (new : unit -> 't)>
          (thing: 't)
          (collection: LiteCollection<'t>) =
  collection.Update thing

/// ## Find a document by its id
///
/// Find a document by its id. Indicates failure to find it by returning None.
///
/// ### Signature:
/// - id: string id to search for
/// - collection: collection to search in
///
/// Returns: 't option
let findById<'t when 't : (new : unit -> 't) and 't : null>
            (id: string)
            (collection: LiteCollection<'t>) =
  let result = collection.FindById(new BsonValue(id))
  if isNull result then
    None
  else Some result

/// ## List all documents in a collection
///
/// Finds all documents in a given collection.
///
/// ### Signature:
/// - collection: collection to enumerate entries of
///
/// Returns: 't list
let findAll<'t when 't : (new : unit -> 't) and 't : null>
           (collection: LiteCollection<'t>) =
  collection.FindAll()
  |> List.ofSeq

/// ## Delete a document by its id
///
/// Delete a document by its ID.
///
/// ### Signature:
/// - id: string id of document to delete
/// - collection: collection the document lives in
///
/// Returns: bool
let inline deleteById< ^a, 't when 't : (new : unit -> 't)>
                    (id: ^a)
                    (collection: LiteCollection<'t>) =
  collection.Delete(new BsonValue(id))

/// ## Delete many documents by id
///
/// Description
///
/// ### Signature:
/// - arg: arg
/// - arg: arg
/// - arg: arg
///
/// Returns: Type
let inline deleteMany< ^a, 't when 't : (new : unit -> 't)>
                     (ids: ^a array)
                     (collection: LiteCollection<'t>) =
  let folder m id = deleteById id collection && m
  Array.fold folder true ids

/// ## Clear an entire database
///
/// Clear a database of all its entries.
///
/// ### Signature:
/// - db: LiteDatabase to truncate
///
/// Returns: unit
let truncateDB (db: LiteDatabase)  =
  for name in db.GetCollectionNames() do
    let collection = db.GetCollection(name)
    collection.Drop() |> ignore

/// ## Clear a collections records
///
/// Clear a collection from all documents.
///
/// ### Signature:
/// - collection: collection to clear
///
/// Returns: unit
let truncateCollection (collection: LiteCollection<'t>) =
  collection.Drop() |> ignore

/// ## Initialize the metadata document
///
/// Initialize the metadata document in the given LiteDatabase.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: unit
let initMetadata (db: LiteDatabase) =
  let metadata = new RaftMetaData()
  getCollection<RaftMetaData> "metadata" db
  |> insert metadata
  metadata

/// ## Get the collection for the Raft metadata document
///
/// Get the collection the metadata doc is stored in.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: LiteCollectio<RaftMetadata>
let raftCollection (db: LiteDatabase) =
  getCollection<RaftMetaData> "metadata" db

/// ## Get metadata
///
/// Get the raft metadata document from this database.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: RaftMetadata option
let getMetadata (db: LiteDatabase) =
  raftCollection db
  |> findById RaftMetaData.Guid

/// ## Get the log collection
///
/// Get the LogData collection from given database.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: LiteCollection<LogData>
let logCollection (db: LiteDatabase) =
  getCollection<LogData> "logs" db

/// ## Get the node collecti
///
/// Get the node collection from given database.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: LiteCollection<NodeMetaData>
let nodeCollection (db: LiteDatabase) =
  getCollection<NodeMetaData> "nodes" db

/// ## Find a node by its id.
///
/// Find a node by its ID. Indicates failure by returning None.
///
/// ### Signature:
/// - id: NodeId to search for
/// - db: LiteDatabase
///
/// Returns: Node option
let findNode (id: NodeId) (db: LiteDatabase) =
  nodeCollection db
  |> ensureIndex "_id"
  |> findById (string id)
  |> Option.map (fun meta -> meta.ToNode())

/// ## list all nodes in database
///
/// List all nodes saved in a database.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: Node list
let allNodes db =
  nodeCollection db
  |> findAll
  |> List.map (fun meta -> meta.ToNode())

/// ## find a log by its id
///
/// Description
///
/// ### Signature:
/// - arg: arg
/// - arg: arg
/// - arg: arg
///
/// Returns: LogEntry option
let findLog (id: Id) (db: LiteDatabase) =
  logCollection db
  |> ensureIndex "_id"
  |> findById (string id)
  |> Option.map (fun (meta: LogData) -> meta.ToLog())

/// ## Insert a log sequence into given database
///
/// Inserts a log chain into the given database
///
/// ### Signature:
/// - log: LogEntry to insert into database
/// - db: LiteDatabase to insert log into
///
/// Returns: unit
let insertLogs (log: LogEntry) (db: LiteDatabase) =
  logCollection db |> insertMany (LogData.FromLog log)

/// ## Insert a log sequence into given database
///
/// Inserts a log chain into the given database
///
/// ### Signature:
/// - log: LogEntry to insert into database
/// - db: LiteDatabase to insert log into
///
/// Returns: unit
let updateLogs (log: LogEntry) (db: LiteDatabase) =
  logCollection db |> updateMany (LogData.FromLog log)

/// ## Delete the given log from the database
///
/// Deletes the top-most log entry from the given database
///
/// ### Signature:
/// - log: LogEntry to delete
/// - db: LiteDatabase
///
/// Returns: bool
let deleteLog (log: LogEntry) (db: LiteDatabase) =
  logCollection db |> deleteById (Log.id log |> string)

/// ## Delete all logs in passed log chain
///
/// Delete all logs in the passed log chain.
///
/// ### Signature:
/// - log: LogEntry list to delete
/// - db: LiteDatabase to delete entries from
///
/// Returns: bool
let deleteLogs (log: LogEntry) (db: LiteDatabase) =
  let ids =
    Log.map (Log.id >> string) log
    |> Array.ofList
  logCollection db |> deleteMany ids

let countLogs (db: LiteDatabase) =
  logCollection db |> countEntries

/// ## Enumerate all logs in order of insertion
///
/// Enumerate all logs in their respective order of insertion.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: LogData list
let allLogs (db: LiteDatabase) =
  logCollection db
  |> ensureIndex "_id"
  |> findAll
  |> List.sort

/// ## Get the actual LogEntry
///
/// Get the entire LogEntry value from the database. This is the entire chain of logs present,
/// in the order entry. This function does not yet check for consistency when log entries are
/// present that have already been deleted in practice [FIXME].
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: LogEntry option
let getLogs (db: LiteDatabase) =
  let folder (prev: LogEntry option) (d: LogData) =
    match d.ToLog() with
    | Configuration(id,idx,term,nodes,_)    -> Configuration(id,idx,term,nodes,prev)
    | JointConsensus(id,idx,term,changes,_) -> JointConsensus(id,idx,term,changes,prev)
    | LogEntry(id,idx,term,data,_)          -> LogEntry(id,idx,term,data,prev)
    | Snapshot _ as snapshot                -> snapshot
    |> Some

  match allLogs db with
    | []  -> None
    | lst -> List.fold folder None lst

/// ## Save the raw RaftMetaData document to disk
///
/// Save the raw RaftMetaData document to disk
///
/// ### Signature:
/// - meta: RaftMetaData
/// - db: LiteDatabase
///
/// Returns: unit
let saveRaftMetadata (meta: RaftMetaData) (db: LiteDatabase) =
  raftCollection db |> update meta |> ignore

/// ## Save Raft metadata
///
/// Save the Raft metadata
///
/// ### Signature:
/// - raft: RAft value to save
/// - db: LiteDatabase
///
/// Returns: unit
let saveMetadata (raft: Raft) (db: LiteDatabase) =
  let meta =
    match getMetadata db with
      | Some m -> m
      | _      -> initMetadata db

  meta.NodeId <- string raft.Node.Id
  meta.Term   <- int64  raft.CurrentTerm
  meta.State  <- string raft.State

  match raft.CurrentLeader with
    | Some nid -> meta.LeaderId <- string nid
    | _        -> meta.LeaderId <- null

  match raft.OldPeers with
    | Some peers ->
      let nids = Map.toArray peers |> Array.map (fst >> string)
      meta.OldPeers <- nids
    | _ ->
      meta.OldPeers <- null

  meta.TimeoutElapsed   <- int64 raft.TimeoutElapsed
  meta.ElectionTimeout  <- int64 raft.ElectionTimeout
  meta.RequestTimeout   <- int64 raft.RequestTimeout
  meta.MaxLogDepth      <- int64 raft.MaxLogDepth
  meta.CommitIndex      <- int64 raft.CommitIndex
  meta.LastAppliedIndex <- int64 raft.LastAppliedIdx

  match raft.ConfigChangeEntry with
    | Some entry -> meta.ConfigChangeEntry <- string <| Log.id entry
    | _          -> meta.ConfigChangeEntry <- null

  match raft.VotedFor with
    | Some nid ->
      meta.VotedFor <- string nid
    | _ ->
      meta.VotedFor <- null

  saveRaftMetadata meta db
  meta


/// ## Save a set of log entries
///
/// Save a set of log entries to a database
///
/// ### Signature:
/// - raft: Raft value to save log of
/// - db: LiteDatabase
///
/// Returns: unit
let saveLog (raft: Raft) (db: LiteDatabase) =
  match Log.entries raft.Log with
    | Some entries ->
      logCollection db
      |> insertMany (LogData.FromLog entries)
      |> ignore
    | _ -> ()

/// ## Save all nodes to database
///
/// Save nodes to the database.
///
/// ### Signature:
/// - raft: Raft value to save nodes from
/// - db: LiteDatabase
///
/// Returns: unit
let saveNodes (raft: Raft) (db: LiteDatabase) =
  let collection = nodeCollection db
  Map.iter (fun _ node ->
              NodeMetaData.FromNode(node)
              |> flip insert collection)
            raft.Peers

/// ## Truncate all nodes in database
///
/// Remove all nodes from database
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: unit
let truncateNodes (db: LiteDatabase) =
  nodeCollection db |> truncateCollection

/// ## Remove all log entries
///
/// Remove all log entries from database.
///
/// ### Signature:
/// - db: LiteDatabase
///
/// Returns: unit
let truncateLog (db: LiteDatabase) =
  logCollection db |> truncateCollection

/// ## summary
///
/// Description
///
/// ### Signature:
/// - arg: arg
/// - arg: arg
/// - arg: arg
///
/// Returns: Type
let saveRaft (raft: Raft) (db: LiteDatabase) =
  truncateLog   db
  truncateNodes db
  saveMetadata raft db |> ignore
  saveLog      raft db
  saveNodes    raft db

/// ## Get the saved Raft instance from the log.
///
/// Construct a raft value from the current state in the database.
///
/// ### Signature:
/// - db:  LiteDatabase
///
/// Returns: Raft option
let loadRaft db =
  match getMetadata db with
    | Some meta ->
      let log =
        match getLogs db with
          | Some entries -> Log.fromEntries entries
          | _            -> Log.empty

      let nodes = allNodes db
      let self = List.tryFind (Node.getId >> ((=) (Id meta.NodeId))) nodes
      let votedfor =
        match meta.VotedFor with
          | null   -> None
          | str -> Id str |> Some

      let state = RaftState.Parse meta.State

      let leader =
        match meta.LeaderId with
          | null   -> None
          | str -> Id str |> Some

      let oldpeers =
        match meta.OldPeers with
          | null   -> None
          | arr -> List.fold (fun lst (n: Node) ->
                              if Array.contains (string n.Id) arr then
                                (n.Id, n)  :: lst
                              else lst) [] nodes
                  |> Map.ofList
                  |> Some

      let configchange =
        match meta.ConfigChangeEntry with
          | null   -> None
          | str -> Log.find (Id str) log

      match self with
        | Some node ->
          { Node              = node
          ; State             = state
          ; CurrentTerm       = uint64 meta.Term
          ; CurrentLeader     = leader
          ; Peers             = List.map (fun (n: Node)-> (n.Id,n)) nodes |> Map.ofList
          ; OldPeers          = oldpeers
          ; NumNodes          = List.length nodes |> uint64
          ; VotedFor          = votedfor
          ; Log               = log
          ; CommitIndex       = uint64 meta.CommitIndex
          ; LastAppliedIdx    = uint64 meta.LastAppliedIndex
          ; TimeoutElapsed    = uint64 meta.TimeoutElapsed
          ; ElectionTimeout   = uint64 meta.ElectionTimeout
          ; RequestTimeout    = uint64 meta.RequestTimeout
          ; MaxLogDepth       = uint64 meta.MaxLogDepth
          ; ConfigChangeEntry = configchange
          } |> Some
        | _ -> None
    | _ -> None

let dumpDb (database: LiteDatabase) =
  sprintf "Logs: %s\nNodes: %s\nMetadata: %s"
    (allLogs  database |> List.fold (fun s l -> sprintf "%s\n%s" s (l.ToString())) "")
    (allNodes database |> List.fold (fun s l -> sprintf "%s\n%s" s (l.ToString())) "")
    (getMetadata database |> string)
