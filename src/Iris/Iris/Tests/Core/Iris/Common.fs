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

  let mkMachine basePort =
    { MachineConfig.create "127.0.0.1" None with
        RaftPort = port (basePort + 1us)
        ApiPort = port (basePort + 2us)
        GitPort = port (basePort + 3us)
        WsPort = port (basePort + 4us)
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

  let mkMember (machine: IrisMachine) =
    { Member.create machine.MachineId with
        Port = machine.RaftPort
        ApiPort = machine.ApiPort
        GitPort = machine.GitPort
        WsPort = machine.WsPort }

  let mkCluster (num: int) =
    either {
      let baseport = 4000us

      let machines =
        [ for n in 1 .. num do
            let port = baseport + uint16 (n * 1000)
            yield mkMachine port ]

      let members = List.map mkMember machines

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
