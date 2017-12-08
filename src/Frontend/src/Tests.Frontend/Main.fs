module Disco.Web.Tests.Main

open Fable.Core
open Fable.Import
open Test.Units

[<Emit("$0.apply()")>]
let apply f = failwith "ONLY IN JS"

let main _ =
  [ Store.main
  ; SerializationTests.main
  ; TypeTests.main
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
