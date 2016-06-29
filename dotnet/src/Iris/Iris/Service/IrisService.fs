namespace Iris.Service.Core

open System
open System.IO
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Core
open LibGit2Sharp

[<AutoOpen>]
module IrisService =
  //  ___      _     ____                  _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
  //
  type IrisService() =
    let tag = "IrisService"

    let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

    let mutable state     : AppState ref = ref AppState.Empty
    let mutable processes : Map<Guid,string> = Map.empty

    let addProcess guid proc =
      processes <- Map.add guid proc processes

    let startProcess proc =
      printfn "bla"

    let stopProcess guid proc =
      printfn "stopping"

    let removeProcess guid =
      match Map.tryFind guid processes with
        | Some proc ->
          processes <- Map.remove guid processes
          Either.succeed proc
        | _ ->
          guid.ToString()
          |> sprintf "process not found for guid %s"
          |> Either.fail

    //  ___       _             __
    // |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  ___
    //  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \/ __|
    //  | || | | | ||  __/ |  |  _| (_| | (_|  __/\__ \
    // |___|_| |_|\__\___|_|  |_|  \__,_|\___\___||___/
    //
    interface IDisposable with
      member self.Dispose() =
        self.Stop()

    //  _     _  __       ____           _
    // | |   (_)/ _| ___ / ___|   _  ___| | ___
    // | |   | | |_ / _ \ |  | | | |/ __| |/ _ \
    // | |___| |  _|  __/ |__| |_| | (__| |  __/
    // |_____|_|_|  \___|\____\__, |\___|_|\___|
    //                        |___/
    member self.Start() =
      let options = RaftConfig.Default
        // { VsyncConfig.Default with
        //     GracefulShutdown = Some(true);
        //     UnicastOnly = Some(true);
        //     Hosts = Some([ "localhost" ]) }

      printfn "hi"


    member self.Stop() =
      Map.toList processes
      |> List.iter (snd >> stopProcess >> ignore)


    member self.Wait() = ()

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.SaveProject(id, msg) =
      match saveProject id signature msg !state with
        | Success (commit, newstate) ->
          state := newstate
          Either.succeed commit
        | Fail err -> Either.fail err

    //   ____                _
    //  / ___|_ __ ___  __ _| |_ ___
    // | |   | '__/ _ \/ _` | __/ _ \
    // | |___| | |  __/ (_| | ||  __/
    //  \____|_|  \___|\__,_|\__\___|
    member self.CreateProject(name, path) =
      // createProject name path signature !state
      //   >>= fun (project, state') ->
      //     // add and start the process groups for this project
      //     let result =
      //       project.Name
      //         >>= startProcess
      //         >>= (addProcess project.Id >> succeed)

      //     match result with
      //       | Success _ -> state := state'                          // replace current state with new project state
      //                      self.Ctrl.Load(project.Id, project.Name) // notify everybody in the cluster that we loaded this project
      //                      succeed project
      //       | Fail err  -> logger tag err
      //                      fail err
      printfn "hm"

    //   ____ _
    //  / ___| | ___  ___  ___
    // | |   | |/ _ \/ __|/ _ \
    // | |___| | (_) \__ \  __/
    //  \____|_|\___/|___/\___|
    member self.CloseProject(pid) =
      // findProject pid !state
      //   >>= fun project ->
      //     combine project (closeProject pid !state)
      //   >>= fun (project, state') ->
      //     // remove and stop process groups
      //     let result = removeProcess project.Id >>= stopProcess
      //     match result with
      //       | Success _ -> state := state'                    // save global state
      //                      self.Ctrl.Close(pid, project.Name) // notify everybody
      //                      succeed project
      //       | Fail err  -> logger tag err
      //                      fail err

      printfn "fm"

    //  _                    _
    // | |    ___   __ _  __| |
    // | |   / _ \ / _` |/ _` |
    // | |__| (_) | (_| | (_| |
    // |_____\___/ \__,_|\__,_|
    member self.LoadProject(path : FilePath) =
      // loadProject path !state
      //   >>= fun (project, state') ->
      //     // add and start the process groups for this project
      //     let result =
      //       ProjectProcess.Create project
      //         >>= startProcess
      //         >>= (addProcess project.Id >> succeed)

      //     match result with
      //       | Success _ -> self.Ctrl.Load(project.Id, project.Name)
      //                      state := state'
      //                      succeed project
      //       | Fail err  -> logger tag err
      //                      fail err

      printfn "oh"

    member self.Dump() =
      // printfn "Members:"
      // !state
      // |> fun s -> s.Members
      // |> List.iter (fun (mem : Member) ->
      //                 printfn "  %s" <| mem.ToString())

      // printfn "Projects:"
      // !state
      // |> fun s -> s.Projects
      // |> Map.toList
      // |> List.iter (fun (id, p : Project) ->
      //                 printfn "  Id: %s Name: %s" (p.Id.ToString()) p.Name)

      printfn "hm"
