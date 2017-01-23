module Iris.Core.Commands

// * Imports

open System
open Iris.Core

// * Commands

type CreateProjectOptions =
  { name: string
  ; ipAddress: string
  ; gitPort: uint16
  ; webSocketPort: uint16 
  ; raftPort: uint16 }

type Command =
  | ListProjects
  | GetWebSocketPort
  | LoadProject of name:string
  | CreateProject of CreateProjectOptions

type CommandAgent = Command -> Async<Either<IrisError,string>>
