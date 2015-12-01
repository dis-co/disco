namespace Iris.Service

open System.Diagnostics
open System

open Akka
open Akka.Actor
open Akka.FSharp

open Iris.Core.Types
open Iris.Service.Types

module Main =

  [<EntryPoint>]
  let main argv =

    use system = ActorSystem.Create "iris"

    let websockets = WebSockets.Create system

    // let assetServer = new AssetServer("0.0.0.0", 3000)
    // assetServer.Start ()

    while true do
      let cmd = Console.ReadLine()
      websockets <! WebSockets.Broadcast cmd

    0
