namespace Iris.Service.Client.Core

#nowarn "1182"

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Socket =

  open Iris.Service.Client.Core.Events

  (*   __  __
      |  \/  | ___  ___ ___  __ _  __ _  ___
      | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
      | |  | |  __/\__ \__ \ (_| | (_| |  __/
      |_|  |_|\___||___/___/\__,_|\__, |\___|
                                  |___/
  *)
  type MsgType = string

  type Message (t : MsgType, p : EventData) =
    let msgtype = t
    let payload = p

    member self.Type    with get () = msgtype
    member self.Payload with get () = payload

  (*  __        __   _    ____             _        _
      \ \      / /__| |__/ ___|  ___   ___| | _____| |_
       \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
        \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_
         \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|
  *)

  let createSocket(host, onMessage, onClosed) = failwith "FIXEME"
    // Async.FromContinuations
    //   (fun (callback, _, _) ->
    //    let sockref = ref<WebSocket>
    //    let socket = new WebSocket(host)

    //    // socket.Onopen <- (fun 
    //    (!socket).Onmessage <- onMessage
    //    (!socket) <- onClosed)
    //       //socket := createImpl(host, (fun () -> callback !socket), onMessage, onClosed))
