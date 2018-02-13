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

module AddPreviousMemberShouldPull =

  let test =
    ftestCase "ensure previous member pulls from leader" <| fun _ ->
      result {
        use configurationDone = new WaitEvent()
        use updateDone = new WaitEvent()

        let machine1 = mkMachine 4000us
        let machine2 = mkMachine 5000us

        let mem1 = Machine.toClusterMember machine1
        let mem2 = Machine.toClusterMember machine2

        let site1 = mkSite [ mem1 ]
        let site2 = mkSite [ mem2 ]

        let! project1 = mkProject machine1 site1

        let path2 = machine2.WorkSpace </> (project1.Name |> unwrap |> filepath)

        let project2 =
          project1.Config
          |> Config.addSiteAndSetActive site2
          |> flip Project.setConfig project1
          |> Project.setPath path2

        do! FileSystem.copyDir project1.Path path2
        do! project2.Save path2

        do Logger.setFields {
          LogEventFields.Default with
            LogLevel = false
            Time = false
            Id = false
            Tier = false
        }

        use lobs = Logger.subscribe Logger.stdout

        let handler mem cmd =
          match cmd with
          | DiscoEvent.FileSystem _ -> ()
          | DiscoEvent.Append(_, LogMsg _) -> ()
          | cmd -> Logger.debug mem (string cmd)
          cmd |> function
          | DiscoEvent.ConfigurationDone members     -> configurationDone.Set()
          | DiscoEvent.Append(_, CommandBatch batch) -> updateDone.Set()
          | ev -> ()

        let! repo1 = Project.repository project1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let! service1 = DiscoService.create {
          Machine = machine1
          ProjectName = project1.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 = service1.Subscribe (handler "machine1")
        do! service1.Start()

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| start

        let! repo2 = Project.repository project2

        let! service2 = DiscoService.create {
          Machine = machine2
          ProjectName = project2.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 = service2.Subscribe (handler "machine2")
        do! service2.Start()

        ///  _____
        /// |___ /
        ///   |_ \
        ///  ___) |
        /// |____/ add member

        do service1.AddMachine (Machine.toRaftMember machine2)

        do! waitFor "configurationDone" configurationDone

        printfn "leader1: %A" service1.RaftServer.Raft.CurrentLeader
        printfn "leader2: %A" service2.RaftServer.Raft.CurrentLeader

        do! waitFor "updateDone" updateDone

        Expect.equal
          service1.State.Project.Config.Sites
          service2.State.Project.Config.Sites
          "Cluster Sites should be equal"

        dispose service1
        dispose service2
      }
      |> noError
