open Fable.Core
open Fable.Import
open Test.Units

[<Emit("$0.apply()")>]
let apply f = failwith "ONLY IN JS"

let main _ =
  [ Html.main
  ; Store.main
  ; Storage.main
  ; Plugins.main
  ; VirtualDom.main
  ; PatchesView.main
  ; ViewController.main
  ; SerializationTests.main
  ] |> List.iter apply

@"
//  _   _                 _
// | \ | |___ _   _ _ __ | | __
// |  \| / __| | | | '_ \| |/ /
// | |\  \__ \ |_| | | | |   <
// |_| \_|___/\__, |_| |_|_|\_\
// © 2016     |___/

" |> printfn "%s"

main()
