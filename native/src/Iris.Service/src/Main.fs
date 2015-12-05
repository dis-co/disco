namespace Iris.Service

open System.Diagnostics
open System

open Akka.Actor
open Akka.FSharp
open Akka.Routing

open Iris.Core.Types
open Iris.Service.Types
open Iris.Service.Serialization

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

            /remotes {
                router = broadcast-group
                routees.paths = [""/user/clients""]
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

    let serializer = IrisSerializer (system  :?> ExtendedActorSystem)
    system.Serialization.AddSerializer(serializer)
    system.Serialization.AddSerializationMap(typeof<WsMsg>, serializer)

    let router = Routes.GetRouter system "clients"
    let remote = Routes.GetRouter system "remotes"

    let websockets = WebSockets.Create system wsport

    while true do
      let cmd = Console.ReadLine()
      // websockets <! WebSockets.Broadcast cmd
      router <! WebSockets.Broadcast cmd
      remote <! WebSockets.Broadcast cmd

    0
