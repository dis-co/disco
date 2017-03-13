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

// * GraphApi

[<RequireQualifiedAccess>]
module GraphApi =

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
      InCommands: IDiffSpread<StateMachine>
      InServer: IDiffSpread<string>
      InPort: IDiffSpread<uint16>
      InDebug: ISpread<bool>
      OutState: ISpread<State>
      OutCommands: ISpread<StateMachine>
      OutConnected: ISpread<bool>
      OutStatus: ISpread<string>
      Disposables: IDisposable list }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Status = ServiceStatus.Starting
        ApiClient = Unchecked.defaultof<IApiClient>
        Events = new ConcurrentQueue<ClientEvent>()
        Logger = null
        InCommands = null
        InServer = null
        InPort = null
        InDebug = null
        OutState = null
        OutConnected = null
        OutCommands = null
        OutStatus = null
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
    let logobs = Logger.subscribe (string >> Util.debug state)
    let me =
      // let ip =
      //   match Network.getIpAddress () with
      //   | Some ip -> IpAddress.ofIPAddress ip
      //   | None -> IPv4Address "127.0.0.1"

      { Id = Id.Create ()
        Name = "Vvvv GraphApi Client"
        Role = Role.Renderer
        Status = ServiceStatus.Starting
        IpAddress = IPv4Address "192.168.2.125"
        Port = 10001us }

    let server : IrisServer =
      // let ip =
      //   match state.InServer.[0] with
      //   | null ->  IPv4Address "127.0.0.1"
      //   | ip -> IPv4Address ip

      { Id = Id.Create ()
        Port = 10000us
        Name = "iris.exe"
        IpAddress = IPv4Address "192.168.2.108" }

    let result =
      either {
        let! client = ApiClient.create server me
        do! client.Start()
        return client
      }

    match result with
    | Right client ->
      let apiobs = client.Subscribe(enqueueEvent state)
      Util.debug state "successfully started ApiClient"
      { state with
          Initialized = true
          Status = ServiceStatus.Running
          ApiClient = client
          Disposables = [ apiobs; logobs ] }
    | Left error ->
      Util.debug state (sprintf "Error starting ApiClient: %A" error)
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
    Util.debug state "updateState"
    match state.ApiClient.State with
    | Right data ->
      state.OutState.[0] <- data
      state
    | Left error ->
      string error |> Util.error state
      { state with Status = ServiceStatus.Failed error }

  let private updateCommands (state: PluginState) (cmds: StateMachine array) =
    Util.debug state "updateCommand"
    state.OutCommands.AssignFrom cmds
    state

  // ** processInputs

  let private processInputs (state: PluginState) =
    if state.InCommands.IsChanged then
      for slice in 0 .. state.InCommands.SliceCount - 1 do
        let cmd: StateMachine = state.InCommands.[slice]
        if not (Util.isNullReference cmd) then
          match state.ApiClient.Append cmd with
          | Right () ->
            cmd
            |> string
            |> sprintf "%s successfully appended in cluster"
            |> Util.debug state
          | Left error ->
            error
            |> string
            |> Util.error state
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
        | true, msg ->
          newstate <-
            match msg with
            | ClientEvent.Registered ->
              { newstate with Status = ServiceStatus.Running }
              |> setStatus

            | ClientEvent.UnRegistered ->
              { newstate with Status = ServiceStatus.Stopped }
              |> setStatus

            | ClientEvent.Status status ->
              { newstate with Status = status }
              |> setStatus

            | ClientEvent.Update cmd ->
              stateUpdates <- stateUpdates + 1
              cmdUpdates.Add cmd
              state

            | ClientEvent.Snapshot ->
              stateUpdates <- stateUpdates + 1
              state
        | false, _ -> run <- false
      if stateUpdates > 0 || cmdUpdates.Count > 0 then
        cmdUpdates.ToArray()
        |> updateCommands newstate
        |> updateState
      else
        newstate
    else
      state

  // ** updateFrame

  let private updateFrame (state: PluginState) =
    { state with Frame = state.Frame + 1UL }

  // ** processor

  let private processor (state: PluginState) =
    state
    |> processMsgs
    |> updateFrame

  // ** evaluate

  let evaluate (state: PluginState) (_: int) =
    state
    |> initialize
    |> processor

// * IrisClientNode

[<PluginInfo(Name="Api Client", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type IrisClientNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Commands")>]
  val mutable InCommands: IDiffSpread<StateMachine>

  [<DefaultValue>]
  [<Input("Server", IsSingle = true)>]
  val mutable InServer: IDiffSpread<string>

  [<DefaultValue>]
  [<Input("Port", IsSingle = true)>]
  val mutable InPort: IDiffSpread<uint16>

  [<DefaultValue>]
  [<Input("Debug", IsSingle = true, DefaultValue = 0.0)>]
  val mutable InDebug: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Commands")>]
  val mutable OutCommands: ISpread<StateMachine>

  [<DefaultValue>]
  [<Output("State", IsSingle = true)>]
  val mutable OutState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Connected", IsSingle = true, DefaultValue = 0.0)>]
  val mutable OutConnected: ISpread<bool>

  [<DefaultValue>]
  [<Output("Status", IsSingle = true)>]
  val mutable OutStatus: ISpread<string>

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<GraphApi.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        let state' =
          { GraphApi.PluginState.Create() with
              Logger = self.Logger
              InCommands = self.InCommands
              InServer = self.InServer
              InPort = self.InPort
              InDebug = self.InDebug
              OutState = self.OutState
              OutCommands = self.OutCommands
              OutConnected = self.OutConnected
              OutStatus = self.OutStatus }
        state <- state'
        initialized <- true

      state <- GraphApi.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
