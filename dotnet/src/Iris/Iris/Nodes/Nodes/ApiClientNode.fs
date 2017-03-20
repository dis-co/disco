namespace VVVV.Nodes

// * Imports

open System
open System.ComponentModel.Composition
open System.Collections.Generic
open System.Collections.Concurrent
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Client
open Iris.Nodes

// * Api

[<RequireQualifiedAccess>]
module Api =

  // ** tag

  let private tag (str: string) = sprintf "Iris.%s" str

  // ** PluginState

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Status: ServiceStatus
      ApiClient: IApiClient
      Events: ConcurrentQueue<ClientEvent>
      Logger: ILogger
      InCommands: ISpread<StateMachine>
      InServerIp: ISpread<string>
      InServerPort: ISpread<int>
      InClientId: ISpread<Id>
      InClientName: ISpread<string>
      InClientIp: ISpread<string>
      InClientPort: ISpread<int>
      InPinGroups: ISpread<PinGroup>
      InReconnect: ISpread<bool>
      InUpdate: ISpread<bool>
      OutState: ISpread<State>
      OutCommands: ISpread<StateMachine>
      OutConnected: ISpread<bool>
      OutStatus: ISpread<string>
      OutUpdate: ISpread<bool>
      Disposables: IDisposable list }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Status = ServiceStatus.Starting
        ApiClient = Unchecked.defaultof<IApiClient>
        Events = new ConcurrentQueue<ClientEvent>()
        Logger = null
        InCommands = null
        InServerIp = null
        InServerPort = null
        InClientId = null
        InClientName = null
        InClientIp = null
        InClientPort = null
        InPinGroups = null
        InReconnect = null
        InUpdate = null
        OutState = null
        OutConnected = null
        OutCommands = null
        OutStatus = null
        OutUpdate = null
        Disposables = List.empty }

    interface IDisposable with
      member self.Dispose() =
        try
          List.iter dispose self.Disposables // first dispose the logger to prevent the logger from
          dispose self.ApiClient             // causing a VVVV crash. Then dispose the rest..
        with
          | _ -> ()

  // ** setStatus

  let setStatus (state: PluginState) =
    state.OutStatus.[0] <- string state.Status
    match state.Status with
    | ServiceStatus.Running ->
      state.OutConnected.[0] <- true
    | _ ->
      state.OutConnected.[0] <- false
    state

  // ** enqueueEvent

  let private enqueueEvent (state: PluginState) (ev: ClientEvent) =
      state.Events.Enqueue ev

  // ** startClient

  let private startClient (state: PluginState) =
    let myself =
      let ip =
        match IpAddress.TryParse state.InClientIp.[0] with
        | Right ip -> ip
        | Left error ->
          error
          |> string
          |> Logger.err state.InClientId.[0] "startClient"
          IPv4Address "127.0.0.1"

      let name =
        match state.InClientName.[0] with
        | null | "" -> "VVVV Client"
        | str -> str

      let port =
        try
          uint16 state.InClientPort.[0]
        with
          | _ -> Constants.DEFAULT_API_CLIENT_PORT

      { Id = state.InClientId.[0]
        Name = name
        Role = Role.Renderer
        Status = ServiceStatus.Starting
        IpAddress = ip
        Port = port }

    let server : IrisServer =
      let ip =
        match IpAddress.TryParse state.InServerIp.[0] with
        | Right ip ->  ip
        | Left error ->
          error
          |> string
          |> Logger.err state.InClientId.[0] "startClient"
          IPv4Address "127.0.0.1"

      let port =
        try
          uint16 state.InServerPort.[0]
        with
          | _ -> Constants.DEFAULT_API_PORT

      { Port = port; IpAddress = ip }

    let result =
      either {
        let! client = ApiClient.create server myself
        do! client.Start()
        return client
      }

    match result with
    | Right client ->
      let apiobs = client.Subscribe(enqueueEvent state)
      Logger.info state.InClientId.[0] "startClient" "successfully started ApiClient"
      { state with
          Initialized = true
          Status = ServiceStatus.Running
          ApiClient = client
          Disposables = [ apiobs ] }
    | Left error ->
      error
      |> string
      |> Logger.err state.InClientId.[0] "startClient"
      { state with
          Initialized = true
          Status = ServiceStatus.Failed error }
    |> setStatus

  // ** initialize

  let private initialize (state: PluginState) =
    if not state.Initialized then
      state
      |> startClient
    else
      state

  // ** updateState

  let private updateState (state: PluginState) =
    Logger.debug state.InClientId.[0] "updateState" "updating state output pins with new state"
    match state.ApiClient.State with
    | Right data ->
      state.OutState.[0] <- data
      state
    | Left error ->
      error
      |> string
      |> Logger.err state.InClientId.[0] "updateState"
      { state with Status = ServiceStatus.Failed error }

  let private updateCommands (state: PluginState) (cmds: StateMachine array) =
    Logger.debug state.InClientId.[0] "updateCommands" "update command output pins"
    state.OutCommands.AssignFrom cmds
    state

  // ** mergeGraphState

  let private mergeGraphState (plugstate: PluginState) =
    let local =
      Seq.fold
        (fun m (grp: PinGroup) ->
          if not (Util.isNullReference grp) then
            Map.add grp.Id grp m
          else m)
        Map.empty
        plugstate.InPinGroups

    match plugstate.ApiClient.State with
    | Right appstate ->
      if local <> appstate.PinGroups then
        let commands: StateMachine list =
          Map.fold
            (fun lst (id: Id) (lgrp: PinGroup) ->
              match Map.tryFind id appstate.PinGroups with
              | Some grp ->
                if lgrp <> grp then
                  (UpdatePinGroup lgrp) :: lst
                else
                  lst
              | None -> (AddPinGroup lgrp) :: lst)
            []
            local
        for cmd in commands do
          match plugstate.ApiClient.Append cmd with
          | Right () -> ()
          | Left error ->
            error
            |> string
            |> Logger.err plugstate.InClientId.[0] "mergeGraphState"
      plugstate
    | Left error ->
      error
      |> string
      |> Logger.err plugstate.InClientId.[0] "mergeGraphState"
      plugstate

  // ** processInputs

  let private processInputs (state: PluginState) =
    if state.InUpdate.[0] then
      for slice in 0 .. state.InCommands.SliceCount - 1 do
        let cmd: StateMachine = state.InCommands.[slice]
        if not (Util.isNullReference cmd) then
          match state.ApiClient.Append cmd with
          | Right () ->
            cmd
            |> string
            |> sprintf "%s successfully appended in cluster"
            |> Logger.debug state.InClientId.[0] "processInputs"
          | Left error ->
            error
            |> string
            |> Logger.err state.InClientId.[0] "processInputs"
      state
    else
      state

  // ** processMsgs

  let private processMsgs (state: PluginState) =
    if state.Events.Count > 0 then
      let mutable run = true
      let mutable newstate = state
      let mutable stateUpdates = 0
      let mutable cmdUpdates = new ResizeArray<StateMachine>()
      while run do
        match state.Events.TryDequeue() with
        | true, ClientEvent.Registered ->
          newstate <- { newstate with Status = ServiceStatus.Running } |> setStatus
        | true, ClientEvent.UnRegistered ->
          newstate <- { newstate with Status = ServiceStatus.Stopped } |> setStatus
        | true, ClientEvent.Status status ->
          newstate <- { newstate with Status = status } |> setStatus
        | true, ClientEvent.Update cmd ->
          stateUpdates <- stateUpdates + 1
          cmdUpdates.Add cmd
          newstate <- state
        | true, ClientEvent.Snapshot ->
          stateUpdates <- stateUpdates + 1
          newstate <- mergeGraphState state
        | false, _ -> run <- false
      if stateUpdates > 0 || cmdUpdates.Count > 0 then
        state.OutUpdate.[0] <- true
        cmdUpdates.ToArray()
        |> updateCommands newstate
        |> updateState
      else
        state.OutUpdate.[0] <- false
        newstate
    else
      state.OutUpdate.[0] <- false
      state

  // ** updateFrame

  let private updateFrame (state: PluginState) =
    { state with Frame = state.Frame + 1UL }

  // ** processor

  let private processor (state: PluginState) =
    state
    |> processInputs
    |> processMsgs
    |> updateFrame

  // ** evaluate

  let evaluate (state: PluginState) (_: int) =
    state
    |> initialize
    |> processor

// * ApiClientNode

[<PluginInfo(Name="Api Client", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ApiClientNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Commands")>]
  val mutable InCommands: ISpread<StateMachine>

  [<DefaultValue>]
  [<Input("PinGroups")>]
  val mutable InPinGroups: ISpread<PinGroup>

  [<DefaultValue>]
  [<Input("Server IP", IsSingle = true)>]
  val mutable InServerIp: ISpread<string>

  [<DefaultValue>]
  [<Input("Server Port", IsSingle = true)>]
  val mutable InServerPort: ISpread<int>

  [<DefaultValue>]
  [<Input("Client Name", IsSingle = true)>]
  val mutable InClientName: ISpread<string>

  [<DefaultValue>]
  [<Input("Client ID", IsSingle = true)>]
  val mutable InClientId: ISpread<Id>

  [<DefaultValue>]
  [<Input("Client IP", IsSingle = true)>]
  val mutable InClientIp: ISpread<string>

  [<DefaultValue>]
  [<Input("Client Port", IsSingle = true)>]
  val mutable InClientPort: ISpread<int>

  [<DefaultValue>]
  [<Input("Reconnect", IsSingle = true, IsBang = true)>]
  val mutable InReconnect: ISpread<bool>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Commands")>]
  val mutable OutCommands: ISpread<StateMachine>

  [<DefaultValue>]
  [<Output("State", IsSingle = true)>]
  val mutable OutState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Output("Status", IsSingle = true)>]
  val mutable OutStatus: ISpread<string>

  [<DefaultValue>]
  [<Output("Connected", IsSingle = true, DefaultValue = 0.0)>]
  val mutable OutConnected: ISpread<bool>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<Api.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        state <-
          { Api.PluginState.Create() with
              Logger = self.Logger
              InCommands = self.InCommands
              InServerIp = self.InServerIp
              InServerPort = self.InServerPort
              InClientId = self.InClientId
              InClientName = self.InClientName
              InClientIp = self.InClientIp
              InClientPort = self.InClientPort
              InPinGroups = self.InPinGroups
              InReconnect = self.InReconnect
              InUpdate = self.InUpdate
              OutState = self.OutState
              OutCommands = self.OutCommands
              OutConnected = self.OutConnected
              OutStatus = self.OutStatus
              OutUpdate = self.OutUpdate }
        initialized <- true

      state <- Api.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
