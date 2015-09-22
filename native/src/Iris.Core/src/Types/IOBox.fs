[<ReflectedDefinition>]
module Iris.Core.Types.Pin

type Name       = string
type Tag        = string option
type IrisId     = string
type NodePath   = string
type OSCAddress = string
type VectorSize = int
type Min        = int
type Max        = int
type Unit       = string
type FileMask   = string option
type MaxChars   = int
type Values     = string list
type Properties = string list
type Precision  = int

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
    Name       *
    Tag        *
    ValType    *
    Behavior   *
    VectorSize *
    Min        *
    Max        *
    Unit       *
    Precision  *
    Values

  | StringBox of
    Name       *
    Tag        *
    StringType *
    FileMask   *
    MaxChars   *
    Values

  | ColorBox  of
    Name       *
    Tag        *
    Values

  | EnumBox   of
    Name       *
    Tag        *
    Properties *
    Values

  | NodeBox   of
    Name       *
    Tag

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

let updateName box name =
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


let isBehavior b box =
  match box with
    | ValueBox(_,_,_,b',_,_,_,_,_,_) -> b = b'
    | _ -> false

let isBang = isBehavior Bang
let isToggle = isBehavior Toggle

let parseBehavior str =
  match str with
    | "Toggle" -> Toggle
    | "Press"  -> Bang
    | "Bang"   -> Bang
    | _        -> Slider

let parseValType str =
  match str with
    | "Boolean" -> Bool
    | "Real"    -> Real
    | _         -> Int

