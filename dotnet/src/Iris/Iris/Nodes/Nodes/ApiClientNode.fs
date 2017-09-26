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
open Iris.Net

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
      Commands: ResizeArray<StateMachine>
      Events: ConcurrentQueue<ClientEvent>
      Logger: ILogger
      InCommands: ISpread<StateMachine>
      InServerIp: ISpread<string>
      InServerPort: ISpread<int>
      InClientId: ISpread<IrisId>
      InClientName: ISpread<string>
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
        Commands = ResizeArray()
        Logger = null
        InCommands = null
        InServerIp = null
        InServerPort = null
        InClientId = null
        InClientName = null
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

  // ** serverInfo

  let private serverInfo (state: PluginState) =
    let ip =
      match IpAddress.TryParse state.InServerIp.[0] with
      | Right ip ->  ip
      | Left error ->
        error
        |> string
        |> Logger.err "startClient"
        IPv4Address "127.0.0.1"

    let port =
      try
        state.InServerPort.[0] |> uint16 |> port
      with
        | _ -> port Constants.DEFAULT_API_PORT

    { Port = port; IpAddress = ip }

  // ** startClient

  let private startClient (state: PluginState) =
    let myself =
      let clientName =
        match state.InClientName.[0] with
        | null | "" -> "VVVV Client"
        | str -> str

      { Id = state.InClientId.[0]
        Name = name clientName
        Role = Role.Renderer
        ServiceId = IrisId.Create()
        Status = ServiceStatus.Starting
        IpAddress = IpAddress.Localhost
        Port = port 0us }

    let server = serverInfo state
    let client = ApiClient.create server myself

    match client.Start() with
    | Right () ->
      let apiobs = client.Subscribe(enqueueEvent state)
      Logger.info "startClient" "successfully started ApiClient"
      { state with
          Initialized = true
          Status = ServiceStatus.Running
          ApiClient = client
          Disposables = [ apiobs ] }
    | Left error ->
      error
      |> string
      |> Logger.err "startClient"
      { state with
          Initialized = true
          Status = ServiceStatus.Failed error }
    |> setStatus

  // ** initialize

  let private initialize (state: PluginState) =
    if not state.Initialized then
      startClient state
    else
      state

  // ** updateState

  let private updateState (state: PluginState) =
    Logger.debug "updateState" "updating state output pins with new state"
    state.OutState.[0] <- state.ApiClient.State
    state

  // ** updateCommands

  let private updateCommands (state: PluginState) =
    Logger.debug "updateCommands" "update command output pins"
    state.OutCommands.SliceCount <- state.Commands.Count
    state.OutCommands.AssignFrom state.Commands

  // ** mergeGraphState

  let private mergeGraphState (plugstate: PluginState) =
    let remoteState = plugstate.ApiClient.State.PinGroups

    for local in plugstate.InPinGroups do
      if not (Util.isNullReference local) then
        match PinGroupMap.tryFindGroup local.ClientId local.Id remoteState with
        | Some remote when local <> remote ->
          match local, remote with
          | { Id = _; ClientId = _; RefersTo = _; Path = lpath; Name = lname; Pins = lpins },
            { Id = _; ClientId = _; RefersTo = _; Path = rpath; Name = rname; Pins = rpins } ->

            /// basecase: update local graph with remote pin states
            rpins
            |> Map.toList
            |> List.map (snd >> Pin.slices)
            |> UpdateSlices.ofList
            |> plugstate.Commands.Add

            /// pins that are exposed locally, which need to be added to the remote state
            let newPins =
              Map.filter (fun pinId _ -> not (Map.containsKey pinId rpins)) lpins

            /// take an update function, and a getter (lenses, anyone?) and check if the
            /// two pins differ for that field. if they do, apply updater to the gotten value
            /// and return the new, updated pin
            let updateWith (updater: 'a -> Pin -> Pin) (get: Pin -> 'a) l r =
              let lval = get l
              let rval = get r
              if lval <> rval
              then updater lval r
              else r

            /// apply a series of updates to the remote pin
            let mergeAndUpdate (lpin: Pin) (rpin: Pin) =
              rpin
              |> updateWith Pin.setName    Pin.name    lpin
              |> updateWith Pin.setTags    Pin.tags    lpin
              |> updateWith Pin.setVecSize Pin.vecSize lpin
              |> Pin.setOnline true

            if lpath <> rpath || lname <> rname then
              let pins =
                /// update the remote pins to be online and with the names parsed
                /// from the graph (they might have changed in the mean time)
                let updatedRemote =
                  Map.map
                    (fun rpinId rpin ->
                      match Map.tryFind rpinId lpins with
                      | Some lpin -> mergeAndUpdate lpin rpin
                      | None -> rpin)
                    rpins
                /// now add all new pins to the ones that have been marked online and updater
                Map.fold (fun m pinId pin -> Map.add pinId pin m) updatedRemote newPins

              /// update the group with the new and updated pins
              { remote with
                  Name = lname
                  Path = lpath
                  Pins = pins }
              |> UpdatePinGroup
              |> plugstate.ApiClient.Append
            else
              /// the list of additions
              let additions = newPins |> Map.toList |> List.map (snd >> AddPin)
              /// and processing the list of known, offline pins to become online
              rpins
              |> Map.toList
              |> List.choose
                (fun (pinId,rpin) ->
                  if Map.containsKey pinId lpins
                  then rpin |> Pin.setOnline true |> UpdatePin |> Some
                  else None)
              |> List.append additions
              |> (StateMachineBatch >> CommandBatch)
              |> plugstate.ApiClient.Append

        | Some _ -> ()                   /// no need to do anything apparently
        | None ->                        /// remote does not yet have this patch
          local
          |> AddPinGroup
          |> plugstate.ApiClient.Append
    plugstate

  // ** processInputs

  let private processInputs (state: PluginState) =
    if state.InReconnect.[0] then
      while state.Events.TryDequeue() |> fst do
        ignore "purging event"
      state
      |> serverInfo
      |> state.ApiClient.Restart
      |> ignore
      state
    elif state.InUpdate.[0] && state.Initialized then
      for slice in 0 .. state.InCommands.SliceCount - 1 do
        let cmd: StateMachine = state.InCommands.[slice]
        if not (Util.isNullReference cmd) then
          state.ApiClient.Append cmd
      state
    else
      state

  // ** processMsgs

  let private processMsgs (state: PluginState) =
    if state.Events.Count > 0 then
      let mutable run = true
      let mutable newstate = state
      let mutable stateUpdates = 0
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
          state.Commands.Add cmd
          newstate <- state
        | true, ClientEvent.Snapshot ->
          Logger.err "ClientEvent.Snapshot" "event received"
          stateUpdates <- stateUpdates + 1
          newstate <- mergeGraphState state
        | false, _ -> run <- false

      /// assign all StateMachine commands to the output
      do updateCommands newstate

      /// signal the need for downstream nodes to update themselves
      if stateUpdates > 0 || state.Commands.Count > 0 then
        state.OutUpdate.[0] <- true
        state.Commands.Clear()          /// clear out the commands array now
        updateState newstate
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
  val mutable InClientId: ISpread<IrisId>

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
  [<Output("Count", IsSingle = true)>]
  val mutable OutCount: ISpread<int>

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
