(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Core

// * Imports

open Aether
open Aether.Operators

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Disco.Serialization

#endif

open Path

// * PinWidgetYaml

#if !FABLE_COMPILER && !DISCO_NODES

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
    result {
      let! id = DiscoId.TryParse yml.Id
      let! widget = DiscoId.TryParse yml.WidgetType
      return {
        Id = id
        Name = name yml.Name
        WidgetType = widget
      }
    }

#endif

// * PinWidget

type PinWidget =
  { Id: WidgetId
    Name: Name
    WidgetType: WidgetTypeId }

  // ** optics

  static member Id_ =
    (fun (widget:PinWidget) -> widget.Id),
    (fun id (widget:PinWidget) -> { widget with Id = id })

  static member Name_ =
    (fun (widget:PinWidget) -> widget.Name),
    (fun name (widget:PinWidget) -> { widget with Name = name })

  static member WidgetType_ =
    (fun (widget:PinWidget) -> widget.WidgetType),
    (fun widgetType (widget:PinWidget) -> { widget with WidgetType = widgetType })

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !DISCO_NODES

  member widget.ToYaml () = PinWidgetYaml.From(widget)

  // ** FromYaml

  static member FromYaml(yml: PinWidgetYaml) = yml.ToPinWidget()

  #endif

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: PinWidgetFB) =
    result {
      let! id = Id.decodeId fb
      let! widget = Id.decodeWidgetType fb
      return {
        Id = id
        Name = name fb.Name
        WidgetType = widget
      }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PinWidgetFB> =
    let id = PinWidgetFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let widgetType = PinWidgetFB.CreateWidgetTypeVector(builder,self.WidgetType.ToByteArray())
    PinWidgetFB.StartPinWidgetFB(builder)
    PinWidgetFB.AddId(builder, id)
    Option.iter (fun value -> PinWidgetFB.AddName(builder,value)) name
    PinWidgetFB.AddWidgetType(builder, widgetType)
    PinWidgetFB.EndPinWidgetFB(builder)

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : DiscoResult<PinWidget> =
    Binary.createBuffer bytes
    |> PinWidgetFB.GetRootAsPinWidgetFB
    |> PinWidget.FromFB

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !DISCO_NODES

  static member Load(path: FilePath) : DiscoResult<PinWidget> =
    DiscoData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : DiscoResult<PinWidget array> =
    basePath </> filepath Constants.PINWIDGET_DIR
    |> DiscoData.loadAll

  // ** Save

  member widget.Save (basePath: FilePath) =
    PinWidget.save basePath widget

  // ** Delete

  member widget.Delete (basePath: FilePath) =
    DiscoData.delete basePath widget

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

  // ** getters

  let id = Optic.get PinWidget.Id_
  let name = Optic.get PinWidget.Name_
  let widgetType = Optic.get PinWidget.WidgetType_

  // ** setters

  let setId = Optic.set PinWidget.Id_
  let setName = Optic.set PinWidget.Name_
  let setWidgetType = Optic.set PinWidget.WidgetType_

  // ** create

  let create (widgetName: string) (widget: WidgetTypeId) =
    { Id = DiscoId.Create()
      Name = Measure.name widgetName
      WidgetType = widget }

  // ** assetPath

  let assetPath (widget: PinWidget) =
    let path = (string widget.Id |> String.sanitize) + ASSET_EXTENSION
    PINWIDGET_DIR <.> path

  // ** save

  #if !FABLE_COMPILER && !DISCO_NODES

  let save basePath (widget: PinWidget) =
    DiscoData.save basePath widget

  #endif

  // ** updatePins

  let updatePins (_:Slices) (widget:PinWidget) =
    widget

  // ** processSlices

  let processSlices (slices:Map<PinId,Slices>) (widget:PinWidget) =
    Map.fold
      (fun (widget:PinWidget) _ (slices:Slices) ->
        updatePins slices widget)
      widget
      slices
