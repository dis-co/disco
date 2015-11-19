namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Socket =

  (*   __  __
      |  \/  | ___  ___ ___  __ _  __ _  ___
      | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
      | |  | |  __/\__ \__ \ (_| | (_| |  __/
      |_|  |_|\___||___/___/\__,_|\__, |\___|
                                  |___/
  *)
  type MsgType = string

  type Message [<Inline "{}">] () =
    [<DefaultValue>]
    val mutable Type : string

    [<DefaultValue>]
    val mutable Payload : obj
