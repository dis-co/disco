namespace Iris.Tests

open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Service
open FSharpx.Functional
open fszmq
open fszmq.Context
open fszmq.Socket

[<AutoOpen>]
module RaftIntegrationTests =

  //  _   _ _   _ _ _ _   _
  // | | | | |_(_) (_) |_(_) ___  ___
  // | | | | __| | | | __| |/ _ \/ __|
  // | |_| | |_| | | | |_| |  __/\__ \
  //  \___/ \__|_|_|_|\__|_|\___||___/

  let createConfig rid idx start lid lip lport  =
    let portbase = 8000
    { RaftId     = rid
    ; Debug      = true
    ; IpAddr     = "127.0.0.1"
    ; WebPort    = (portbase - 1000) + idx
    ; RaftPort   = portbase + idx
    ; Start      = start
    ; LeaderId   = lid
    ; LeaderIp   = lip
    ; LeaderPort = lport
    ; MaxRetries = 5u
    }

  let createFollower (rid: string) (portidx: int) lid lport =
    createConfig rid portidx false (Some lid) (Some "127.0.0.1") (Some lport)

  let createLeader (rid: string) (portidx: int) =
    createConfig rid portidx true None None None

  //  ____        __ _     _____         _
  // |  _ \ __ _ / _| |_  |_   _|__  ___| |_ ___
  // | |_) / _` | |_| __|   | |/ _ \/ __| __/ __|
  // |  _ < (_| |  _| |_    | |  __/\__ \ |_\__ \
  // |_| \_\__,_|_|  \__|   |_|\___||___/\__|___/

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      use context = new Context()

      let leadercfg = createLeader "0x01" 1
      let leader = new RaftServer(leadercfg, context)

      leader.Start()

      let follower = new RaftServer(leadercfg, context)

      try
        follower.Start()
        failwith "Should have already thrown an exception due to already bound port."
      with
        | _ -> ()

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let raftIntegrationTests =
    testList "Raft Integration Tests" [
        test_validate_raft_service_bind_correct_port
      ]
