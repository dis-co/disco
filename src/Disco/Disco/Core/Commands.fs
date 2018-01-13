(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

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
  | CreateProject   of CreateProjectOptions
  | PullProject     of machineId:string * projectName:Name * uri:Url
  | CloneProject    of projectName:Name * uri:Url
  | LoadProject     of projectName:Name * site:NameAndId option
  | GetProjectSites of projectName:Name

type CommandAgent = Command -> Async<Either<DiscoError,string>>
