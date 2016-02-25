namespace Iris.Core

open System
open Iris.Core
open Iris.Core.Types
open Iris.Core.Config
open Iris.Service.Core
open Iris.Service.Groups
open LibGit2Sharp

open Vsync

[<AutoOpen>]
module IrisService =

  type IrisService() as this =
    [<DefaultValue>] val mutable Ready   : bool
    [<DefaultValue>] val mutable Context : Context
    [<DefaultValue>] val mutable Ctrl    : ControlGroup

    do
      let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))
      this.Context <- new Context(signature)
      this.Ready <- false

    //  ___       _             __
    // |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  ___
    //  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \/ __|
    //  | || | | | ||  __/ |  |  _| (_| | (_|  __/\__ \
    // |___|_| |_|\__\___|_|  |_|  \__,_|\___\___||___/
    //
    interface IDisposable with
      member self.Dispose() =
        (self.Context :> IDisposable).Dispose()
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

      self.Ctrl <- new ControlGroup(self.Context)
      self.Ctrl.Join()

      self.Ready <- true

    member self.Stop() =
      try
        self.Ctrl.Leave()
        try VsyncSystem.Shutdown()
        with
          | :? System.InvalidOperationException as exn ->
            printfn "Shutdown: %s" exn.Message
      finally 
        self.Ready <- false

    member self.Wait() = VsyncSystem.WaitForever()

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.LoadProject(path : FilePath) =
      self.Context.LoadProject(path)
      match self.Context.Project with      
        | Some project -> self.Ctrl.LoadProject(project.Name)
        | _ -> printfn "no project was loaded. path correct?"

    member self.SaveProject(msg) =
      self.Context.SaveProject(msg)

    member self.CreateProject(name, path) =
      self.Context.CreateProject(name, path)

    member self.CloseProject() =
      self.Context.StopDaemon()
      self.Context.Project <- None
