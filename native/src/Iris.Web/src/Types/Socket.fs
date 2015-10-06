[<ReflectedDefinition>]
module Iris.Web.Types.Socket

open FunScript
open FunScript.TypeScript
open Iris.Web.Types.Events

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

type IWebSocket =
  abstract send : string -> unit
  abstract close : unit -> unit
  

[<JSEmit("""
    socket = new WebSocket({0});
    socket.onopen = function () { 
        {1}();
    };
    socket.onmessage = function (msg) {
        {2}(JSON.parse(msg.data));
    };
    socket.onclose = function () {
        {3}();
    };
    return socket;
    """)>]
let private createImpl(host : string, onOpen : unit -> unit, onMessage : Message -> unit, onClosed : unit -> unit) : IWebSocket = 
    failwith "never"

let create(host, onMessage, onClosed) =
    Async.FromContinuations (fun (callback, _, _) ->
        let socket = ref Unchecked.defaultof<_>
        socket := createImpl(host, (fun () -> callback !socket), onMessage, onClosed))
