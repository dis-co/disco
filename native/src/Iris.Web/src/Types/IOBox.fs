[<ReflectedDefinition>]
module Iris.Web.Types.IOBox

open FunScript
open FunScript.TypeScript

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

type IOBox (id, patch, name) =
  let id       : string = id
  let patch    : string = patch
  let name     : string = name
  let ``type`` : string = "number"
  let mutable slices    = Array.empty
  
  member self.Id
    with get () = id

  member self.PatchId
    with get () = patch

  member self.Name
    with get () = name

  member self.Type
    with get () = ``type``
    
  member self.Slices
    with get ()  = slices
    and  set arr = slices <- arr


type IOBoxes () =
  [<JSEmit("""{0}[{1}] = {2}""")>]
  member self.add (id : string) (box : IOBox) : unit = failwith "never"
    
  [<JSEmit("""arguments[0][{0}] = null""")>]
  member self.remove (id : string) : unit = failwith "never"
    
