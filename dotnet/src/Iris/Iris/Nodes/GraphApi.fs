namespace Iris.Nodes

open System
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Core.Logging
open Iris.Core
open Iris.Client

[<RequireQualifiedAccess>]
module GraphApi =

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Status: ServiceStatus
      ApiClient: IApiClient
      V1Host: IPluginHost
      V2Host: IHDEHost
      Logger: ILogger
      InServer: IDiffSpread<string>
      InPort: IDiffSpread<uint16>
      InDebug: IDiffSpread<bool>
      OutState: ISpread<State>
      OutConnected: ISpread<bool>
      Disposables: IDisposable list }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Status = ServiceStatus.Starting
        ApiClient = Unchecked.defaultof<IApiClient>
        V1Host = Unchecked.defaultof<IPluginHost>
        V2Host = Unchecked.defaultof<IHDEHost>
        Logger = Unchecked.defaultof<ILogger>
        InServer = Unchecked.defaultof<IDiffSpread<string>>
        InPort = Unchecked.defaultof<IDiffSpread<uint16>>
        InDebug = Unchecked.defaultof<IDiffSpread<bool>>
        OutState = Unchecked.defaultof<ISpread<State>>
        OutConnected = Unchecked.defaultof<ISpread<bool>>
        Disposables = List.empty }

    interface IDisposable with
      member self.Dispose() =
        List.iter dispose self.Disposables // first dispose the logger to prevent the logger from
        dispose self.ApiClient             // causing a VVVV crash. Then dispose the rest..

  let log (state: PluginState) (level: LogType) (msg: string) =
    try
      state.Logger.Log(level, msg)
    with
      | _ -> () // gulp

  let debug (state: PluginState) (msg: string) =
    log state LogType.Debug msg

  let error (state: PluginState) (msg: string) =
    log state LogType.Error msg

  // ------------  Call Graph (for Bangs)  -------------------
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

  let initialize (state: PluginState) =
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
        let obs = Logger.subscribe (string >> debug state)

        let result =
          either {
            let! client = ApiClient.create server me
            do! client.Start()
            return client
          }
        match result with
        | Right client ->
          debug state "successfully started ApiClient"
          { state with
              Initialized = true
              ApiClient = client
              Disposables = [ obs ] }
        | Left error ->
          debug state (sprintf "Error starting ApiClient: %A" error)
          state
      with
        | exn ->
          debug state (sprintf "Error starting ApiClient: %A" exn.Message)
          debug state exn.StackTrace
          state
    else
      state

  let processor (state: PluginState) =
    debug state (sprintf "Frame: %d" state.Frame)
    { state with Frame = state.Frame + 1UL }

  let evaluate (state: PluginState) (spreadMax: int) =
    state
    |> initialize
    |> processor
