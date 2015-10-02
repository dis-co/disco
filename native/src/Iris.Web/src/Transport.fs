[<FunScript.JS>]
module Iris.Web.Transport

open FunScript
open FunScript.TypeScript

type IWebSocket =
    abstract send : string -> unit
    abstract close : unit -> unit
    
[<JSEmit("""
    socket = new WebSocket({0});
    socket.onopen = function () { 
        {1}();
    };
    socket.onmessage = function (msg) {
        {2}(msg.data);
    };
    socket.onclose = function () {
        {3}();
    };
    return socket;
    """)>]
let createImpl(host : string, onOpen : unit -> unit, onMessage : string -> unit, onClosed : unit -> unit) : IWebSocket = 
    failwith "never"

let create(host, onMessage, onClosed) =
    Async.FromContinuations (fun (callback, _, _) ->
        let socket = ref Unchecked.defaultof<_>
        socket := createImpl(host, (fun () -> callback !socket), onMessage, onClosed))
