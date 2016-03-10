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

  
  let inline (~~) (data : GCHandle) = data.AddrOfPinnedObject()
  let inline (!~) (ptr  : GCHandle) = ptr.Free()
  
  let pin (data : 'a) = GCHandle.Alloc(data,GCHandleType.Pinned)

  //   _                    _    ______
  //  | |    ___   __ _  __| |  / / ___|  __ ___   _____
  //  | |   / _ \ / _` |/ _` | / /\___ \ / _` \ \ / / _ \
  //  | |__| (_) | (_| | (_| |/ /  ___) | (_| |\ V /  __/
  //  |_____\___/ \__,_|\__,_/_/  |____/ \__,_| \_/ \___|ed
  //
  let createRaftTest =
    testCase "Instantiate Raft" <|
      fun _ ->
        let raft = RaftNew()
        let mutable data = "hfh"
        let mutable cbs = new RaftCallbacks()

        let mutable cpsptr = ~~(pin cbs)
        let mutable dataptr = ~~(pin data)

        Marshal.StructureToPtr<RaftCallbacks>(cbs, cpsptr, true)

        SetCallbacks(raft, cpsptr, dataptr)

  [<Tests>]
  let raftTests =
    testList "Raft Tests" [
        createRaftTest
      ]
