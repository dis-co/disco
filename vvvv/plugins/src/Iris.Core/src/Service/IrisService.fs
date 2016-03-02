namespace Iris.Service.Core

open System
open System.IO
open Iris.Core
open Iris.Core.Utils
open Iris.Core.Types
open Iris.Core.Config
open Iris.Service.Core
open Iris.Service.Groups
open LibGit2Sharp
open Vsync

[<AutoOpen>]
module IrisService =
  //   ___                 _
  //  / _ \ _ __ __ _  ___| | ___
  // | | | | '__/ _` |/ __| |/ _ \
  // | |_| | | | (_| | (__| |  __/
  //  \___/|_|  \__,_|\___|_|\___|
  //
  type Oracle =
    static member Start() =
      let options =
        { VsyncConfig.Default with
            GracefulShutdown = Some(true);
            UnicastOnly = Some(true);
            Hosts = Some([ "localhost" ]) }

      options.Apply()
      VsyncSystem.Start()

    static member Stop() =
      VsyncSystem.Shutdown()

    static member Wait() =
      VsyncSystem.WaitForever()

  //  ___      _     ____                  _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
  //
  type IrisService() =
    let tag = "IrisService"

    let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

    let mutable state : AppState ref = ref AppState.empty
    let mutable Projects : Map<Guid,ProjectController> = Map.empty

    [<DefaultValue>] val mutable Ctrl  : ControlGroup


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
      let options =
        { VsyncConfig.Default with
            GracefulShutdown = Some(true);
            UnicastOnly = Some(true);
            Hosts = Some([ "localhost" ]) }

      options.Apply()
      VsyncSystem.Start()

      self.Ctrl <- new ControlGroup(state)
      self.Ctrl.Join()


    member self.Stop() =
      self.Ctrl.Leave()
      try VsyncSystem.Shutdown()
      with
        | :? System.InvalidOperationException as exn ->
          logger tag exn.Message

    member self.Wait() = VsyncSystem.WaitForever()

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.SaveProject(id, msg) =
      saveProject id signature msg !state
        >>= fun (commit, state') -> 
          state := state'
          succeed commit

    member self.CreateProject(name, path) =
      createProject name path signature !state
        >>= fun (project, state') ->
          state := state'
          self.Ctrl.Load(project.Id, project.Name)
          succeed project

    member self.CloseProject(pid) =
      findProject pid !state
        >>= fun project ->
          combine project (closeProject pid !state)
        >>= fun (project, state') ->
          state := state'
          self.Ctrl.Close(pid, project.Name)
          succeed project

    member self.LoadProject(path : FilePath) =
      loadProject path !state
        >>= fun (project, state') -> 
          self.Ctrl.Load(project.Id, project.Name)
          state := state'
          succeed project

    member self.Dump() =
      printfn "Members:"

      !state
      |> fun s -> s.Members
      |> List.iter (fun (mem : Member) ->
                      printfn "  %s" <| mem.ToString())

      printfn "Projects:"
      !state
      |> fun s -> s.Projects
      |> Map.toList
      |> List.iter (fun (id, p : Project) ->
                      printfn "  Id: %s Name: %s" (p.Id.ToString()) p.Name)
