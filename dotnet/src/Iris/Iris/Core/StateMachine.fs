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
        | AddCue      _ -> mkAppEvent ev ApplicationEventTypeFB.AddCueFB
        | UpdateCue   _ -> mkAppEvent ev ApplicationEventTypeFB.UpdateCueFB
        | RemoveCue   _ -> mkAppEvent ev ApplicationEventTypeFB.RemoveCueFB

        | AddPatch    _ -> mkAppEvent ev ApplicationEventTypeFB.AddPatchFB
        | UpdatePatch _ -> mkAppEvent ev ApplicationEventTypeFB.UpdatePatchFB
        | RemovePatch _ -> mkAppEvent ev ApplicationEventTypeFB.RemovePatchFB

        | AddIOBox    _ -> mkAppEvent ev ApplicationEventTypeFB.AddIOBoxFB
        | UpdateIOBox _ -> mkAppEvent ev ApplicationEventTypeFB.UpdateIOBoxFB
        | RemoveIOBox _ -> mkAppEvent ev ApplicationEventTypeFB.RemoveIOBoxFB

        | AddNode     _ -> mkAppEvent ev ApplicationEventTypeFB.AddNodeFB
        | UpdateNode  _ -> mkAppEvent ev ApplicationEventTypeFB.UpdateNodeFB
        | RemoveNode  _ -> mkAppEvent ev ApplicationEventTypeFB.RemoveNodeFB

        | LogMsg      _ -> mkAppEvent ev ApplicationEventTypeFB.LogMsgFB
        | Command     _ -> mkAppEvent ev ApplicationEventTypeFB.AppCommandFB

      | DataSnapshot str ->
        let data = builder.CreateString str
        DataSnapshotFB.StartDataSnapshotFB(builder)
        DataSnapshotFB.AddData(builder, data)
        let snapshot = DataSnapshotFB.EndDataSnapshotFB(builder)

        StateMachineFB.StartStateMachineFB(builder)
        StateMachineFB.AddCommandType(builder, StateMachineTypeFB.DataSnapshotFB)
        StateMachineFB.AddCommand(builder, snapshot.Value)
        StateMachineFB.EndStateMachineFB(builder)

    member self.ToBytes () = Binary.buildBuffer self

    static member FromBytes (bytes: byte array) : StateMachine option =
      let msg = StateMachineFB.GetRootAsStateMachineFB(new ByteBuffer(bytes))
      StateMachine.FromFB(msg)
