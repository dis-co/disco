(* ------------------------------------------------------------------------
This file is part of fszmq.

This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
------------------------------------------------------------------------ *)
namespace fszmq

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open ZeroMQ
open ZeroMQ.lib

/// For use with the Polling module...
///
/// Associates a callback with a Socket instance and one or more events,
/// such that the callback is invoked when the event(s) occurs on the Socket instance
///
/// ** Note: all sockets passed to Polling.poll MUST share the same context
/// and belong to the thread calling Polling.poll **

type PollItem = ZPollItem

type Callback = Socket -> Message array -> unit

type Poll =
  | PollIn  of PollItem * Socket * Callback
  | PollOut of PollItem * Socket * Callback
  | PollIO  of PollItem * Socket * Callback

/// Contains methods for working with ZMQ's polling capabilities
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Polling =

  /// Creates a Poll item for the socket which will
  /// invoke the callback when the socket receives a message
  [<CompiledName("PollIn")>]
  let pollIn fn socket =
    PollIn(PollItem.CreateReceiver(), socket, fn)

  /// Creates a Poll item for the socket which will
  /// invoke the callback when the socket sends a message
  [<CompiledName("PollOut")>]
  let pollOut fn socket =
    PollOut(PollItem.CreateSender(), socket, fn)

  /// Creates a Poll item for the socket which will
  /// invoke the callback when the socket sends or receives messages
  ///
  [<CompiledName("PollIO")>]
  let pollIO fn socket =
    PollIO(PollItem.CreateReceiverSender(), socket, fn)

  let inline private third (_,_,c) = c
  let inline private _combine a b c = (a, b, c)
  let inline private _call (s, f : Callback,_) msg = f s msg

  let private _poll timeout m p = 
    let mutable err = new ZError(0)
    use mutable msg = new ZMessage()

    let timeout' =
      match timeout with
        | Some t -> new Nullable<TimeSpan>(TimeSpan.FromMilliseconds(float t))
        | _ -> new Nullable<TimeSpan>()

    let result =
      match p with
        | PollIn(item, socket, fn) ->
          if Option.isSome timeout
            then socket.PollIn(item, &msg, &err, timeout')
            else socket.PollIn(item, &msg, &err)
          |> _combine socket fn
        | PollOut(item, socket, fn) ->
          if Option.isSome timeout
            then socket.PollOut(item, msg, &err, timeout')
            else socket.PollOut(item, msg, &err) 
          |> _combine socket fn
        | PollIO(item, socket, fn) ->
          if Option.isSome timeout
            then socket.Poll(item,ZPoll.In ||| ZPoll.Out, &msg, &err, timeout')
            else socket.Poll(item,ZPoll.In ||| ZPoll.Out, &msg, &err)
          |> _combine socket fn

    if third result
      then
        let output = Array.zeroCreate msg.Count
        let i = ref 0

        for item in msg do
          output.[!i] <- new Message(item.Read())
          i := !i + 1

        _call result output
        third result
      else m

  /// Performs a single polling run
  /// across the given sequence of Poll items, waiting up to the given timeout.
  /// Returns true when one or more callbacks have been invoked, returns false otherwise.
  ///
  /// ** Note: All items passed to Polling.poll MUST share the same context
  /// and belong to the thread calling `Polling.poll`. **
  ///
  /// This function is named DoPoll in compiled assemblies.
  /// If you are accessing the function from a language other than F#, or through reflection, use this name.
  [<CompiledName("DoPoll")>]
  let poll (timeout:int64) items =
    Seq.toArray items |> Array.fold (Some timeout |> _poll) false

  /// Calls `Polling.poll` with the given sequence of Poll items and 0 microseconds timeout
  [<CompiledName("PollNow")>]
  let pollNow items =
    Seq.toArray items |> Array.fold (Some ZMQ.NOW |> _poll) false

  /// Calls `Polling.poll` with the given sequence of Poll items and no timeout,
  /// effectively causing the polling loop to block indefinitely.
  [<CompiledName("PollForever")>]
  let pollForever items =
    Seq.toArray items |> Array.fold (Some ZMQ.FOREVER |> _poll) false

  /// Polls the given socket, up to the given timeout, for an input message.
  /// Returns a byte[][] option, where None indicates no message was received.
  [<CompiledName("TryPollInput")>]
  let tryPollInput (timeout:int64) socket =
    let msg = ref Array.empty

    let cb (_: Socket) (messages: Message array) =
      msg := Array.append !msg messages

    let items = [ pollIn cb socket ]
    match poll timeout items with
    | true  -> Some !msg
    | false -> None


/// Utilities for working with Polling from languages other than F#
[<Extension>]
type PollingExtensions =

  /// Creates a Poll item for the socket which will
  /// invoke the callback when the socket receives a message
  [<Extension>]
  static member AsPollIn (socket,callback:Action<_>) =
    socket |> Polling.pollIn (fun s msgs -> callback.Invoke(s, msgs))

  /// Creates a Poll item for the socket which will
  /// invoke the callback when the socket receives a message
  [<Extension>]
  static member AsPollOut (socket,callback:Action<_>) =
    socket |> Polling.pollOut (fun s msgs -> callback.Invoke(s, msgs))

  /// Creates a Poll item for the socket which will
  /// invoke the callback when the socket sends or receives a message
  [<Extension>]
  static member AsPollIO (socket,callback:Action<_>) =
    socket |> Polling.pollIO (fun s msgs -> callback.Invoke(s, msgs))

  /// Polls the given socket, up to the given timeout, for an input message.
  /// Retuns true if input was received, in which case the message is assigned to the out parameter.
  [<Extension>]
  static member TryGetInput (socket,timeout:int64,[<Out>]message:byref<Message []>) =
    match Polling.tryPollInput timeout socket with
    | Some msg  -> message <- msg;  true
    | None      -> message <- [||]; false
