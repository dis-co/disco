namespace Iris.Service

open System.Diagnostics
open System

open Akka.Actor
open Akka.FSharp
open Akka.Routing

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

        deployment {
            /clients {
                router = broadcast-group
                routees.paths = [""/user/websocket/*""]
            }

            /workers {
                router = broadcast-group
                routees.paths = [""/user/w1"", ""/user/w2"", ""/user/w3""]
            }

            /remote-workers {
                router = broadcast-group
                routees.paths = [""/user/workers""]
                cluster {
                  enabled = on
                  nr-of-instances = 99
                  allow-local-routees = off
                  use-role = backend
                }
            }
        }
    }

    remote {
        log-remote-lifecycle-events = DEBUG
        log-received-messages = on

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
        roles = [ backend ]
    }
}"
           
    let cnfstr =
      tmpl
        .Replace("%localport%", localPort.ToString())
        .Replace("%remoteport%", remotePort.ToString())

    let config = Configuration.parse(cnfstr)
    let wsport = localPort + 1

    use system = System.create "iris" config

    let router = system.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "workers");
    let remote = system.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "remote-workers");

    ["w1"; "w2"; "w3"]
    |> List.map (fun name -> spawn system name <| fun mailbox ->
                 let rec loop () = actor {
                   let! msg = mailbox.Receive()
                   printfn "%s received a message: %s" name msg
                   return! loop()
                 }
                 loop())
    |> ignore

    let websockets = WebSockets.Create system wsport

    // let assetServer = new AssetServer("0.0.0.0", 3000)
    // assetServer.Start ()

    while true do
      let cmd = Console.ReadLine()
      // websockets <! WebSockets.Broadcast cmd
      router <! cmd
      remote <! cmd

    0
