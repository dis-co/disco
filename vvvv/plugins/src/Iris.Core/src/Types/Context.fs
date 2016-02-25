namespace Iris.Core.Types

open System
open LibGit2Sharp
open Iris.Service.Core

[<AutoOpen>]
module Context =

  type Context(signature) as self =
    [<DefaultValue>] val mutable Signature : Signature  option
    [<DefaultValue>] val mutable Project   : Project    option
    [<DefaultValue>] val mutable GitDaemon : Git.Daemon option

    do
      self.Signature <- Some(signature)
      self.Project   <- None
      self.GitDaemon <- None

    interface IDisposable with
      member self.Dispose() =
        self.StopDaemon()

    member self.StopDaemon() =
      match self.GitDaemon with
        | Some(daemon) -> daemon.Stop()
        | _ -> ()

    member self.StartDaemon() =
      match self.Project with
        | Some(project) ->
          self.StopDaemon()
          new Git.Daemon(Option.get project.Path)
          |> fun daemon ->
            daemon.Start()
            self.GitDaemon <- Some(daemon)
        | None -> printfn "project not found"

    member self.LoadProject(path : FilePath) : unit =
      self.StopDaemon()
      self.Project <- loadProject path
      self.StartDaemon()

    member self.SaveProject(msg : string) : unit =
      if Option.isSome self.Signature
      then
        let signature = Option.get self.Signature
        match self.Project with
          | Some(project) -> saveProject project signature msg
          | _ -> printfn "No project loaded."
      else printfn "Unable to save project. No signature supplied."

    member self.CreateProject(name : Name, path : FilePath) =
      self.StopDaemon()
      let project = createProject name
      project.Path <- Some(path)
      self.Project <- Some(project)
      self.SaveProject(sprintf "Created %s" name)
      self.StartDaemon()
