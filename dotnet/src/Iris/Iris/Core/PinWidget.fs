namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Iris.Serialization

#endif

open Path

// * PinWidgetYaml

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

type PinWidgetYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable WidgetType: string

  static member From (widget: PinWidget) =
    let yml = PinWidgetYaml()
    yml.Id <- string widget.Id
    yml.Name <- unwrap widget.Name
    yml.WidgetType <- string widget.WidgetType
    yml

  member yml.ToPinWidget() =
    { Id = Id yml.Id
      Name = name yml.Name
      WidgetType = Id yml.WidgetType }
    |> Either.succeed

#endif

// * PinWidget

type PinWidget =
  { Id: Id
    Name: Name
    WidgetType: Id }

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member widget.ToYamlObject () = PinWidgetYaml.From(widget)

  // ** ToYaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject (yml: PinWidgetYaml) = yml.ToPinWidget()

  // ** FromYaml

  static member FromYaml (str: string) : Either<IrisError,PinWidget> =
    let serializer = Serializer()
    let yml = serializer.Deserialize<PinWidgetYaml>(str)
    Yaml.fromYaml yml

  #endif

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: PinWidgetFB) =
    { Id = Id fb.Id
      Name = name fb.Name
      WidgetType = Id fb.WidgetType }
    |> Either.succeed

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PinWidgetFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let widgetType = self.WidgetType |> string |> builder.CreateString
    PinWidgetFB.StartPinWidgetFB(builder)
    PinWidgetFB.AddId(builder, id)
    Option.iter (fun value -> PinWidgetFB.AddName(builder,value)) name
    PinWidgetFB.AddWidgetType(builder, widgetType)
    PinWidgetFB.EndPinWidgetFB(builder)

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,PinWidget> =
    Binary.createBuffer bytes
    |> PinWidgetFB.GetRootAsPinWidgetFB
    |> PinWidget.FromFB

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError, PinWidget> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, PinWidget array> =
    basePath </> filepath Constants.PINWIDGET_DIR
    |> IrisData.loadAll

  // ** Save

  member widget.Save (basePath: FilePath) =
    PinWidget.save basePath widget

  // ** Delete

  member widget.Delete (basePath: FilePath) =
    IrisData.delete basePath widget

  // ** Exists

  member widget.Exists (basePath: FilePath) =
    basePath </> PinWidget.assetPath widget
    |> File.exists

  #endif

  // ** HasParent

  /// Widgets don't live in nested directories, hence false
  member widget.HasParent with get () = false

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member pinwidget.AssetPath
    with get () = PinWidget.assetPath pinwidget

// * PinWidget module

module PinWidget =

  // ** create

  let create (widget: Id) (widgetName: Name) =
    { Id = Id.Create()
      Name = widgetName
      WidgetType = widget }

  // ** assetPath

  let assetPath (widget: PinWidget) =
    let path = (string widget.Id |> String.sanitize) + ASSET_EXTENSION
    PINWIDGET_DIR <.> path

  // ** save

  #if !FABLE_COMPILER && !IRIS_NODES

  let save basePath (widget: PinWidget) =
    IrisData.save basePath widget

  #endif
