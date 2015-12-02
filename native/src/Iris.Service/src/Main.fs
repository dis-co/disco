namespace Iris.Service

open System.Diagnostics
open System

open Akka
open Akka.Actor
open Akka.FSharp
open Akka.Remote
open Akka.Routing
open Akka.Configuration

open Iris.Core.Types
open Iris.Service.Types

module Main =

  [<EntryPoint>]
  let main argv =
    let localPort  = int(Environment.GetEnvironmentVariable("LOCAL_PORT"))
    let remotePort = Environment.GetEnvironmentVariable("REMOTE_PORT")

    let tmpl = @"
akka {
    actor {
        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""

        deployment = {
            /as = {
                router = broadcast-group
                routees.paths = [""../../a1"", ""../../a2"", ""../../a3""]
                cluster = {
										enabled = on
										allow-local-routees = on
										use-role = broker
                }
            }
        }
    }

    remote {
        log-remote-lifecycle-events = DEBUG
        helios.tcp {
            port = %localport%
            hostname = localhost
        }
    }

    cluster {
        seed-nodes = [
            ""akka.tcp://iris@localhost:%localport%"",
            ""akka.tcp://iris@localhost:%remoteport%""
        ]
        roles = [ ""broker"" ]
    }
}"
           
    let cnfstr =
      tmpl
        .Replace("%localport%", localPort.ToString())
        .Replace("%remoteport%", remotePort.ToString())

    let config = ConfigurationFactory.ParseString(cnfstr)
    let wsport = localPort + 1

    use system = ActorSystem.Create("iris", config)

    let remote = system.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "as")

    ["a1"; "a2"; "a3"]
    |> List.map (fun name -> spawn system name <| fun mailbox ->
                 let rec loop () = actor {
                   let! msg = mailbox.Receive()
                   printfn "%s received a message: %s" name msg
                   return! loop()
                 }
                 loop())
    |> ignore

    // let websockets = WebSockets.Create system wsport

    // let assetServer = new AssetServer("0.0.0.0", 3000)
    // assetServer.Start ()

    while true do
      let cmd = Console.ReadLine()
      // websockets <! WebSockets.Broadcast cmd
      remote <! cmd

    0
