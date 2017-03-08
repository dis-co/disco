namespace Iris.Nodes

// * Imports

open System
open System.Web
open System.Collections.Generic
open System.Collections.Concurrent
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.PluginInterfaces.V2.Graph
open VVVV.Core.Logging
open Iris.Core
open Iris.Client
open Newtonsoft.Json

// * GraphApi

[<RequireQualifiedAccess>]
module GraphApi =

  // ** tag

  let private tag (str: string) = sprintf "Iris.%s" str

  //   ____                 _     ____       _       _
  //  / ___|_ __ __ _ _ __ | |__ |  _ \ __ _| |_ ___| |__
  // | |  _| '__/ _` | '_ \| '_ \| |_) / _` | __/ __| '_ \
  // | |_| | | | (_| | |_) | | | |  __/ (_| | || (__| | | |
  //  \____|_|  \__,_| .__/|_| |_|_|   \__,_|\__\___|_| |_|
  //                 |_|

  // ** PinAttributes

  type PinAttributes =
    { Id: string }

  // ** NodeAttributes

  type NodeAttributes =
    { Pins: Dictionary<string,PinAttributes> }

    member attrs.ToJson() : string =
      JsonConvert.SerializeObject attrs

  // ** GraphPatch

  type GraphPatch =
    { Frame: uint64
      ParentId: int
      ParentFileName: string
      XmlSnippet: string }

    override patch.ToString() =
      sprintf "frame=%d parentid=%d parentfile=%s xml=%s"
        patch.Frame
        patch.ParentId
        patch.ParentFileName
        patch.XmlSnippet

  // ** NodePatch

  type NodePatch =
    { FilePath: string
      Payload: string }

  // ** Msg

  [<RequireQualifiedAccess>]
  type Msg =
    | PinAdded   of Pin                  // a new pin got added in the local VVVV instance
    | PinRemoved of Pin                  // a pin got removed in the local VVVV instance
    | PinUpdated of Pin                  // a remote pin got updated
    | CallCue    of Cue
    | Status     of ServiceStatus
    | GraphPatch of GraphPatch
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
        V1Host = null
        V2Host = null
        Logger = null
        InServer = null
        InPort = null
        InDebug = null
        OutState = null
        OutConnected = null
        OutStatus = null
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
    if state.InDebug.[0] then
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

  // ** startClient

  let private startClient (state: PluginState) =
    let logobs = Logger.subscribe (string >> debug state)
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
      debug state "successfully started ApiClient"
      { state with
          Initialized = true
          Status = ServiceStatus.Running
          ApiClient = client
          Disposables = [ apiobs; logobs ] }
    | Left error ->
      debug state (sprintf "Error starting ApiClient: %A" error)
      { state with
          Initialized = true
          Status = ServiceStatus.Failed error }
    |> setStatus

  let private htmlEncodePayload (raw: string) =
    "|" + raw + "|"
    |> HttpUtility.HtmlEncode

  let private htmlDecodePayload (raw: string) =
    raw.Substring(1, raw.Length - 1).Substring(0, raw.Length - 2)
    |> HttpUtility.HtmlDecode

  let private formatNodeTagSnippet (node: INode2) (raw: string) =
    let tmpl =
      @"<NODE id=""{0}"">
         <PIN pinname=""Tag"" slicecount=""1"" values=""{1}""/>
        </NODE>"
    String.Format(tmpl, node.ID, htmlEncodePayload raw)

  let private formatPatchTagSnippet (id: int) (tags: string) =
    let tmpl = @"<PATCH id=""{0}"">{1}</PATCH>";
    String.Format(tmpl, id, tags);

  // ** pin

  let private getPinByName (node: INode2) (name: string) =
    Seq.fold
      (fun (m: IPin2 option) (pin: IPin2) ->
        match m with
        | Some _ -> m
        | None ->
          if pin.Name = name then
            Some pin
          else
            None)
      None
      node.Pins

  [<Literal>]
  let private DESCRIPTIVE_NAME_PIN = "Descriptive Name"

  [<Literal>]
  let private TAG_PIN = "Tag"

  [<Literal>]
  let private IRIS_NODE_NAME = "Iris (Iris)"

  let private getIrisNode (state: PluginState) : INode2 option =
    let mutable result = None
    let root = state.V2Host.RootNode

    let rec getImpl (node: INode2) =
      for child in node do
        if child.Name = IRIS_NODE_NAME then
          result <- Some child
        else
          getImpl child

    getImpl root
    result

  // ** createAttributes

  let private createAttributes (state: PluginState) (node: INode2) =
    match getIrisNode state with
    | Some iris ->
      let attrs: NodeAttributes =
        let dict = new Dictionary<string,PinAttributes>()
        let path = node.GetNodePath(false)
        dict.Add("Pins", { Id = path })
        { Pins = dict }

      { Frame = state.Frame
        ParentId = iris.Parent.ID
        ParentFileName = iris.Parent.NodeInfo.Filename
        XmlSnippet = formatNodeTagSnippet iris (attrs.ToJson()) }
      |> Msg.GraphPatch
      |> state.Events.Enqueue
    | None -> ()

    match getPinByName node TAG_PIN with
    | Some pin ->
      debug state "-------------------- root tag field --------------------"
      debug state pin.[0]
      debug state "---------------------------------------------------"
    | _ -> ()

    let attrs: NodeAttributes =
      let dict = new Dictionary<string,PinAttributes>()

      match getPinByName node DESCRIPTIVE_NAME_PIN with
      | Some _ ->
        // let name =
        //   sprintf "%s -- %s"
        //     node.Parent.Name
        //     pin.[0]
        let path = node.GetNodePath(false)
        dict.Add("dn", { Id = path })
      | None -> ()

      { Pins = dict }

    { Frame = state.Frame
      ParentId = node.Parent.ID
      ParentFileName = node.Parent.NodeInfo.Filename
      XmlSnippet = formatNodeTagSnippet node (attrs.ToJson()) }
    |> Msg.GraphPatch
    |> state.Events.Enqueue

    attrs

  // ** parseINode2

  let private parseINode2 (_: INode2) : Either<IrisError,Pin> =
    Pin.Toggle(Id.Create(),"Hello",Id.Create(), [| |], [| |])
    |> Either.succeed

  // ** onNodeExposed

  let private onNodeExposed (state: PluginState) (node: INode2) =
    for pin in node.Pins do
      sprintf "Pin Name: %s Value: %A" pin.Name pin.[0]
      |> debug state

  // ** onNodeUnExposed

  let private onNodeUnExposed (state: PluginState) (node: INode2) =
    debug state "----------------------------------------> a node was un-exposed"
    for pin in node.Pins do
      sprintf "Pin Name: %s Value: %A" pin.Name pin.[0]
      |> debug state
    debug state "----------------------------------------"

  // ** setupVvvv

  let private setupVvvv (state: PluginState) =
    let onNodeAdded = new NodeEventHandler(onNodeExposed state)
    let onNodeRemoved = new NodeEventHandler(onNodeUnExposed state)

    state.V2Host.ExposedNodeService.add_NodeAdded(onNodeAdded)
    state.V2Host.ExposedNodeService.add_NodeRemoved(onNodeRemoved)

    let disposable =
      { new IDisposable with
          member self.Dispose () =
            state.V2Host.ExposedNodeService.remove_NodeAdded(onNodeAdded)
            state.V2Host.ExposedNodeService.remove_NodeRemoved(onNodeRemoved) }

    { state with Disposables = disposable :: state.Disposables }

  // ** initialize

  let private initialize (state: PluginState) =
    if not state.Initialized then
      state
      |> startClient
      |> setupVvvv
    else
      state

  // ** callCue

  let private callCue (state: PluginState) (_: Cue) =
    debug state "CallCue"
    state

  // ** addPin

  let private addPin (state: PluginState) (_: Pin) =
    debug state "addPin"
    state

  // ** removePin

  let private removePin (state: PluginState) (_: Pin) =
    debug state "removePin"
    state

  // ** updatePin

  let private updatePin (state: PluginState) (_: Pin) =
    debug state "updatePin"
    state

  // ** setState

  let private setState (state: PluginState) =
    debug state "setState"
    state

  // ** patchGraph

  let private patchGraph (state: PluginState) (patch: GraphPatch) =
    debug state (string patch)

    let patches = new Dictionary<int,NodePatch>()

    if patch.Frame < state.Frame then
      if patches.ContainsKey(patch.ParentId) then
        let tmp = patches.[patch.ParentId].Payload + patch.XmlSnippet
        patches.[patch.ParentId] <- { FilePath = patch.ParentFileName; Payload = tmp }
      else
        patches.[patch.ParentId] <- { FilePath = patch.ParentFileName; Payload = patch.XmlSnippet }

      if patches.Count > 0 then
        for KeyValue(key,value) in patches do
          let ptc = formatPatchTagSnippet key value.Payload
          debug state (string ptc)
          state.V2Host.SendXMLSnippet(value.FilePath, ptc, false)

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
            | Msg.CallCue cue    -> callCue    state cue
            | Msg.PinAdded pin   -> addPin     state pin
            | Msg.PinRemoved pin -> removePin  state pin
            | Msg.PinUpdated pin -> updatePin  state pin
            | Msg.Update         -> setState   state
            | Msg.GraphPatch ptc -> patchGraph state ptc
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

  // ** evaluate

  let evaluate (state: PluginState) (_: int) =
    state
    |> initialize
    |> processor
