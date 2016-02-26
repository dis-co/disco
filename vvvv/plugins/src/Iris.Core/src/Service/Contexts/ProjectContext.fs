namespace Iris.Service.Contexts

open System
open Iris.Core.Types
open Iris.Core.Utils
open Iris.Service.Core
open Iris.Service.Groups

[<AutoOpen>]
module Project =
  
  type ProjectContext(project : Project) =
    let tag = "ProjectContext"

    let projectGroup = new ProjectGroup(project)
    let gitDaemon = new Git.Daemon(Option.get project.Path)

    do
      gitDaemon.Start()
      projectGroup.Join()

    interface IDisposable with
      member self.Dispose() =
        projectGroup.Leave()
        gitDaemon.Stop()
