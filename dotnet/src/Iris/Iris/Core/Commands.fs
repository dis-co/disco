module Iris.Core.Commands

// * Imports

open System
open Iris.Core

// * Commands

type ProjectOptions =
  { name: string
    activeSite: string
    ipAddr: string
    port: uint16
    apiPort: uint16
    wsPort: uint16
    gitPort: uint16 }

type ServiceInfo =
  { webSocket: string
    version: string
    buildNumber: string }

type NameAndId(name: string, id: Id) =
    member val Name = name
    member val Id = id

type Command =
  | Shutdown
  | UnloadProject
  | ListProjects
  | GetServiceInfo
  | MachineStatus
  | CreateProject of ProjectOptions
  | CloneProject of projectName:string * uri:string
  | PullProject of projectId:string * projectName:string * uri:string
  | LoadProject of projectName:string * username:string * password:Password * options:ProjectOptions option
  | GetProjectSites of projectName:string * username:string * password:string

type CommandAgent = Command -> Async<Either<IrisError,string>>
