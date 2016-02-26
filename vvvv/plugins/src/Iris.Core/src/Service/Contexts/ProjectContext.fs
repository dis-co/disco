namespace Iris.Service.Contexts

open System
open Iris.Core.Types
open Iris.Core.Utils
open Iris.Service.Core

[<AutoOpen>]
module Project =
  
  type ProjectContext(project : Project) =
    let tag = "ProjectContext"

    let gitDaemon = new Git.Daemon(Option.get project.Path)
    let mutable GitDaemon : Git.Daemon option  = None

    do logger tag "Starting"

    interface IDisposable with
      member self.Dispose() =
        gitDaemon.Stop()
