module Iris.Core.Commands

// * Imports

open System
open Iris.Core

// * Commands

type CreateProjectOptions =
  { name: string
    ipAddr: string
    port: uint16
    apiPort: uint16
    wsPort: uint16
    gitPort: uint16 }

type ServiceInfo =
  { webSocket: string
    version: string
    buildNumber: string }

type NameAndId = { Name: Name; Id: Id }

type Command =
  | Shutdown
  | UnloadProject
  | ListProjects
  | GetServiceInfo
  | MachineStatus
  | MachineConfig
  | CreateProject of CreateProjectOptions
  | CloneProject of projectName:Name * uri:string
  | PullProject of projectId:string * projectName:Name * uri:string
  | LoadProject of projectName:Name * username:UserName * password:Password * site:Name option
  | GetProjectSites of projectName:Name * username:UserName * password:Password

type CommandAgent = Command -> Async<Either<IrisError,string>>
