[<ReflectedDefinition>]
module Iris.Web.Types.IOBox

(*   ___ ___  ____            
    |_ _/ _ \| __ )  _____  __
     | | | | |  _ \ / _ \ \/ /
     | | |_| | |_) | (_) >  < 
    |___\___/|____/ \___/_/\_\
*)

type Slice (name : string, value: string) =
  let mutable name  = name
  let mutable value = value

type Slices = Slice array

type IOBox () =
  let   id     = ""
  let   name   = ""
  let ``type`` = ""
  let mutable slices = Array.empty
  
  member x.Name
    with get () = name

  member x.Type
    with get () = ``type``
    
  member x.Slices
    with get ()  = slices
    and  set arr = slices <- arr
