namespace Disco.Nodes

// * Imports

open Disco.Core
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.PluginInterfaces.V2.Graph
open VVVV.Core.Logging

// * PinType

[<RequireQualifiedAccess>]
type PinType =
  | Boolean
  | Number
  | Enum
  | Color
  | String

// * NodeMapping

type NodeMapping =
  { PinId: PinId
    GroupId: PinGroupId
    NodePath: NodePath
    Pin: IPin2
    Type: PinType
    Trigger: bool
    PinConfiguration: PinConfiguration
    Properties: Property array option
    ChangedNode: IPin2 }

  member nm.IsSource
    with get () = nm.PinConfiguration = PinConfiguration.Source

  member nm.IsSink
    with get () = nm.PinConfiguration = PinConfiguration.Sink

// * Settings

[<RequireQualifiedAccess>]
module Settings =

  [<Literal>]
  let NODES_CATEGORY = "Disco"

  [<Literal>]
  let DESCRIPTIVE_NAME_PIN = "Descriptive Name"

  [<Literal>]
  let PAGES_PIN = "Pages"

  [<Literal>]
  let ROWS_PIN = "Rows"

  [<Literal>]
  let COLUMNS_PIN = "Columns"

  [<Literal>]
  let CHANGED_PIN = "Changed"

  [<Literal>]
  let TAG_PIN = "Tag"

  [<Literal>]
  let VALUE_TYPE_PIN = "Value Type"

  [<Literal>]
  let BEHAVIOR_PIN = "Behavior"

  [<Literal>]
  let SLICECOUNT_MODE_PIN = "SliceCount Mode"

  [<Literal>]
  let VECSIZE_PIN = "Vector Size"

  [<Literal>]
  let MIN_PIN = "Minimum"

  [<Literal>]
  let MAX_PIN = "Maximum"

  [<Literal>]
  let UNITS_PIN = "Units"

  [<Literal>]
  let PRECISION_PIN = "Precision"

  [<Literal>]
  let STRING_TYPE_PIN = "String Type"

  [<Literal>]
  let MAXCHAR_PIN = "Maximum Characters"

  [<Literal>]
  let INPUT_ENUM_PIN = "Input Enum"

  [<Literal>]
  let PIN_NAME_PROP = "PinName"

  [<Literal>]
  let PIN_PATH_PROP = "NodePath"

  [<Literal>]
  let TOP_LEVEL_GROUP_NAME = "VVVV Default Group"

  let TOP_LEVEL_GROUP_ID = DiscoId.Parse "d7b5c489-0772-47ac-8433-8d8911aa1cd5"

// * Util

[<RequireQualifiedAccess>]
module Util =

  let inline isNullReference (o: 't) =
    obj.ReferenceEquals(o, null)

// * Graph Patching
  // //   ____                 _     ____       _       _
  // //  / ___|_ __ __ _ _ __ | |__ |  _ \ __ _| |_ ___| |__
  // // | |  _| '__/ _` | '_ \| '_ \| |_) / _` | __/ __| '_ \
  // // | |_| | | | (_| | |_) | | | |  __/ (_| | || (__| | | |
  // //  \____|_|  \__,_| .__/|_| |_|_|   \__,_|\__\___|_| |_|
  // //                 |_|

  // // ** PinAttributes

  // type PinAttributes =
  //   { Id: string }

  // // ** NodeAttributes

  // type NodeAttributes =
  //   { Pins: Dictionary<string,PinAttributes> }

  //   member attrs.ToJson() : string =
  //     JsonConvert.SerializeObject attrs

  // // ** GraphPatch

  // type GraphPatch =
  //   { Frame: uint64
  //     ParentId: int
  //     ParentFileName: string
  //     XmlSnippet: string }

  //   override patch.ToString() =
  //     sprintf "frame=%d parentid=%d parentfile=%s xml=%s"
  //       patch.Frame
  //       patch.ParentId
  //       patch.ParentFileName
  //       patch.XmlSnippet

  // // ** NodePatch

  // type NodePatch =
  //   { FilePath: string
  //     Payload: string }

  // let private htmlEncodePayload (raw: string) =
  //   "|" + raw + "|"
  //   |> HttpUtility.HtmlEncode

  // let private htmlDecodePayload (raw: string) =
  //   raw.Substring(1, raw.Length - 1).Substring(0, raw.Length - 2)
  //   |> HttpUtility.HtmlDecode

  // let private formatNodeTagSnippet (node: INode2) (raw: string) =
  //   let tmpl =
  //     @"<NODE id=""{0}"">
  //        <PIN pinname=""Tag"" slicecount=""1"" values=""{1}""/>
  //       </NODE>"
  //   String.Format(tmpl, node.ID, htmlEncodePayload raw)

  // let private formatPatchTagSnippet (id: int) (tags: string) =
  //   let tmpl = @"<PATCH id=""{0}"">{1}</PATCH>";
  //   String.Format(tmpl, id, tags);


  // let private getDiscoNode (state: PluginState) : INode2 option =
  //   let mutable result = None
  //   let root = state.V2Host.RootNode

  //   let rec getImpl (node: INode2) =
  //     for child in node do
  //       if child.Name = DISCO_NODE_NAME then
  //         result <- Some child
  //       else
  //         getImpl child

  //   getImpl root
  //   result

  // ** createAttributes

  // let private createAttributes (state: PluginState) (node: INode2) =
  //   match getDiscoNode state with
  //   | Some disco ->
  //     let attrs: NodeAttributes =
  //       let dict = new Dictionary<string,PinAttributes>()
  //       let path = node.GetNodePath(false)
  //       dict.Add("Pins", { Id = path })
  //       { Pins = dict }

  //     { Frame = state.Frame
  //       ParentId = disco.Parent.ID
  //       ParentFileName = disco.Parent.NodeInfo.Filename
  //       XmlSnippet = formatNodeTagSnippet disco (attrs.ToJson()) }
  //     |> Msg.GraphPatch
  //     |> state.Events.Enqueue
  //   | None -> ()

  //   match getPinByName node TAG_PIN with
  //   | Some pin ->
  //     Util.debug state "-------------------- root tag field --------------------"
  //     Util.debug state pin.[0]
  //     Util.debug state "---------------------------------------------------"
  //   | _ -> ()

  //   let attrs: NodeAttributes = failwith "NEVER DEAR"
  //     let dict = new Dictionary<string,PinAttributes>()

  //     match getPinByName node DESCRIPTIVE_NAME_PIN with
  //     | Some _ ->
  //       // let name =
  //       //   sprintf "%s -- %s"
  //       //     node.Parent.Name
  //       //     pin.[0]
  //       let path = node.GetNodePath(false)
  //       dict.Add("dn", { Id = path })
  //     | None -> ()

  //     { Pins = dict }

  //   { Frame = state.Frame
  //     ParentId = node.Parent.ID
  //     ParentFileName = node.Parent.NodeInfo.Filename
  //     XmlSnippet = formatNodeTagSnippet node (attrs.ToJson()) }
  //   |> Msg.GraphPatch
  //   |> state.Events.Enqueue

  //   attrs

  // // ** patchGraph

  // let private patchGraph (state: PluginState) (patch: GraphPatch) =
  //   Util.debug state (string patch)

  //   let patches = new Dictionary<int,NodePatch>()

  //   if patch.Frame < state.Frame then
  //     if patches.ContainsKey(patch.ParentId) then
  //       let tmp = patches.[patch.ParentId].Payload + patch.XmlSnippet
  //       patches.[patch.ParentId] <- { FilePath = patch.ParentFileName; Payload = tmp }
  //     else
  //       patches.[patch.ParentId] <- { FilePath = patch.ParentFileName; Payload = patch.XmlSnippet }

  //     if patches.Count > 0 then
  //       for KeyValue(key,value) in patches do
  //         let ptc = formatPatchTagSnippet key value.Payload
  //         Util.debug state (string ptc)
  //         state.V2Host.SendXMLSnippet(value.FilePath, ptc, false)

  //   state
