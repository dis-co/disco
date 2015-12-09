namespace Iris.Service

open System.Diagnostics
open System

open Akka.Actor
open Akka.FSharp
open Akka.Routing

module Main =

  [<EntryPoint>]
  let main argv =
    let seedPort = Environment.GetEnvironmentVariable("SEED_PORT")

    let tmpl = @"
akka {
    actor {
        provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
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
        seed-nodes = [ ""akka.tcp://iris@localhost:%localport%"" ]
        roles = [ lighthouse ]
    }
}"
           
    let cnfstr = tmpl.Replace("%localport%", seedPort)
    
    let config = Configuration.parse(cnfstr)
    use system = System.create "iris" config

    while true do Console.ReadLine() |> ignore

    0
