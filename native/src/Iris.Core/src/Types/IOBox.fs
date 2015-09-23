[<ReflectedDefinition>]
module Iris.Core.Types.IOBox

type Name       = string
type Tag        = string option
type IrisId     = string
type NodePath   = string
type OSCAddress = string
type VectorSize = int
type Min        = int
type Max        = int
type Unit       = string option
type FileMask   = string option
type MaxChars   = int
type Values     = string list
type Properties = string list
type Precision  = int option

type Behavior = 
  |  Slider
  |  Toggle
  |  Bang

type ValType =
  | Real
  | Int
  | Bool

type StringType =
  |  String
  |  MultiLine
  |  FileName
  |  Directory
  |  Url
  |  IP
  
type IOBox =
  | ValueBox  of
    name      : Name       *
    tag       : Tag        *
    valType   : ValType    *
    behavior  : Behavior   *
    vecSize   : VectorSize *
    min       : Min        *
    max       : Max        *
    unit      : Unit       *
    precision : Precision  *
    slices    : Values

  | StringBox of
    name     : Name       *
    tag      : Tag        *
    strType  : StringType *
    fileMask : FileMask   *
    maxChars : MaxChars   *
    slices   : Values

  | ColorBox  of
    name   : Name       *
    tag    : Tag        *
    slices : Values

  | EnumBox   of
    name       : Name       *
    tag        : Tag        *
    properties : Properties *
    slices     : Values

  | NodeBox   of
    name : Name * tag  : Tag

let updateValues box values =
  match box with
    | ValueBox(n,ta,ty,b,vt,mi,ma,u,p,_) ->
      ValueBox(n,ta,ty,b,vt,mi,ma,u,p,values)

    | StringBox(n,ta,st,f,m,_) ->
      StringBox(n,ta,st,f,m,values)

    | ColorBox(n,ta,_) ->
      ColorBox(n,ta,values)

    | EnumBox(n,ta,p,_) ->
      EnumBox(n,ta,p,values)

    | _ as box -> box

let setName box name =
  match box with
    | ValueBox(_,ta,ty,b,vt,mi,ma,u,p,va) ->
      ValueBox(name,ta,ty,b,vt,mi,ma,u,p,va)

    | StringBox(_,ta,st,f,m,va) ->
      StringBox(name,ta,st,f,m,va)

    | ColorBox(_,ta,va) ->
      ColorBox(name,ta,va)

    | EnumBox(_,ta,p,va) ->
      EnumBox(name,ta,p,va)

    | _ as box -> box

let getName box = 
  match box with
    | ValueBox(name = n')  -> n'
    | ColorBox(name = n')  -> n'
    | EnumBox(name = n')   -> n'
    | StringBox(name = n') -> n'
    | NodeBox(name = n')   -> n'

let isBehavior b box =
  match box with
    | ValueBox(behavior = b') -> b = b'
    | _ -> false

let isBang = isBehavior Bang
let isToggle = isBehavior Toggle

let parseBehavior str =
  match str with
    | "Bang" | "Press" -> Bang
    | "Toggle"         -> Toggle
    | _                -> Slider

let parseValType str =
  match str with
    | "Boolean" -> Bool
    | "Real"    -> Real
    | _         -> Int

