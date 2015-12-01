namespace Iris.Service

open System.Diagnostics
open System

open Akka
open Akka.Actor
open Akka.FSharp
open Akka.Remote
open Akka.Configuration

open Iris.Core.Types
open Iris.Service.Types

module Main =

  [<EntryPoint>]
  let main argv =

    let localPort  = int(Environment.GetEnvironmentVariable("LOCAL_PORT"))
    let remotePort = Environment.GetEnvironmentVariable("REMOTE_PORT")

    let tmpl = @"
akka.remote.helios.tcp {
    transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
    transport-protocol = tcp
    port = %port%
    hostname = ""127.0.0.1""
}"
           
    let config = ConfigurationFactory.ParseString(tmpl.Replace("%port%", localPort.ToString()))
    let wsport = localPort + 1

    use system = ActorSystem.Create("iris", config)
    let websockets = WebSockets.Create system wsport

    let remote = system.ActorSelection("akka.tcp://iris@localhost:"+remotePort+"/user/clients")
      
    // let assetServer = new AssetServer("0.0.0.0", 3000)
    // assetServer.Start ()

    while true do
      let cmd = Console.ReadLine()
      websockets <! WebSockets.Broadcast cmd
      remote <! WebSockets.Broadcast "remooooty"

    0
