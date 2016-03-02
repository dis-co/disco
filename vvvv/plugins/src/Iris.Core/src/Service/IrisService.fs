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

    let mutable Ready : bool = false
    let mutable State : AppState = AppState.empty
    let mutable Projects : Map<Guid,ProjectController> = Map.empty

    [<DefaultValue>] val mutable Ctrl  : ControlGroup


    do Ready <- false

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

      self.Ctrl <- new ControlGroup(State)
      self.Ctrl.Join()

      Ready <- true

    member self.Stop() =
      try
        self.Ctrl.Leave()
        try VsyncSystem.Shutdown()
        with
          | :? System.InvalidOperationException as exn ->
            logger tag exn.Message
      finally
        Ready <- false

    member self.Wait() = VsyncSystem.WaitForever()

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.SaveProject(id, msg) =
      State.Save(id, signature, msg)

    member self.CreateProject(name, path) =
      match State.Create(name, path, signature) with
        | Success project -> self.Ctrl.Load(project.Id, project.Name)
        | Fail err -> logger tag err

    member self.CloseProject(pid) =
      match State.Close(pid) with
        | Success project -> self.Ctrl.Close(project.Id, project.Name)
        | Fail err -> logger tag err

    member self.LoadProject(path : FilePath) =
      match State.Load(path) with
        | Success project -> self.Ctrl.Load(project.Id, project.Name)
        | Fail err -> logger tag err

    member self.Dump() =
      printfn "Members:"
      State.Members
      |> List.iter (fun (mem : Member) ->
                      printfn "  %s" <| mem.ToString())

      printfn "Projects:"
      State.Projects
      |> Map.toList
      |> List.iter (fun (id, p : Project) ->
                      printfn "  Id: %s Name: %s" (p.Id.ToString()) p.Name)
