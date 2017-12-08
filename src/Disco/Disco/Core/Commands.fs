module Disco.Core.Commands

// * Imports

open System
open Disco.Core

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

type NameAndId = { Name: Name; Id: DiscoId }

type Command =
  | Shutdown
  | SaveProject
  | UnloadProject
  | ListProjects
  | GetServiceInfo
  | MachineStatus
  | MachineConfig
  | CreateProject of CreateProjectOptions
  | CloneProject of projectName:Name * uri:Url
  | PullProject of machineId:string * projectName:Name * uri:Url
  | LoadProject of projectName:Name * username:UserName * password:Password * site:NameAndId option
  | GetProjectSites of projectName:Name * username:UserName * password:Password

type CommandAgent = Command -> Async<Either<DiscoError,string>>
