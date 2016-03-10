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

module RaftTests =

  //   _                    _    ______
  //  | |    ___   __ _  __| |  / / ___|  __ ___   _____
  //  | |   / _ \ / _` |/ _` | / /\___ \ / _` \ \ / / _ \
  //  | |__| (_) | (_| | (_| |/ /  ___) | (_| |\ V /  __/
  //  |_____\___/ \__,_|\__,_/_/  |____/ \__,_| \_/ \___|ed
  //
  let createRaftTest =
    let raft = RaftNew()
    let mutable cbs = new RaftCallbacks()
    let mutable data = "hfh"

    SetCallbacks(raft, NativePtr.toNativeInt &&cbs, NativePtr.toNativeInt &&data)

  [<Tests>]
  let raftTests =
    testList "Raft Tests" [
        loadSaveTest
      ]
