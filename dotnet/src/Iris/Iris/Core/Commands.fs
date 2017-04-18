module Iris.Core.Commands

// * Imports

open System
open Iris.Core
open Iris.Core.Discovery

// * Commands

type CreateProjectOptions =
  { name: string
    ipAddress: string
    apiPort: uint16
    raftPort: uint16
    webSocketPort: uint16
    gitPort: uint16 }

type ServiceInfo =
  { webSocket: string
    version: string
    buildNumber: string }

type Command =
  | Shutdown
  | UnloadProject
  | ListProjects
  | GetServiceInfo
  | CreateProject of CreateProjectOptions
  | CloneProject of projectName:string * uri:string
  | LoadProject of projectName:string * username:string * password:string * site:string option
  | GetProjectSites of projectName:string * username:string * password:string

type CommandAgent = Command -> Async<Either<IrisError,string>>
