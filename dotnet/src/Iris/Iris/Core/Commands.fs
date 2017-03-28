module Iris.Core.Commands

// * Imports

open System
open Iris.Core
open Iris.Core.Discovery

// * Commands

type CreateProjectOptions =
  { name: string
  ; ipAddress: string
  ; apiPort: uint16
  ; raftPort: uint16
  ; webSocketPort: uint16 
  ; gitPort: uint16 }

type Command =
  | Shutdown
  | UnloadProject
  | ListProjects
  | GetWebSocketAddress
  | CreateProject of CreateProjectOptions
  | LoadProject of projectName:string * userName:string * password:string * site:ClusterConfig option

type CommandAgent = Command -> Async<Either<IrisError,string>>
