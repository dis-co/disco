namespace Iris.Core

open Iris.Serialization.Raft
open FlatBuffers

type StateMachine =
  | Open         of string
  | Save         of string
  | Create       of string
  | Close        of string
  | AddClient    of string 
  | UpdateClient of string 
  | RemoveClient of string 
  | DataSnapshot of string 

  with

    member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateMachineFB> =
      match self with
      | Open str ->
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.OpenProjectTypeFB,
          builder.CreateString str)

      | Save str ->
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.SaveProjectTypeFB,
          builder.CreateString str)

      | Create str -> 
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.CreateProjectTypeFB,
          builder.CreateString str)

      | Close        str -> 
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.CloseProjectTypeFB,
          builder.CreateString str)

      | AddClient    str -> 
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.AddClientTypeFB,
          builder.CreateString str)

      | UpdateClient str -> 
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.UpdateClientTypeFB,
          builder.CreateString str)

      | RemoveClient str -> 
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.RemoveClientTypeFB,
          builder.CreateString str)

      | DataSnapshot str -> 
        StateMachineFB.CreateStateMachineFB(
          builder,
          StateMachineTypeFB.DataSnapshotTypeFB,
          builder.CreateString str)

