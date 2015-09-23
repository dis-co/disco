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

type ValueData = {
    name      : Name;       
    tag       : Tag;        
    valType   : ValType;
    behavior  : Behavior;   
    vecSize   : VectorSize; 
    min       : Min;        
    max       : Max;        
    unit      : Unit;       
    precision : Precision;
    slices    : Values;
  }

type StringData = {
    name     : Name;       
    tag      : Tag;        
    strType  : StringType;
    fileMask : FileMask;   
    maxChars : MaxChars;   
    slices   : Values;
  }

type ColorData = {
    name   : Name;       
    tag    : Tag;        
    slices : Values;
  }

type EnumData = {
    name       : Name;       
    tag        : Tag;        
    properties : Properties;
    slices     : Values;
  }
  
type NodeData = {
    name : Name;
    tag  : Tag;
  }

type IOBox =
  | ValueBox  of ValueData
  | StringBox of StringData
  | ColorBox  of ColorData
  | EnumBox   of EnumData
  | NodeBox   of NodeData

let updateValues (box : IOBox) values =
  match box with
    | ValueBox  data -> ValueBox  { data with slices = values }
    | StringBox data -> StringBox { data with slices = values }
    | ColorBox  data -> ColorBox  { data with slices = values }
    | EnumBox   data -> EnumBox   { data with slices = values }
    | box -> box


let setName (box : IOBox) n =
  match box with
    | ValueBox  data -> ValueBox  { data with name = n }
    | StringBox data -> StringBox { data with name = n }
    | ColorBox  data -> ColorBox  { data with name = n }
    | EnumBox   data -> EnumBox   { data with name = n }
    | NodeBox   data -> NodeBox   { data with name = n }

let getName box = 
  match box with
    | ValueBox  { name = n } -> n
    | StringBox { name = n } -> n
    | ColorBox  { name = n } -> n
    | EnumBox   { name = n } -> n
    | NodeBox   { name = n } -> n

let isBehavior b box =
  match box with
    | ValueBox { behavior = b' } -> b = b'
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

