namespace Iris.Core

open Iris.Raft
open Iris.Serialization.Raft
open FlatBuffers

type StateMachine =
  | AppEvent     of ApplicationEvent
  | DataSnapshot of string

  with
    static member FromFB (fb: StateMachineFB) =
      match fb.CommandType with
        | StateMachineTypeFB.ApplicationEventFB ->
          let command = fb.GetCommand(new ApplicationEventFB())
          ApplicationEvent.FromFB command
          |> Option.map AppEvent

        | StateMachineTypeFB.DataSnapshotFB ->
          let entry = fb.GetCommand(new DataSnapshotFB())
          DataSnapshot entry.Data
          |> Some

        | _ -> None

    member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateMachineFB> =
      let mkAppEvent (ev: ApplicationEvent) tipe =
        let appevent = ev.ToOffset(builder)
        StateMachineFB.StartStateMachineFB(builder)
        StateMachineFB.AddCommandType(builder, StateMachineTypeFB.ApplicationEventFB)
        StateMachineFB.AddCommand(builder, appevent.Value)
        StateMachineFB.EndStateMachineFB(builder)

      match self with
      | AppEvent ev ->
        match ev with
        | AddCue _    -> mkAppEvent ev ApplicationEventTypeFB.AddCueFB
        | UpdateCue _ -> mkAppEvent ev ApplicationEventTypeFB.UpdateCueFB
        | RemoveCue _ -> mkAppEvent ev ApplicationEventTypeFB.RemoveCueFB
        | LogMsg _    -> mkAppEvent ev ApplicationEventTypeFB.LogMsgFB
        | Command _   -> mkAppEvent ev ApplicationEventTypeFB.AppCommandFB

      | DataSnapshot str ->
        let data = builder.CreateString str
        DataSnapshotFB.StartDataSnapshotFB(builder)
        DataSnapshotFB.AddData(builder, data)
        let snapshot = DataSnapshotFB.EndDataSnapshotFB(builder)

        StateMachineFB.StartStateMachineFB(builder)
        StateMachineFB.AddCommandType(builder, StateMachineTypeFB.DataSnapshotFB)
        StateMachineFB.AddCommand(builder, snapshot.Value)
        StateMachineFB.EndStateMachineFB(builder)

    member self.ToBytes () =
      let builder = new FlatBufferBuilder(1)
      let offset = self.ToOffset(builder)
      builder.Finish(offset.Value)
      builder.SizedByteArray()

    static member FromBytes (bytes: byte array) : StateMachine option =
      let msg = StateMachineFB.GetRootAsStateMachineFB(new ByteBuffer(bytes))
      StateMachine.FromFB(msg)

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/ for Raft-specific stuff

type Log = Log<StateMachine>
type LogEntry = LogEntry<StateMachine>
type Raft = Raft<StateMachine>
type AppendEntries = AppendEntries<StateMachine>
type InstallSnapshot = InstallSnapshot<StateMachine>
