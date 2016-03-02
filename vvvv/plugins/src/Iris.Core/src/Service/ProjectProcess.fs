namespace Iris.Service.Core

open System
open Iris.Core.Types
open Iris.Core.Utils
open Iris.Service.Core
open Iris.Service.Groups

[<AutoOpen>]
module ProjectProcess =
  let private tag = "ProjectProcess"

  type ProjectProcess =
    { GitDaemon    : Git.Daemon
    ; ProjectGroup : ProjectGroup
    ; PinGroup     : PinGroup
    ; CueGroup     : CueGroup
    }

    static member Create(project : Project) : Either<string,ProjectProcess> =
      try
        match (project.Path, project.CurrentBranch) with
          | (Some path, Some branch) -> 
            { GitDaemon    = new Git.Daemon(path)
            ; ProjectGroup = new ProjectGroup(project)
            ; PinGroup     = new PinGroup(project, branch.CanonicalName)
            ; CueGroup     = new CueGroup(project, branch.CanonicalName)
            } |> succeed
          | (None, Some _) -> fail "disk path not set."
          | (Some _, None) -> fail "current branch not available."
          | _              -> fail "disk path and branch not set"
      with
        | exn ->
          logger tag "Oops"
          fail exn.Message

  let startProcess (proc : ProjectProcess) : Either<string,ProjectProcess> =
    logger tag "startProcess"
    try
      proc.GitDaemon.Start()
      proc.ProjectGroup.Join()
      proc.CueGroup.Join()
      proc.PinGroup.Join()
      succeed proc
    with
      | exn ->
        logger tag "Oop"
        fail exn.Message

  let stopProcess (proc : ProjectProcess) : Either<string,ProjectProcess> =
    try
      proc.GitDaemon.Stop()
      proc.CueGroup.Leave()
      proc.PinGroup.Leave()
      proc.ProjectGroup.Leave()
      succeed proc
    with
      | exn -> fail exn.Message
