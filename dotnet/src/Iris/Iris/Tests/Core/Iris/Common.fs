namespace Iris.Tests

open System.Diagnostics
open System.IO
open System.Threading
open Expecto

open Iris.Core
open Iris.Service
open Iris.Client
open Iris.Client.Interfaces
open Iris.Service.Interfaces
open Iris.Raft
open Iris.Net

module Common =

  type WaitCount = { count: int ref }
    with
      static member Create() = { count = ref 0 }
      member lock.Increment() = lock.count := !(lock.count) + 1
      member lock.Decrement() = lock.count := !(lock.count) - 1
      member lock.Count with get () = !(lock.count)

  let waitFor msg (count: WaitCount) (expected: int) =
    let max = 30000L
    let timer = Stopwatch()
    timer.Start()
    while count.Count < expected && timer.ElapsedMilliseconds < max do
      Thread.Sleep(1)
    timer.Stop()
    if timer.ElapsedMilliseconds >= max then
      msg
      |> String.format "Timeout in {0}"
      |> Error.asOther "test"
      |> Either.fail
    else
      Either.nothing

  let mkMachine () =
    { MachineConfig.create "127.0.0.1" None with
        WorkSpace = tmpPath() </> Path.getRandomFileName() }

  let mkProject (machine: IrisMachine) (site: ClusterConfig) =
    either {
      let name = Path.GetRandomFileName()
      let path = machine.WorkSpace </> filepath name

      let author1 = "karsten"

      let cfg =
        machine
        |> Config.create
        |> Config.addSiteAndSetActive site
        |> Config.setLogLevel (LogLevel.Debug)

      let! project = Project.create (Project.ofFilePath path) name machine

      let updated =
        { project with
            Path = Project.ofFilePath path
            Author = Some(author1)
            Config = cfg }

      let! commit = IrisData.saveWithCommit path User.Admin.Signature updated

      return updated
    }

  let mkMember baseport (machine: IrisMachine) =
    { Member.create machine.MachineId with
        Port = port  baseport
        ApiPort = port (baseport + 1us)
        GitPort = port (baseport + 2us)
        WsPort = port (baseport + 3us) }

  let mkCluster (num: int) =
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
