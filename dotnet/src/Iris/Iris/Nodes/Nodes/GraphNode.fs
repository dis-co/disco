namespace VVVV.Nodes

// * Imports

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes

[<RequireQualifiedAccess>]
module internal Graph =

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

[<PluginInfo(Name="Graph", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type GraphNode() =

  [<Import();DefaultValue>]
  val mutable V1Host: IPluginHost

  [<Import();DefaultValue>]
  val mutable V2Host: IHDEHost

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Commands")>]
  val mutable InCommands: IDiffSpread<StateMachine>

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

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<GraphApi.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        let state' =
          { GraphApi.PluginState.Create() with
              V2Host = self.V2Host
              Logger = self.Logger
              InDebug = self.InDebug
              OutState = self.OutState
              OutStatus = self.OutStatus }
        state <- state'
        initialized <- true

      state <- GraphApi.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
