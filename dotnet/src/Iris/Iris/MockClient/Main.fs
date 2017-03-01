namespace Iris.MockClient

open Argu
open Iris.Core
open System
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Interfaces

[<AutoOpen>]
module Main =

  [<EntryPoint>]
  let main args =
    printfn "hi: %d" (Array.length args)

    exit 0
