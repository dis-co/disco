namespace Iris.Core

open Pallet.Core
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

    static member FromFB (fb: StateMachineFB) =
      match fb.Type with
        | StateMachineTypeFB.OpenProjectTypeFB   -> Open         fb.Command
        | StateMachineTypeFB.SaveProjectTypeFB   -> Save         fb.Command
        | StateMachineTypeFB.CreateProjectTypeFB -> Create       fb.Command
        | StateMachineTypeFB.CloseProjectTypeFB  -> Close        fb.Command
        | StateMachineTypeFB.AddClientTypeFB     -> AddClient    fb.Command
        | StateMachineTypeFB.UpdateClientTypeFB  -> UpdateClient fb.Command
        | StateMachineTypeFB.RemoveClientTypeFB  -> RemoveClient fb.Command
        | StateMachineTypeFB.DataSnapshotTypeFB  -> DataSnapshot fb.Command
        | _ -> failwith "could not de-serialize garbage StateMachineTypeFB command"

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


//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/ for Raft-specific stuff

type ConfigChange = ConfigChange<IrisNode>
type Log = Log<StateMachine,IrisNode>
type LogEntry = LogEntry<StateMachine,IrisNode>
type Raft = Raft<StateMachine,IrisNode>
type AppendEntries = AppendEntries<StateMachine,IrisNode>
type VoteRequest = VoteRequest<IrisNode>
type Node = Node<IrisNode>
type InstallSnapshot = InstallSnapshot<StateMachine,IrisNode>
