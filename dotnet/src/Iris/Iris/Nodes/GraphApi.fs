namespace Iris.Nodes

// * Imports

open System
open System.Collections.Concurrent
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Core.Logging
open Iris.Core
open Iris.Client

// * GraphApi

[<RequireQualifiedAccess>]
module GraphApi =

  // ** tag

  let private tag (str: string) = sprintf "Iris.%s" str

  // ** Msg

  [<RequireQualifiedAccess>]
  type Msg =
    | UpdatePin of Pin
    | CallCue   of Cue
    | Status    of ServiceStatus
    | Update

  // ** PluginState

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Status: ServiceStatus
      ApiClient: IApiClient
      Events: ConcurrentQueue<Msg>
      V1Host: IPluginHost
      V2Host: IHDEHost
      Logger: ILogger
      InServer: IDiffSpread<string>
      InPort: IDiffSpread<uint16>
      InDebug: IDiffSpread<bool>
      OutState: ISpread<State>
      OutConnected: ISpread<bool>
      OutStatus: ISpread<string>
      Disposables: IDisposable list }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Status = ServiceStatus.Starting
        ApiClient = Unchecked.defaultof<IApiClient>
        Events = new ConcurrentQueue<Msg>()
        V1Host = Unchecked.defaultof<IPluginHost>
        V2Host = Unchecked.defaultof<IHDEHost>
        Logger = Unchecked.defaultof<ILogger>
        InServer = Unchecked.defaultof<IDiffSpread<string>>
        InPort = Unchecked.defaultof<IDiffSpread<uint16>>
        InDebug = Unchecked.defaultof<IDiffSpread<bool>>
        OutState = Unchecked.defaultof<ISpread<State>>
        OutConnected = Unchecked.defaultof<ISpread<bool>>
        OutStatus = Unchecked.defaultof<ISpread<string>>
        Disposables = List.empty }

    interface IDisposable with
      member self.Dispose() =
        try
          List.iter dispose self.Disposables // first dispose the logger to prevent the logger from
          dispose self.ApiClient             // causing a VVVV crash. Then dispose the rest..
        with
          | _ -> ()

  // ** log

  let log (state: PluginState) (level: LogType) (msg: string) =
    state.Logger.Log(level, msg)

  // ** debug

  let debug (state: PluginState) (msg: string) =
    log state LogType.Debug msg

  // ** error

  let error (state: PluginState) (msg: string) =
    log state LogType.Error msg

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
    match ev with
    | ClientEvent.Registered ->
      ServiceStatus.Running
      |> Msg.Status
      |> state.Events.Enqueue
    | ClientEvent.UnRegistered ->
      ServiceStatus.Stopped
      |> Msg.Status
      |> state.Events.Enqueue
    | ClientEvent.Status status ->
      status
      |> Msg.Status
      |> state.Events.Enqueue
    | ClientEvent.Snapshot | ClientEvent.Update _ ->
      state.Events.Enqueue Msg.Update

  // ** initialize

  let private initialize (state: PluginState) =
    if not state.Initialized then
      let me =
        let ip =
          match Network.getIpAddress () with
          | Some ip -> IpAddress.ofIPAddress ip
          | None -> IPv4Address "127.0.0.1"

        { Id = Id.Create ()
          Name = "Vvvv GraphApi Client"
          Role = Role.Renderer
          Status = ServiceStatus.Starting
          IpAddress = IPv4Address "192.168.2.105"
          Port = 10001us }

      let server =
        let ip =
          match state.InServer.[0] with
          | null ->  IPv4Address "127.0.0.1"
          | ip -> IPv4Address ip

        { Id = Id.Create ()
          Port = 10000us
          Name = "iris.exe"
          IpAddress = IPv4Address "192.168.2.108" }

      try
        let logobs = Logger.subscribe (string >> debug state)

        let result =
          either {
            let! client = ApiClient.create server me
            do! client.Start()
            return client
          }

        match result with
        | Right client ->
          let apiobs = client.Subscribe(enqueueEvent state)
          debug state "successfully started ApiClient"
          { state with
              Initialized = true
              Status = ServiceStatus.Running
              ApiClient = client
              Disposables = [ apiobs; logobs ] } // order matters!
        | Left error ->
          debug state (sprintf "Error starting ApiClient: %A" error)
          { state with
              Initialized = true
              Status = ServiceStatus.Failed error }
        |> setStatus
      with
        | exn ->
          debug state (sprintf "Error starting ApiClient: %A" exn.Message)
          debug state exn.StackTrace
          let error = exn.Message |> Error.asClientError (tag "initialize")
          { state with
              Initialized = true
              Status = ServiceStatus.Failed error }
          |> setStatus
    else
      state

  // ** callCue

  let private callCue (state: PluginState) (cue: Cue) =
    debug state "CallCue"
    state

  // ** updatePin

  let private updatePin (state: PluginState) (pin: Pin) =
    debug state "UpdatePin"
    state

  // ** setState

  let private setState (state: PluginState) =
    debug state "setState"
    state

  // ** processMsgs

  let private processMsgs (state: PluginState) =
    if state.Events.Count > 0 then
      let mutable run = true
      let mutable newstate = state
      while run do
        match state.Events.TryDequeue() with
        | true, msg ->
          newstate <-
            match msg with
            | Msg.CallCue cue   -> callCue   state cue
            | Msg.UpdatePin pin -> updatePin state pin
            | Msg.Update        -> setState  state
            | Msg.Status status ->
              { newstate with Status = status}
              |> setStatus
        | false, _ -> run <- false
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

  // ------------  Call Graph -------------------
  //
  // Evaluate
  //    |
  //    Process (update our world)
  //    |  |
  //    |  CallCue &&  UpdatePin
  //    |      |
  //    |      pin.Update (either values, or entire pin)
  //    |      |
  //    |      MkQueueJob value
  //    |
  //    Tick  (now flush it to vvvv)
  //    |  |
  //    |  VVVVGraph.FrameCount <= CurrentFrame
  //    |  |
  //    |  ProcessGraphWrites
  //    |         |
  //    |         IPin2.Spread = "|val|"
  //    |         |
  //    |         MkQueueJob value (Reset with current frame + 1)
  //    |
  //    Cleanup
  //

  // ** evaluate

  let evaluate (state: PluginState) (spreadMax: int) =
    state
    |> initialize
    |> processor
