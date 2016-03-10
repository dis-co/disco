namespace Iris.Tests

open System
open System.IO
open System.Linq
open System.Threading
open Fuchu
open Fuchu.Test
open Iris.Raft.FFI

open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

module Raft =
  
  let inline addr (data : GCHandle) = data.AddrOfPinnedObject()
  let inline free (ptr  : GCHandle) = ptr.Free()
  
  let pin (data : 'a) = GCHandle.Alloc(data,GCHandleType.Pinned)

  //  ___           _              _   _       _
  // |_ _|_ __  ___| |_ __ _ _ __ | |_(_) __ _| |_ ___
  //  | || '_ \/ __| __/ _` | '_ \| __| |/ _` | __/ _ \
  //  | || | | \__ \ || (_| | | | | |_| | (_| | ||  __/
  // |___|_| |_|___/\__\__,_|_| |_|\__|_|\__,_|\__\___|
 
  let createRaftTest =
    testCase "Instantiate Raft" <|
      fun _ ->
        RaftNew() |> ignore

  //  _                   _
  // | |    ___  __ _  __| | ___ _ __
  // | |   / _ \/ _` |/ _` |/ _ \ '__|
  // | |__|  __/ (_| | (_| |  __/ |
  // |_____\___|\__,_|\__,_|\___|_|
 
  let becomeLeaderTest =
    testCase "Becoming leader" <|
      fun _ ->
        let myid = 1
        let raft = RaftNew()
        let node = AddNode(raft, 0n, 1, 1)

        Assert.Equal("Should not be leader yet", true,
                     IsLeader(raft) = 0)

        BecomeLeader(raft)
        
        Assert.Equal("Should be leader now", true,
                     IsLeader(raft) = 1)

  //  ____       _  __   _   _           _
  // / ___|  ___| |/ _| | \ | | ___   __| | ___
  // \___ \ / _ \ | |_  |  \| |/ _ \ / _` |/ _ \
  //  ___) |  __/ |  _| | |\  | (_) | (_| |  __/
  // |____/ \___|_|_|   |_| \_|\___/ \__,_|\___|

  let createSelfNodeTest =
    testCase "Create self node and check IDs" <|
      fun _ ->
        let myid = 1
        let raft = RaftNew()

        let mutable data = "hfh"
        let mutable cbs = new RaftCallbacks()

        let mutable cpsh = pin cbs
        let mutable datah = pin data
        
        Marshal.StructureToPtr<RaftCallbacks>(cbs, addr(cpsh), true)
        SetCallbacks(raft, addr(cpsh), addr(datah))

        let node = AddNode(raft, 0n, myid, 1)

        Assert.Equal("NodeGetId should return correct value", true,
                     NodeGetId(node) = myid)

        Assert.Equal("GetNodeId should return correct value", true,
                     GetNodeId(raft) = myid)

        free cpsh   // free GCHandle to prevent memory leaks
        free datah  // "

  //  _   _           _
  // | \ | | ___   __| | ___  ___
  // |  \| |/ _ \ / _` |/ _ \/ __|
  // | |\  | (_) | (_| |  __/\__ \
  // |_| \_|\___/ \__,_|\___||___/

  let addMoreNodesTest =
    testCase "Add more nodes and check count" <|
      fun _ ->
        let myid = 1
        let raft = RaftNew()

        let self = AddNode(raft, 0n, myid, 1)
        let other1 = AddNode(raft, 0n, myid + 1, 0)
        let other2 = AddNode(raft, 0n, myid + 2, 0)

        Assert.Equal("GetNumNodes should return correct value", true,
                     GetNumNodes(raft) = 3)

  [<Tests>]
  let raftTests =
    testList "Raft Tests" [
        createRaftTest
        createSelfNodeTest
        addMoreNodesTest
        becomeLeaderTest
      ]
