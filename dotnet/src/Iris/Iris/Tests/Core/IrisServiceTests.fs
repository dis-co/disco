namespace Iris.Tests

open System.IO
open System.Threading
open Expecto

open Iris.Core
open Iris.Service
open Iris.Client
open Iris.Client.Interfaces
open Iris.Service.Interfaces
open Iris.Raft
open ZeroMQ

[<AutoOpen>]
module IrisServiceTests =

  let private mkMachine () =
    { MachineConfig.create "127.0.0.1" None with
        WorkSpace = tmpPath() </> Path.getRandomFileName() }

  let private mkProject (machine: IrisMachine) (site: ClusterConfig) =
    either {
      let name = Path.GetRandomFileName()
      let path = machine.WorkSpace </> filepath name

      let author1 = "karsten"

      let cfg =
        Config.create "leader" machine
        |> Config.addSiteAndSetActive site
        |> Config.setLogLevel (LogLevel.Debug)

      let! project = Project.create (Project.ofFilePath path) name machine

      let updated =
        { project with
            Path = Project.ofFilePath path
            Author = Some(author1)
            Config = cfg }

      let! commit = Asset.saveWithCommit path User.Admin.Signature updated

      return updated
    }

  let private mkMember baseport (machine: IrisMachine) =
    { Member.create machine.MachineId with
        Port = port  baseport
        ApiPort = port (baseport + 1us)
        GitPort = port (baseport + 2us)
        WsPort = port (baseport + 3us) }

  let private mkCluster (num: int) =
    either {
      let machines = [ for n in 0 .. num - 1 -> mkMachine () ]

      let baseport = 4000us

      let members =
        List.mapi
          (fun i machine ->
            let port = baseport + uint16 (i * 1000)
            mkMember port machine)
          machines

      let site =
        { ClusterConfig.Default with
            Name = name "Cool Cluster Yo"
            Members = members |> List.map (fun mem -> mem.Id,mem) |> Map.ofList }

      let project =
        List.fold
          (fun (i, project') machine ->
            if i = 0 then
              match mkProject machine site with
              | Right project -> (i + 1, project)
              | Left error -> failwithf "unable to create project: %O" error
            else
              let path = Project.toFilePath project'.Path
              match copyDir path (machine.WorkSpace </> (project'.Name |> unwrap |> filepath)) with
              | Right () -> (i + 1, project')
              | Left error -> failwithf "error copying project: %O" error)
          (0, Unchecked.defaultof<IrisProject>)
          machines
        |> snd

      let zipped = List.zip members machines

      return (project, zipped)
    }


  //  ___      _     ____                  _            _____         _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___  |_   _|__  ___| |_ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \   | |/ _ \/ __| __/ __|
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/   | |  __/\__ \ |_\__ \
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|   |_|\___||___/\__|___/

  let test_ensure_iris_server_clones_changes_from_leader =
    ftestCase "ensure iris server clones changes from leader" <| fun _ ->
      either {
        use lobs = Logger.subscribe Logger.stdout

        use ctx = new ZContext()

        use checkGitStarted = new AutoResetEvent(false)
        use electionDone = new AutoResetEvent(false)
        use appendDone = new AutoResetEvent(false)
        use pullDone = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 2

        let! repo1 = Project.repository project

        let num1 = Git.Repo.commitCount repo1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let mem1, machine1 = List.head zipped

        let service1 = IrisService.create ctx {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (fun ev ->
            printfn "ev: %A" ev
            match ev with
            | IrisEvent.Git (GitEvent.Started _)    -> checkGitStarted.Set() |> ignore
            | IrisEvent.Git (GitEvent.Pull _)       -> pullDone.Set() |> ignore
            | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set() |> ignore
            | IrisEvent.Append(Origin.Raft, _)      -> appendDone.Set() |> ignore
            | _                                     -> ())
          |> service1.Subscribe

        do! service1.Start()

        printfn "waiting: git started"

        do! waitOrDie "checkGitStarted" checkGitStarted

        printfn "waiting: git started DONE"

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| start

        let mem2, machine2 = List.last zipped

        let! repo2 = Project.repository {
          project with
            Path = machine2.WorkSpace </> (project.Name |> unwrap |> filepath)
        }

        let num2 = Git.Repo.commitCount repo2

        let! service2 = Dispatcher.create ctx {
          Machine = machine2
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 =
          (function
            | IrisEvent.Git (GitEvent.Started _)    -> checkGitStarted.Set() |> ignore
            | IrisEvent.Git (GitEvent.Pull _)       -> pullDone.Set() |> ignore
            | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set() |> ignore
            | IrisEvent.Append(Origin.Raft, _)      -> appendDone.Set() |> ignore
            | _                                     -> ())
          |> service2.Subscribe

        do! service2.Start()

        do! waitOrDie "checkGitStarted" checkGitStarted

        do! waitOrDie "electionDone" electionDone

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ do some work

        let raft1 = service1.RaftServer
        let raft2 = service2.RaftServer

        let leader =
          match raft1.IsLeader, raft2.IsLeader with
          | true, false  -> raft1
          | false, true  -> raft2
          | false, false -> failwith "no leader is bad news"
          | true, true   -> failwith "two leaders is really bad news"

        mkCue()
        |> AddCue
        |> leader.Append

        do! waitOrDie "appendDone" appendDone
        appendDone.Reset() |> ignore
        do! waitOrDie "appendDone" appendDone

        AppCommand.SaveProject
        |> Command
        |> leader.Append

        do! waitOrDie "appendDone" appendDone
        appendDone.Reset() |> ignore
        do! waitOrDie "appendDone" appendDone

        do! waitOrDie "pullDone" pullDone

        dispose service1
        dispose service2

        expect "Instance 1 should have same commit count" (num1 + 1) Git.Repo.commitCount repo1
        expect "Instance 2 should have same commit count" (num2 + 1) Git.Repo.commitCount repo2
      }
      |> noError

  let test_ensure_cue_resolver_works =
    testCase "ensure cue resolver works" <| fun _ ->
      either {
        use ctx = new ZContext()
        use checkGitStarted = new AutoResetEvent(false)
        use electionDone = new AutoResetEvent(false)
        use appendDone = new AutoResetEvent(false)
        use clientRegistered = new AutoResetEvent(false)
        use clientAppendDone = new AutoResetEvent(false)
        use updateDone = new AutoResetEvent(false)
        use pullDone = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let mem1, machine1 = List.head zipped

        use service1 = IrisService.create ctx {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (function
            | IrisEvent.Git (GitEvent.Started _)    -> checkGitStarted.Set() |> ignore
            | IrisEvent.Git (GitEvent.Pull _)       -> pullDone.Set() |> ignore
            | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set() |> ignore
            | IrisEvent.Append(Origin.Raft, _)      -> appendDone.Set() |> ignore
            | _                                     -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitOrDie "checkGitStarted" checkGitStarted
        do! waitOrDie "electionDone" electionDone

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ create an API client

        let server:IrisServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddr
        }

        use client = ApiClient.create ctx server {
          Id = Id.Create()
          Name = "hi"
          Role = Role.Renderer
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        let handleClient = function
          | ClientEvent.Registered              -> clientRegistered.Set() |> ignore
          | ClientEvent.Update (AddCue _)       -> clientAppendDone.Set() |> ignore
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Set() |> ignore
          | ClientEvent.Update (UpdateSlices _) -> updateDone.Set() |> ignore
          | _ -> ()

        use clobs = client.Subscribe (handleClient)

        do! client.Start()

        do! waitOrDie "clientRegistered" clientRegistered

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| do some work

        let pinId = Id.Create()
        let groupId = Id.Create()

        let pin = BoolPin {
          Id        = pinId
          Name      = "hi"
          PinGroup  = groupId
          Tags      = Array.empty
          Direction = ConnectionDirection.Output
          IsTrigger = false
          VecSize   = VecSize.Dynamic
          Labels    = Array.empty
          Values    = [| true |]
        }

        let group = {
          Id = groupId
          Name = name "whatevva"
          Client = Id.Create()
          Pins = Map.ofList [(pin.Id, pin)]
        }

        client.AddPinGroup group

        do! waitOrDie "appendDone" appendDone
        do! waitOrDie "clientAppendDone" clientAppendDone

        let cue = {
          Id = Id.Create()
          Name = name "hi"
          Slices = [| BoolSlices(pin.Id, [| false |]) |]
        }

        cue
        |> CallCue
        |> service1.Append

        do! waitOrDie "appendDone" appendDone
        do! waitOrDie "updateDone" updateDone

        let actual: Slices =
          client.State.PinGroups
          |> Map.find groupId
          |> fun group -> Map.find pinId group.Pins
          |> fun pin -> pin.Values

       expect "should be equal" cue.Slices.[0] id actual
      }
      |> noError

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let irisServiceTests =
    testList "IrisService Tests" [
      test_ensure_iris_server_clones_changes_from_leader
      test_ensure_cue_resolver_works
    ] |> testSequenced
