namespace Iris.Tests

open System
open System.Threading
open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Service
open Pallet.Core
open FSharpx.Functional
open ZeroMQ

[<AutoOpen>]
module RaftIntegrationTests =

  //  _   _ _   _ _ _ _   _
  // | | | | |_(_) (_) |_(_) ___  ___
  // | | | | __| | | | __| |/ _ \/ __|
  // | |_| | |_| | | | |_| |  __/\__ \
  //  \___/ \__|_|_|_|\__|_|\___||___/

  let mkTmpPath snip =
    let basePath =
      match Environment.GetEnvironmentVariable("IN_NIX_SHELL") with
        | "1" -> "/tmp/"
        | _   -> System.IO.Path.GetTempPath()
    basePath </> snip

  let createConfig rid idx start lip lpidx  =
    let portbase = 8000
    { RaftId     = rid
    ; Debug      = true
    ; IpAddr     = "127.0.0.1"
    ; WebPort    = (portbase - 1000) + idx
    ; RaftPort   = portbase + idx
    ; Start      = start
    ; LeaderIp   = lip
    ; LeaderPort = Option.map (fun n -> uint32 portbase + n) lpidx
    ; MaxRetries = 5u
    ; DataDir    = createGuid() |> string |> mkTmpPath
    }

  let createFollower (rid: string) (portidx: int) lid lpidx =
    createConfig rid portidx false (Some "127.0.0.1") (Some (uint32 lpidx))

  let createLeader (rid: string) (portidx: int) =
    createConfig rid portidx true None None

  open System.Linq

  /// ## delete a file or directory
  ///
  /// recursively delete a directory or single File.
  ///
  /// ### Signature:
  /// - path: FilePath to delete
  ///
  /// Returns: unit
  let rec delete path =
    match IO.DirectoryInfo(path).Attributes with
      | IO.FileAttributes.Directory ->
        let children = IO.DirectoryInfo(path).EnumerateFileSystemInfos()
        if children.Count() > 0 then
          for child in children do
            delete child.FullName
          IO.Directory.Delete(path)
        else
          IO.Directory.Delete(path)
      | _ ->
        IO.File.Delete path

  //  ____  ____    _____         _
  // |  _ \| __ )  |_   _|__  ___| |_ ___
  // | | | |  _ \    | |/ _ \/ __| __/ __|
  // | |_| | |_) |   | |  __/\__ \ |_\__ \
  // |____/|____/    |_|\___||___/\__|___/

  open Iris.Service.Raft.Utilities
  open Iris.Service.Raft.Db
  open Iris.Service.Raft.Server

  let test_should_create_database =
    testCase "should store load raft correctly" <| fun _ ->
      let path = mkTmpPath "test_should_create_database"
      let db = createDB path
      expect "Raft db should exist" true Option.isSome db
      Option.get db |> dispose
      delete path

  let test_should_store_load_raftmetadata_correctly =
    testCase "should store load raftmetadata correctly" <| fun _ ->
      let rid = "0xdeadbeef"
      let path = mkTmpPath "test_should_store_load_raftmetadata_correctly"
      let db = createDB path |> Option.get
      let col = getCollection<RaftMetaData> "metadata" db

      let meta = new RaftMetaData()
      meta.RaftId <- rid
      meta.Term <- 666L

      insert meta col |> ignore
      closeDB db

      // re-open the database
      let db = openDB path |> Option.get
      let col = getCollection<RaftMetaData> "metadata" db
      let loaded = findById meta._id col |> Option.get

      expect "_id should be the same"    meta._id  id loaded._id
      expect "Term should be the same"   meta.Term id loaded.Term
      expect "RaftId should be the same" rid       id loaded.RaftId

      loaded.RaftId <- "hi"
      loaded.Term <- 800L

      let result = update loaded col

      expect "should have been successful" true id result

      closeDB db

      // re-open the database
      let db = openDB path |> Option.get
      let col = getCollection<RaftMetaData> "metadata" db
      let updated = findById meta._id col |> Option.get

      expect "_id should be the same"    updated._id    id meta._id
      expect "Term should be the same"   updated.Term   id loaded.Term
      expect "RaftId should be the same" updated.RaftId id loaded.RaftId

      closeDB db
      delete path

  let test_save_restore_log_values_correctly =
    testCase "save/restore log values correctly" <| fun _ ->
      let info1 =
        { MemberId = createGuid()
        ; HostName = "Hans"
        ; IpAddr = IpAddress.Parse "192.168.1.20"
        ; Port = 8080
        ; Status = IrisNodeStatus.Running
        ; TaskId = None }

      let info2 =
        { MemberId = createGuid()
        ; HostName = "Klaus"
        ; IpAddr = IpAddress.Parse "192.168.1.22"
        ; Port = 8080
        ; Status = IrisNodeStatus.Failed
        ; TaskId = createGuid() |> Some }

      let node1 = Node.create (RaftId.Create()) info1
      let node2 = Node.create (RaftId.Create()) info2

      let changes = [| NodeRemoved node2 |]
      let nodes = [| node1; node2 |]

      let log =
        Some <| LogEntry(RaftId.Create(), 7UL, 1UL, Close "cccc",
          Some <| LogEntry(RaftId.Create(), 6UL, 1UL, AddClient "bbbb",
            Some <| Configuration(RaftId.Create(), 5UL, 1UL, [| node1 |],
              Some <| JointConsensus(RaftId.Create(), 4UL, 1UL, changes,
                Some <| Snapshot(RaftId.Create(), 3UL, 1UL, 2UL, 1UL, nodes, DataSnapshot "aaaa")))))

      let depth = log |> Option.get |> Log.depth |> int
      let path = mkTmpPath "test_save_restore_log_values_correctly"

      let db = createDB path |> Option.get
      let col = logCollection db

      let logdatas = LogData.FromLog(Option.get log)

      expect "LogDatas should have correct length" depth id (Array.length logdatas)

      insertMany logdatas col |> ignore

      expect "Should have correct number of log entries" depth id (countEntries col)

      dispose db

      let db = openDB path |> Option.get
      let loaded = getLogs db

      expect "Logs should be structurally equal" log id loaded

      dispose db
      delete path


  let test_save_restore_raft_value_correctly =
    testCase "save/restore raft value correctly" <| fun _ ->
      let info1 =
        { MemberId = createGuid()
        ; HostName = "Hans"
        ; IpAddr = IpAddress.Parse "192.168.1.20"
        ; Port = 8080
        ; Status = IrisNodeStatus.Running
        ; TaskId = None }

      let info2 =
        { MemberId = createGuid()
        ; HostName = "Klaus"
        ; IpAddr = IpAddress.Parse "192.168.1.22"
        ; Port = 8080
        ; Status = IrisNodeStatus.Failed
        ; TaskId = createGuid() |> Some }

      let node1 = Node.create (RaftId.Create()) info1
      let node2 = Node.create (RaftId.Create()) info2

      let changes = [| NodeRemoved node2 |]
      let nodes = [| node1; node2 |]

      let log =
        LogEntry(RaftId.Create(), 7UL, 1UL, Close "cccc",
          Some <| LogEntry(RaftId.Create(), 6UL, 1UL, AddClient "bbbb",
            Some <| Configuration(RaftId.Create(), 5UL, 1UL, [| node1 |],
              Some <| JointConsensus(RaftId.Create(), 4UL, 1UL, changes,
                Some <| Snapshot(RaftId.Create(), 3UL, 1UL, 2UL, 1UL, nodes, DataSnapshot "aaaa")))))
        |> Log.fromEntries

      let raft =
        { (createLeader "0x01" 1 |> createRaft) with
            Log = log
            CurrentTerm = 666UL }

      let path = mkTmpPath "save_restore-raft_value-correctly"
      let db = createDB path |> Option.get

      saveRaft raft db

      dispose db

      let db = openDB path |> Option.get

      let loaded = loadRaft db

      expect "Values should be equal" (Some raft) id loaded

      dispose db
      delete path


  let test_validate_logs_get_deleted_correctly =
    testCase "validate logs get deleted correctly" <| fun _ ->
      let info1 =
        { MemberId = createGuid()
        ; HostName = "Hans"
        ; IpAddr = IpAddress.Parse "192.168.1.20"
        ; Port = 8080
        ; Status = IrisNodeStatus.Running
        ; TaskId = None }

      let info2 =
        { MemberId = createGuid()
        ; HostName = "Klaus"
        ; IpAddr = IpAddress.Parse "192.168.1.22"
        ; Port = 8080
        ; Status = IrisNodeStatus.Failed
        ; TaskId = createGuid() |> Some }

      let node1 = Node.create (RaftId.Create()) info1
      let node2 = Node.create (RaftId.Create()) info2

      let changes = [| NodeRemoved node2 |]
      let nodes = [| node1; node2 |]

      let log =
        LogEntry(RaftId.Create(), 7UL, 1UL, Close "cccc",
          Some <| LogEntry(RaftId.Create(), 6UL, 1UL, AddClient "bbbb",
            Some <| Configuration(RaftId.Create(), 5UL, 1UL, [| node1 |],
              Some <| JointConsensus(RaftId.Create(), 4UL, 1UL, changes,
                Some <| Snapshot(RaftId.Create(), 3UL, 1UL, 2UL, 1UL, nodes, DataSnapshot "aaaa")))))

      let count = int <| Log.depth log

      let path = mkTmpPath "test_validate_logs_get_deleted_correctly"
      let db = createDB path |> Option.get

      insertLogs log db

      expect "Should have correct number of logs" (countLogs db) id count

      let result = deleteLog log db

      expect "Should have succeeded" true id result
      expect "Should have correct number of logs" (countLogs db) id (count - 1)

      let result = deleteLog (log) db

      expect "Should have failed" false id result
      expect "Should have correct number of logs" (countLogs db) id (count - 1)

      let result = deleteLog (Log.fromEntries log |> Log.prevEntry  |> Option.get) db

      expect "Should have correct number of logs" (countLogs db) id (count - 2)

      truncateLog db

      expect "Should have no logs" (countLogs db) id 0

      insertLogs log db

      expect "Should have correct number of logs" (countLogs db) id count

      let result = deleteLogs log db

      expect "Should have no logs" (countLogs db) id 0
      expect "Should have succeeded" true id result

      dispose db
      delete path

  let test_log_snapshotting_should_clean_all_logs =
    pending "log snapshotting should clean all logs"

  //  ____        __ _     _____         _
  // |  _ \ __ _ / _| |_  |_   _|__  ___| |_ ___
  // | |_) / _` | |_| __|   | |/ _ \/ __| __/ __|
  // |  _ < (_| |  _| |_    | |  __/\__ \ |_\__ \
  // |_| \_\__,_|_|  \__|   |_|\___||___/\__|___/

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      let ctx = new ZContext()

      let leadercfg = createLeader "0x01" 1
      let leader = new RaftServer(leadercfg, ctx)
      leader.Start()

      let follower = new RaftServer(leadercfg, ctx)
      follower.Start()
      expect "Should be in failed state" true hasFailed follower.ServerState

      dispose leader
      dispose follower

      dispose ctx

  let test_validate_follower_joins_leader_after_startup =
    testCase "validate follower joins leader after startup" <| fun _ ->
      let leaderid = "0x01"
      let followerid1 = "0x02"
      let followerid2 = "0x03"

      let mutable stop = false

      let leadercfg = createLeader leaderid 1
      let followercfg1 = createFollower followerid1 2 leaderid 1
      let followercfg2 = createFollower followerid2 3 leaderid 1

      let makeServer cfg _ =
        let tid = Thread.CurrentThread.ManagedThreadId
        printfn "PRIMARY THREAD FOR %A is %d" cfg.RaftId tid
        let ctx = new ZContext()
        let server = new RaftServer(cfg, ctx)
        server.Start()

        while not stop do
          Thread.Sleep(10)

        dispose server
        dispose ctx


      let leader = new Thread(new ThreadStart(makeServer leadercfg))
      leader.Start()

      Thread.Sleep(1000)

      let follower1 = new Thread(new ThreadStart(makeServer followercfg1))
      follower1.Start()

      // let follower2 = new Thread(new ThreadStart(makeServer followercfg2))
      // follower2.Start()

      Thread.Sleep(10000)

      leader.Stop()
      follower.Stop()


  let test_follower_join_should_fail_on_duplicate_raftid =
    pending "follower join should fail on duplicate raftid"

  let test_all_rafts_should_share_a_common_distributed_event_log =
    pending "all rafts should share a common distributed event log"

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let raftIntegrationTests =
    testList "Raft Integration Tests" [
        // db
        // test_should_create_database
        // test_should_store_load_raftmetadata_correctly
        // test_save_restore_log_values_correctly
        // test_save_restore_raft_value_correctly
        // test_validate_logs_get_deleted_correctly
        // test_log_snapshotting_should_clean_all_logs

        // raft
        // test_validate_raft_service_bind_correct_port
        test_validate_follower_joins_leader_after_startup
        // test_follower_join_should_fail_on_duplicate_raftid
        // test_all_rafts_should_share_a_common_distributed_event_log
      ]
