(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System.IO
open System.Threading
open Expecto

open Disco.Core
open Disco.Service
open Disco.Client
open Disco.Client.Interfaces
open Disco.Service.Interfaces
open Disco.Raft
open Disco.Net

open Common

module AddedMemberShouldHaveCorrectState =

  let test =
    testCase "added member should have correct state" <| fun _ ->
      result {
        use configurationDone = new WaitEvent()
        use snapshotDone = new WaitEvent()
        use updateDone = new WaitEvent()

        let machine1 = mkMachine 4000us
        let machine2 = mkMachine 5000us

        let mem1 = Machine.toClusterMember machine1
        let mem2 = Machine.toClusterMember machine2

        let site1 = mkSite [ mem1 ]
        let site2 = mkSite [ mem2 ] |> ClusterConfig.setName (name "Ohai!")

        let! project1 = mkProject machine1 site1

        let path2 = machine2.WorkSpace </> (project1.Name |> unwrap |> filepath)

        let project2 =
          project1.Config
          |> Config.addSiteAndSetActive site2
          |> flip Project.setConfig project1
          |> Project.setPath path2

        do! FileSystem.copyDir project1.Path path2
        do! project2.Save path2

        /// do Logger.setFields {
        ///   LogEventFields.Default with
        ///     LogLevel = false
        ///     Time = false
        ///     Id = false
        ///     Tier = false
        /// }

        /// use lobs = Logger.subscribe Logger.stdout

        let handler = function
          | DiscoEvent.ConfigurationDone members          -> configurationDone.Set()
          | DiscoEvent.Append(_, DataSnapshot _)          -> snapshotDone.Set()
          | DiscoEvent.Append(_, CommandBatch batch)      -> updateDone.Set()
          | ev -> ()

        let! repo1 = Project.repository project1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        use! service1 = DiscoService.create {
          Machine = machine1
          ProjectName = project1.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 = service1.Subscribe handler
        do! service1.Start()

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| start

        let! repo2 = Project.repository project2

        use! service2 = DiscoService.create {
          Machine = machine2
          ProjectName = project2.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 = service2.Subscribe handler
        do! service2.Start()


        ///  _____
        /// |___ /
        ///   |_ \
        ///  ___) |
        /// |____/ add member

        do service1.AddMachine (Machine.toRaftMember machine2)

        do! waitFor "configurationDone" configurationDone

        do! waitFor "snapshotDone" snapshotDone
        do! waitFor "snapshotDone" snapshotDone

        do! waitFor "updateDone" updateDone
        do! waitFor "updateDone" updateDone

        // we have to wait here because under some circumstances (when LeaderChange happens, and new
        // leader socket gets created and local state is forwarded to the leader) CommandBatch gets
        // sent 3x rather than 2x causing the next expectations to fail.
        do updateDone.WaitOne(System.TimeSpan.FromSeconds 2.0) |> ignore

        Expect.equal
          service1.State.Project.Config.Sites
          service2.State.Project.Config.Sites
          "Cluster Sites should be equal"

        Expect.equal
          (service1.RaftServer.Raft.Peers |> Map.count)
          2
          "Raft peers of Service 1 Should have 2 Members"

        Expect.equal
          (service2.RaftServer.Raft.Peers |> Map.count)
          2
          "Raft peers of Service 2 Should have 2 Members"

        Expect.equal
          (service1.State.Project.Config |> Config.getActiveSite |> Option.map (ClusterConfig.members >> Map.count))
          (Some 2)
          "ActiveSite of Service 1 Should have 2 Members"

        Expect.equal
          (service2.State.Project.Config |> Config.getActiveSite |> Option.map (ClusterConfig.members >> Map.count))
          (Some 2)
          "ActiveSite of Service 2 Should also have 2 Members"
      }
      |> noError
