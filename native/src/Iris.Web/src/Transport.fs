[<FunScript.JS>]
module Iris.Web.Transport

open FunScript
open FunScript.TypeScript

open Iris.Web.Types

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
let createImpl(host : string, onOpen : unit -> unit, onMessage : Message -> unit, onClosed : unit -> unit) : IWebSocket = 
    failwith "never"

let create(host, onMessage, onClosed) =
    Async.FromContinuations (fun (callback, _, _) ->
        let socket = ref Unchecked.defaultof<_>
        socket := createImpl(host, (fun () -> callback !socket), onMessage, onClosed))
