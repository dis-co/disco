module Iris.Core.Commands

// * Imports

open System
open Iris.Core
open Iris.Core.Discovery

// * Commands

type CreateProjectOptions =
  { name: string
  ; ipAddress: string
  ; gitPort: uint16
  ; apiPort: uint16
  ; webSocketPort: uint16 
  ; raftPort: uint16 }

type Command =
  | Shutdown
  | UnloadProject
  | ListProjects
  | GetWebSocketAddress
  | CreateProject of CreateProjectOptions
  | LoadProject of projectName:string * userName:string * password:string

type CommandAgent = Command -> Async<Either<IrisError,string>>
