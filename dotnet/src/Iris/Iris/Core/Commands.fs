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

type Command =
  | Shutdown
  | UnloadProject
  | ListProjects
  | GetServiceInfo
  | CreateProject of CreateProjectOptions
  | CloneProject of projectName:string * uri:string
  | LoadProject of projectName:string * username:string * password:Password * site:string option
  | GetProjectSites of projectName:string * username:string * password:string

type CommandAgent = Command -> Async<Either<IrisError,string>>
