namespace Iris.Service

open System
open System.Threading

open Iris.Core
open Iris.Core.Utils

open Pallet.Core

open fszmq
open fszmq.Context
open fszmq.Socket
open fszmq.Polling

open FSharpx.Stm
open FSharpx.Functional

[<AutoOpen>]
module RaftServer =

  // -------------------------------------------------------------------------------------------- //
  //                                        ____  ____                                            //
  //                                       |  _ \| __ )                                           //
  //                                       | | | |  _ \                                           //
  //                                       | |_| | |_) |                                          //
  //                                       |____/|____/                                           //
  // -------------------------------------------------------------------------------------------- //

  module DB =

    open LiteDB
    open Nessos.FsPickler

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

        let id   = RaftId self._id
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
        meta._id   <- string node.Id
        meta.State <- string node.State
        meta.Data  <- coder.Pickle(node.Data)
        meta.Voting <- node.Voting
        meta.VotedForMe <- node.VotedForMe
        meta.NextIndex <- int64 node.NextIndex
        meta.MatchIndex <- int64 node.MatchIndex
        meta

      //  ____            _       _ _
      // / ___|  ___ _ __(_) __ _| (_)_______
      // \___ \ / _ \ '__| |/ _` | | |_  / _ \
      //  ___) |  __/ |  | | (_| | | |/ /  __/
      // |____/ \___|_|  |_|\__,_|_|_/___\___|

      member self.ToNode () : Node =
        let coder = FsPickler.CreateBinarySerializer()
        { Id         = RaftId self._id
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

      member __._id
        with get ()          = METADATA_ID
         and set (_: string) = ()

      member __.RaftId
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

      static member Id = METADATA_ID

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
    let insert<'t when 't : (new : unit -> 't)> (thing: 't) (collection: LiteCollection<'t>) =
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
    let insertMany<'t when 't : (new : unit -> 't)> (things: 't array) (collection: LiteCollection<'t>) =
      collection.InsertBulk things |> ignore

    /// ## Update a document
    ///
    /// Update a document in the given collection.
    ///
    /// ### Signature:
    /// - thing: document to update
    /// - collection: collection the document lives in
    ///
    /// Returns: unit
    let update<'t when 't : (new : unit -> 't)> (thing: 't) (collection: LiteCollection<'t>) =
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
    let findById<'t when 't : (new : unit -> 't) and 't : null> (id: string) (collection: LiteCollection<'t>) =
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
    let findAll<'t when 't : (new : unit -> 't) and 't : null> (collection: LiteCollection<'t>) =
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
    let deleteById<'t when 't : (new : unit -> 't)> (id: string) (collection: LiteCollection<'t>) =
      let bson = new BsonValue(id)
      collection.Delete(bson)

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
        let col = db.GetCollection(name)
        col.Drop() |> ignore

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
      |> findById RaftMetaData.Id

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
      |> Option.map (fun meta -> meta.ToLog())

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

      meta.RaftId <- string raft.Node.Id
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

      raftCollection db
      |> update meta
      |> ignore

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
      let col = nodeCollection db
      Map.iter (fun _ node ->
                  NodeMetaData.FromNode(node)
                  |> flip insert col)
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
      saveMetadata raft db
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
          let self = List.tryFind (Node.getId >> ((=) (RaftId meta.RaftId))) nodes
          let votedfor =
            match meta.VotedFor with
              | null   -> None
              | str -> RaftId str |> Some

          let state = RaftState.Parse meta.State

          let leader =
            match meta.LeaderId with
              | null   -> None
              | str -> RaftId str |> Some

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
              | str -> Log.find (RaftId str) log

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


  // ------------------------------------------------------------------------------------- //
  //                            _   _ _   _ _ _ _   _                                      //
  //                           | | | | |_(_) (_) |_(_) ___  ___                            //
  //                           | | | | __| | | | __| |/ _ \/ __|                           //
  //                           | |_| | |_| | | | |_| |  __/\__ \                           //
  //                            \___/ \__|_|_|_|\__|_|\___||___/                           //
  // ------------------------------------------------------------------------------------- //

  module Utilities =

    /// ## Get the current machine's host name
    ///
    /// Get the current machine's host name.
    ///
    /// ### Signature:
    /// - unit: unit
    ///
    /// Returns: string
    let getHostName () =
      System.Net.Dns.GetHostName()

    /// ## Format ZeroMQ URI
    ///
    /// Formates the given IrisNode's host metadata into a ZeroMQ compatible resource string.
    ///
    /// ### Signature:
    /// - data: IrisNode
    ///
    /// Returns: string
    let formatUri (data: IrisNode) =
      sprintf "tcp://%s:%d" (string data.IpAddr) data.Port

    /// ## Create a new Raft state
    ///
    /// Create a new initial Raft state value from the passed-in options.
    ///
    /// ### Signature:
    /// - options: RaftOptions
    ///
    /// Returns: Raft<StateMachine,IrisNode>
    let createRaft (options: RaftOptions) =
      let node =
        { MemberId = createGuid()
        ; HostName = getHostName()
        ; IpAddr   = IpAddress.Parse options.IpAddr
        ; Port     = options.RaftPort
        ; TaskId   = None
        ; Status   = IrisNodeStatus.Running }
        |> Node.create (RaftId options.RaftId)
      Raft.create node

    let loadRaft (options: RaftOptions) =
      let dir = options.DataDir </> DB.DB_NAME
      match IO.Directory.Exists dir with
        | true -> DB.openDB dir |> Option.bind DB.loadRaft
        | _    -> None

    let mkRaft (options: RaftOptions) =
      match loadRaft options with
        | Some raft -> raft
        | _         -> createRaft options

    /// ## Create an AppState value
    ///
    /// Given the `RaftOptions`, create or load data and construct a new `AppState` for the
    /// `RaftServer`.
    ///
    /// ### Signature:
    /// - context: `ZeroMQ` `Context`
    /// - options: `RaftOptions`
    ///
    /// Returns: AppState
    let mkState (context: Context) (options: RaftOptions) : AppState =
      { Clients     = []
      ; Sessions    = []
      ; Projects    = Map.empty
      ; Peers       = Map.empty
      ; Connections = Map.empty
      ; Raft        = mkRaft options
      ; Context     = context
      ; Options     = options
      }

    /// ## idiomatically cancel a CancellationTokenSource
    ///
    /// Cancels a ref to an CancellationTokenSource. Assign None when done.
    ///
    /// ### Signature:
    /// - cts: CancellationTokenSource option ref
    ///
    /// Returns: unit
    let cancelToken (cts: CancellationTokenSource option ref) =
      match !cts with
      | Some token ->
        try
          token.Cancel()
        finally
          cts := None
      | _ -> ()

  // ----------------------------------------------------------------------------------------- //
  //                                    ____ _____ __  __                                      //
  //                                   / ___|_   _|  \/  |                                     //
  //                                   \___ \ | | | |\/| |                                     //
  //                                    ___) || | | |  | |                                     //
  //                                   |____/ |_| |_|  |_|                                     //
  // ----------------------------------------------------------------------------------------- //

  module STM =

    open Utilities

    /// ## getSocket for Member
    ///
    /// Gets the socket we memoized for given MemberId, else creates one and instantiates a
    /// connection.
    ///
    /// ### Signature:
    /// - appState: current TVar<AppState>
    ///
    /// Returns: Socket
    let getSocket (node: Node) appState =
      stm {
        let! state = readTVar appState

        match Map.tryFind node.Data.MemberId state.Connections with
        | Some client -> return client
        | _           ->
          let sock = req state.Context
          let addr = formatUri node.Data

          connect sock addr

          let newstate =
            { state with
                Connections = Map.add node.Data.MemberId sock state.Connections }

          do! writeTVar appState newstate

          return sock
      }

    /// ## Send RaftMsg to node
    ///
    /// Sends given RaftMsg to node. If the request times out, None is return to indicate
    /// failure. Otherwise the de-serialized RaftMsg response is returned, wrapped in option to
    /// indicate whether de-serialization was successful.
    ///
    /// ### Signature:
    /// - thing:    RaftMsg to send
    /// - node:     node to send the message to
    /// - appState: application state TVar
    ///
    /// Returns: RaftMsg option
    let performRequest (thing: RaftRequest) (node: Node<IrisNode>) appState =
      stm {
        let mutable frames = [| |]

        let handler _ (msgs : Message array) =
          frames <- Array.map (Message.data >> decode<RaftResponse>) msgs

        let! state = readTVar appState
        let! client = getSocket node appState

        thing |> encode |>> client

        let result =
          [ pollIn handler client ]
          |> poll (int64 state.Raft.RequestTimeout)

        if result && Array.isEmpty frames |> not then
          return frames.[0]               // for now we only process single-ZFrame messages in Raft
        else
          return None
      }

    /// ## Run Raft periodic functions with AppState
    ///
    /// Runs Raft's periodic function with the current AppState.
    ///
    /// ### Signature:
    /// - elapsed: seconds elapsed
    /// - appState: AppState TVar
    /// - cbs: Raft callbacks
    ///
    /// Returns: unit
    let periodicR elapsed appState cbs =
      stm {
        let! state = readTVar appState

        do! periodic elapsed
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      }

    /// ## Add a new node to the Raft cluster
    ///
    /// Adds a new node the Raft cluster. This is done in the 2-phase commit model described in the
    /// Raft paper.
    ///
    /// ### Signature:
    /// - node: Node to be added to the cluster
    /// - appState: AppState TVar
    /// - cbs: Raft callbacks
    ///
    /// Returns: unit
    let addNodeR node appState cbs =
      stm {
        let! state = readTVar appState

        let term = currentTerm state.Raft
        let changes = [| NodeAdded node |]
        let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)
        let response = receiveEntry entry |> runRaft state.Raft cbs

        match response with
          | Right(resp, raft) ->
            do! writeTVar appState (updateRaft raft state)

          | Middle(_, raft) ->
            do! writeTVar appState (updateRaft raft state)

          | Left(err, raft) ->
            do! writeTVar appState (updateRaft raft state)
      }

    /// ## Remove a node from the Raft cluster
    ///
    /// Safely remove a node from the Raft cluster. This operation also follows the 2-phase commit
    /// model set out by the Raft paper.
    ///
    /// ### Signature:
    /// - ndoe: the node to remove from the current configuration
    /// - appState: AppState TVar
    /// - cbs: Raft callbacks
    ///
    /// Returns: unit
    let removeNodeR node appState cbs =
      stm {
        let! state = readTVar appState

        let term = currentTerm state.Raft
        let changes = [| NodeRemoved node |]
        let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)
        do! receiveEntry entry
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      }

    /// ## Redirect to leader
    ///
    /// Gets the current leader node from the Raft state and returns a corresponding RaftMsg response.
    ///
    /// ### Signature:
    /// - state: AppState
    ///
    /// Returns: Stm<RaftMsg>
    let redirectR state =
      stm {
        match getLeader state.Raft with
        | Some node -> return Redirect node
        | _         -> return ErrorResponse (OtherError "No known leader")
      }

    /// ## Handle AppendEntries requests
    ///
    /// Handler for AppendEntries requests. Returns an appropriate response value.
    ///
    /// ### Signature:
    /// - sender:   Raft node which sent the request
    /// - ae:       AppendEntries request value
    /// - appState: AppState TVar
    /// - cbs:      Raft callbacks
    ///
    /// Returns: Stm<RaftMsg>
    let handleAppendEntries sender ae appState cbs =
      stm {
        let! state = readTVar appState

        let result =
          receiveAppendEntries (Some sender) ae
          |> runRaft state.Raft cbs

        match result with
          | Right (resp, raft) ->
            do! writeTVar appState (updateRaft raft state)
            return AppendEntriesResponse(raft.Node.Id, resp)

          | Middle (resp, raft) ->
            do! writeTVar appState (updateRaft raft state)
            return AppendEntriesResponse(raft.Node.Id, resp)

          | Left (err, raft) ->
            do! writeTVar appState (updateRaft raft state)
            return ErrorResponse err
      }

    let handleAppendResponse sender ar appState cbs =
      stm {
        let! state = readTVar appState

        do! receiveAppendEntriesResponse sender ar
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      }

    let handleVoteRequest sender req appState cbs =
      stm {
        let! state = readTVar appState

        let result =
          Raft.receiveVoteRequest sender req
          |> runRaft state.Raft cbs

        match result with
          | Right  (resp, raft) ->
            do! writeTVar appState (updateRaft raft state)
            return RequestVoteResponse(raft.Node.Id, resp)

          | Middle (resp, raft) ->
            do! writeTVar appState (updateRaft raft state)
            return RequestVoteResponse(raft.Node.Id, resp)

          | Left (err, raft) ->
            do! writeTVar appState (updateRaft raft state)
            return ErrorResponse err
      }

    let handleVoteResponse sender rep appState cbs =
      stm {
        let! state = readTVar appState

        do! receiveVoteResponse sender rep
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      }

    let handleHandshake node appState cbs =
      stm {
        let! state = readTVar appState
        if isLeader state.Raft then
          do! addNodeR node appState cbs
          return Welcome
        else
          return! redirectR state
      }

    let handleHandwaive node appState cbs =
      stm {
        let! state = readTVar appState
        if isLeader state.Raft then
          do! removeNodeR node appState cbs
          return Arrivederci
        else
          return! redirectR state
      }

    let appendEntry entry appState cbs =
      stm {
        let! state = readTVar appState

        let result =
          raft {
            let! result = receiveEntry entry
            // do! periodic 1001UL
            return result
          }
          |> runRaft state.Raft cbs

        let (response, newstate) =
          match result with
            | Right  (response, newstate) -> (Some response, newstate)
            | Middle (_, newstate)        -> (None, newstate)
            | Left   (err, newstate)      -> (None, newstate)

        do! writeTVar appState (updateRaft newstate state)

        return response
      }

    let handleInstallSnapshot node snapshot appState cbs =
      stm {
        let! state = readTVar appState
        // do! createSnapshot ()
        //     |> evalRaft raft' cbs
        //     |> writeTVar raftState
        return InstallSnapshotResponse (state.Raft.Node.Id, { Term = state.Raft.CurrentTerm })
      }

    let handleRequest msg appState cbs =
      stm {
        match msg with
          | RequestVote (sender, req) ->
            return! handleVoteRequest sender req appState cbs

          | AppendEntries (sender, ae) ->
            return! handleAppendEntries  sender ae appState cbs

          | HandShake node ->
            return! handleHandshake node appState cbs

          | HandWaive node ->
            return! handleHandwaive node appState cbs

          | InstallSnapshot (sender, snapshot) ->
            return! handleInstallSnapshot sender snapshot appState cbs
      }

    let handleResponse msg appState cbs =
      stm {
        match msg with
          | RequestVoteResponse (sender, rep)  ->
            do! handleVoteResponse sender rep appState cbs

          | AppendEntriesResponse (sender, ar) ->
            do! handleAppendResponse sender ar appState cbs

          | InstallSnapshotResponse (sender, snapshot) ->
            printfn "[InstallSnapshotResponse RPC] done"

          | Redirect node ->
            failwithf "[HandShake] redirected us to %A" node

          | Welcome ->
            failwith "[HandShake] welcome to the fold"

          | Arrivederci ->
            failwith "[HandShake] bye bye "

          | ErrorResponse err ->
            failwithf "[ERROR] %A" err
      }

    let startServer appState cbs =
      stm {
        let token = new CancellationTokenSource()

        let! state = readTVar appState
        let server = rep state.Context
        let uri = state.Raft.Node.Data |> formatUri

        bind server uri

        let rec proc () =
          async {
            let msg : RaftRequest option =
              recv server |> decode

            let response =
              match msg with
              | Some message ->
                handleRequest message appState cbs
                |> atomically
              | None ->
                ErrorResponse <| OtherError "Unable to decipher request"

            response |> encode |> send server

            if token.IsCancellationRequested then
              unbind server uri
              dispose server
            else
              return! proc ()
          }

        Async.Start(proc (), token.Token)

        return token
      }

    /// ## startPeriodic
    ///
    /// Starts an asynchronous loop to run Raft's `periodic` function. Returns a token, with which the
    /// loop can be cancelled at a later time.
    ///
    /// ### Signature:
    /// - timeoput: interval at which the loop runs
    /// - appState: current AppState TVar
    /// - cbs: Raft Callbacks
    ///
    /// Returns: CancellationTokenSource
    let startPeriodic timeout appState cbs =
      stm {
        let token = new CancellationTokenSource()

        let rec proc () =
          async {
              Thread.Sleep(int timeout)                   // sleep for 100ms
              periodicR timeout appState cbs |> atomically // kick the machine
              return! proc ()                             // recurse
            }

        Async.Start(proc(), token.Token)

        return token                      // return the cancellation token source so this loop can be
      }                                   // stopped at a  later time


    // -------------------------------------------------------------------------
    let tryJoin (leader: Node<IrisNode>) appState =
      let rec _tryJoin retry node' =
        stm {
          let! state = readTVar appState

          if retry < int state.Options.MaxRetries then
            printfn "Trying to join cluster. [retry: %d] [node: %A]" retry node'

            let msg = HandShake(state.Raft.Node)
            let! result = performRequest msg node' appState

            match result with
              | Some message ->
                match message with
                  | Welcome ->
                    printfn "HandShake successful. Waiting to be updated"

                  | Redirect next ->
                    do! _tryJoin (retry + 1) next

                  | ErrorResponse err ->
                    printfn "Unexpected error occurred. %A" err
                    exit 1

                  | res ->
                    printfn "Unexpected response. Aborting.\n%A" res
                    exit 1
              | _ ->
                printfn "Node: %A unreachable. Aborting." node'.Id
                exit 1
          else
            printfn "Too many connection attempts unsuccesful. Aborting."
            exit 1
        }

      printfn "joining leader %A now" leader
      _tryJoin 0 leader

    /// ## Attempt to leave a Raft cluster
    ///
    /// Attempt to leave a Raft cluster by identifying the current cluster leader and sending an
    /// AppendEntries request with a JointConsensus entry.
    ///
    /// ### Signature:
    /// - appState: AppState TVar
    /// - cbs: Raft callbacks
    ///
    /// Returns: unit
    let tryLeave appState cbs =
      let rec _tryLeave retry (node: Node<IrisNode>) =
        stm {
          printfn "Trying to join cluster. [retry: %A] [node: %A]" retry node
          let! state = readTVar appState
          let msg = HandWaive(state.Raft.Node)
          let! result = performRequest msg node appState

          match result with
            | Some message ->
              match message with
                | Redirect other ->
                  if retry <= int state.Options.MaxRetries then
                    do! _tryLeave (retry + 1) other
                  else
                    failwith "too many retries. aborting"

                | Arrivederci ->
                  printfn "HandWaive successful."

                | ErrorResponse err ->
                  printfn "Unexpected error occurred. %A" err
                  exit 1

                | resp ->
                  printfn "Unexpected response. Aborting.\n%A" resp
                  exit 1
            | _ ->
              printfn "Node unreachable. Aborting."
              exit 1
        }

      stm {
        let! state = readTVar appState

        if not (isLeader state.Raft) then
          match Option.bind (flip getNode state.Raft) state.Raft.CurrentLeader with
            | Some node ->
              do! _tryLeave 0 node
            | _ ->
              printfn "Leader not found. Exiting without saying goodbye."
        else
          let term = currentTerm state.Raft
          let changes = [| NodeRemoved state.Raft.Node |]
          let entry = JointConsensus(RaftId.Create(), 0UL, term , changes, None)

          let! response = appendEntry entry appState cbs

          failwith "FIXME: must now block to await the committed state for response"
      }

    /// ## requestLoop
    ///
    /// Request loop.
    ///
    /// ### Signature:
    /// - inbox: MailboxProcessor
    ///
    /// Returns: Async<(Node<IrisNode> * RaftMsg)>
    let rec requestLoop appState cbs (inbox: Actor<(Node<IrisNode> * RaftRequest)>) =
      async {
        // block until there is a new message in my inbox
        let! (node, msg) = inbox.Receive()

        stm {
          let! response = performRequest msg node appState

          match response with
            | Some message ->
              do! handleResponse message appState cbs
            | _ ->
              printfn "[REQUEST TIMEOUT]: must mark node as failed now and fire a callback"
        } |> atomically

        return! requestLoop appState cbs inbox
      }

    let forceElection appState cbs =
      stm {
        let! state = readTVar appState

        do! raft {
              let! timeout = electionTimeoutM ()
              do! setTimeoutElapsedM timeout
              do! periodic timeout
            }
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      }

    let prepareSnapshot appState =
      stm {
        let! state = readTVar appState
        let snapshot = createSnapshot (DataSnapshot "snip snap snapshot") state.Raft
        return snapshot
      }

    let initialize appState cbs =
      stm {
        let! state = readTVar appState

        let term = 0UL                    // this likely needs to be adjusted when
                                          // loading state from disk

        let changes = [| NodeAdded state.Raft.Node |]
        let nodes =  [||]
        let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)

        let newstate =
          raft {
            do! setTermM term
            do! setRequestTimeoutM 500UL
            do! setElectionTimeoutM 1000UL

            if state.Options.Start then
              let! result = appendEntryM entry
              do! becomeLeader ()
              do! periodic 1001UL
            else
              let leader =
                { MemberId = createGuid()
                ; HostName = "<empty>"
                ; IpAddr = Option.get state.Options.LeaderIp   |> IpAddress.Parse
                ; Port   = Option.get state.Options.LeaderPort |> int
                ; TaskId = None
                ; Status = IrisNodeStatus.Running
                }
                |> Node.create (Option.get state.Options.LeaderId |> RaftId)
              failwith "FIXME: call tryJoin now"
          }
          |> evalRaft state.Raft cbs

        // tryJoin leader
        do! writeTVar appState (updateRaft newstate state)
      }

  open Utilities
  open STM
  open DB

  //  ____        __ _     ____                             ____  _        _
  // |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __  / ___|| |_ __ _| |_ ___
  // | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__| \___ \| __/ _` | __/ _ \
  // |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |     ___) | || (_| | ||  __/
  // |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|    |____/ \__\__,_|\__\___|

  type RaftServerState =
    | Starting
    | Running
    | Stopping
    | Stopped
    | Failed

  [<AutoOpen>]
  module RaftServerStateHelpers =

    let hasFailed = function
      | Failed -> true
      | _      -> false


  //  ____        __ _     ____
  // |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __
  // | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__|
  // |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |
  // |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|

  type RaftServer(options: RaftOptions, context: fszmq.Context) as this =
    let max_retry = 5
    let timeout = 10UL

    let database = openDB (options.DataDir </> DB.DB_NAME)

    let serverState = ref Stopped

    let servertoken   = ref None
    let workertoken   = ref None
    let periodictoken = ref None

    let cbs = this :> IRaftCallbacks<_,_>
    let appState = mkState context options |> newTVar

    let requestWorker =
      let cts = new CancellationTokenSource()
      workertoken := Some cts
      new Actor<(Node<IrisNode> * RaftRequest)> (requestLoop appState cbs, cts.Token)

    //                           _
    //  _ __ ___   ___ _ __ ___ | |__   ___ _ __ ___
    // | '_ ` _ \ / _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
    // | | | | | |  __/ | | | | | |_) |  __/ |  \__ \
    // |_| |_| |_|\___|_| |_| |_|_.__/ \___|_|  |___/

    /// ## Start the Raft engine
    ///
    /// Start the Raft engine and start processing requests.
    ///
    /// ### Signature:
    /// - unit: unit
    ///
    /// Returns: unit
    member self.Start() =
      try
        stm {
          serverState := Starting

          requestWorker.Start()

          do!  initialize appState cbs
          let! srvtkn = startServer appState cbs
          let! prdtkn = startPeriodic timeout appState cbs

          servertoken   := Some srvtkn
          periodictoken := Some prdtkn

          serverState := Running

        } |> atomically
      finally
        serverState := Failed

    /// ## Stop the Raft engine, sockets and all.
    ///
    /// Description
    ///
    /// ### Signature:
    /// - arg: arg
    /// - arg: arg
    /// - arg: arg
    ///
    /// Returns: Type
    member self.Stop() =
      match !serverState with
        | Starting | Stopping | Stopped | Failed _ -> ()
        | Running ->
          stm {
            serverState := Stopping

            // cancel the running async tasks
            cancelToken periodictoken
            cancelToken servertoken
            cancelToken workertoken

            let! state = readTVar appState

            // disconnect all cached sockets
            state.Connections
            |> Map.iter (fun (mid: MemberId) (sock: Socket) ->
                        let nodeinfo = List.tryFind (fun c -> c.MemberId = mid) state.Clients
                        match nodeinfo with
                          | Some info -> formatUri info |> disconnect sock
                          | _         -> ())

            do! writeTVar appState { state with Connections = Map.empty }

            serverState := Stopped

            failwith "STOP SHOULD ALSO PERSIST LAST STATE TO DISK"
          } |> atomically

    member self.Options
      with get () =
        let state = readTVar appState |> atomically
        state.Options
      and set opts =
        stm {
          let! state = readTVar appState
          do! writeTVar appState { state with Options = opts }
        } |> atomically

    member self.Context
      with get () = context

    /// Alas, we may only *look* at the current state.
    member self.State
      with get () = atomically (readTVar appState)

    member self.Append entry =
      appendEntry entry appState cbs
      |> atomically

    member self.EntryCommitted resp =
      stm {
        let! state = readTVar appState

        let committed =
          match responseCommitted resp |> runRaft state.Raft cbs with
          | Right (committed, _) -> committed
          | _                    -> false

        return committed
      } |> atomically

    member self.ForceTimeout() =
      failwith "FIXME: ForceTimeout"

    member self.Log msg =
      let state = self.State
      cbs.LogMsg state.Raft.Node msg

    member self.ServerState with get () = !serverState

    //  ____  _                           _     _
    // |  _ \(_)___ _ __   ___  ___  __ _| |__ | | ___
    // | | | | / __| '_ \ / _ \/ __|/ _` | '_ \| |/ _ \
    // | |_| | \__ \ |_) | (_) \__ \ (_| | |_) | |  __/
    // |____/|_|___/ .__/ \___/|___/\__,_|_.__/|_|\___|
    //             |_|

    interface IDisposable with
      member self.Dispose() =
        self.Stop()

    //  ____        __ _     ___       _             __
    // |  _ \ __ _ / _| |_  |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___
    // | |_) / _` | |_| __|  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \
    // |  _ < (_| |  _| |_   | || | | | ||  __/ |  |  _| (_| | (_|  __/
    // |_| \_\__,_|_|  \__| |___|_| |_|\__\___|_|  |_|  \__,_|\___\___|

    interface IRaftCallbacks<StateMachine,IrisNode> with
      member self.SendRequestVote node req  =
        let state = self.State
        (node, RequestVote(state.Raft.Node.Id,req))
        |> requestWorker.Post

      member self.SendAppendEntries node ae =
        let state = self.State
        (node, AppendEntries(state.Raft.Node.Id, ae))
        |> requestWorker.Post

      member self.SendInstallSnapshot node is =
        let state = self.State
        (node, InstallSnapshot(state.Raft.Node.Id, is))
        |> requestWorker.Post

      member self.ApplyLog sm      = failwith "FIXME: ApplyLog"
      member self.NodeAdded node   = failwith "FIXME: Node was added."
      member self.NodeUpdated node = failwith "FIXME: Node was updated."
      member self.NodeRemoved node = failwith "FIXME: Node was removed."
      member self.Configured nodes = failwith "FIXME: Cluster configuration done."
      member self.StateChanged o n = failwithf "FIXME: State changed from %A to %A" o n

      member self.PrepareSnapshot raft = failwith "FIXME: PrepareSnapshot"
      member self.RetrieveSnapshot () = failwith "FIXME: RetrieveSnapshot"
      member self.PersistSnapshot log = failwith "FIXME: PersistSnapshot"
      member self.PersistVote node = failwith "FIXME: PersistVote"
      member self.PersistTerm node = failwith "FIXME: PersistTerm"
      member self.PersistLog log   = failwith "FIXME: LogOffer"
      member self.DeleteLog log    = failwith "FIXME: LogPoll"

      member self.HasSufficientLogs node = failwith "FIXME: HasSufficientLogs"

      member self.LogMsg node str =
        if options.Debug then
          printfn "%s" str
