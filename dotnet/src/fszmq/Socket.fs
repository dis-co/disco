(* ------------------------------------------------------------------------
This file is part of fszmq.

This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
------------------------------------------------------------------------ *)
namespace fszmq

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

open ZeroMQ
open ZeroMQ.lib
open ZeroMQ.Monitoring

/// Contains methods for working with Socket instances
[<Extension;CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Socket =

(* connectivity *)

  /// Causes an endpoint to start accepting
  /// connections at the given address
  [<Extension;CompiledName("Bind")>]
  let bind (socket:Socket) address = socket.Bind address

  /// Causes an endpoint to stop accepting
  /// connections at the given address
  [<Extension;CompiledName("Unbind")>]
  let unbind (socket:Socket) address = socket.Unbind address

  /// Connects to an endpoint to the given address
  [<Extension;CompiledName("Connect")>]
  let connect (socket:Socket) address = socket.Connect address

  /// Disconnects to an endpoint from the given address
  [<Extension;CompiledName("Disconnect")>]
  let disconnect (socket:Socket) address = socket.Disconnect address

  (* socket options *)

  /// Gets the value of the given option for the given Socket
  [<Extension;CompiledName("GetOption")>]
  let getOption<'t> (socket:Socket) (opts: ZSocketOption) : 't =
    let size,read =
      let   t = typeof<'t>
      if    t = typeof<int>     then   4,(snd >> readInt32  >> box)
      elif  t = typeof<bool>    then   4,(snd >> readBool   >> box)
      elif  t = typeof<int64>   then   8,(snd >> readInt64  >> box)
      elif  t = typeof<uint64>  then   8,(snd >> readUInt64 >> box)
      elif  t = typeof<string>  then 255,(       readString >> box)
      elif  t = typeof<byte[]>  then 255,(       readBytes  >> box)
                                else invalidOp "Invalid data type"
    let getter (size,buffer) =
      let size' = nativeint size
      match zmq.getsockopt.Invoke(socket.SocketPtr,(int)opts,buffer,size') with
      | 0 -> downcast read (size,buffer)
      | _ -> new ZException() |> raise
    useBuffer size getter

  /// Sets the given option value for the given Socket
  [<Extension;CompiledName("SetOption")>]
  let setOption (socket:Socket) (opts: ZSocketOption,value:'t) =
    let size,write =
      match box value with
      | :? (int32 ) as v  -> sizeof<Int32>,(writeInt32  v)
      | :? (bool  ) as v  -> sizeof<Int32>,(writeBool   v)
      | :? (int64 ) as v  -> sizeof<Int32>,(writeInt64  v)
      | :? (uint64) as v  -> sizeof<Int64>,(writeUInt64 v)
      | :? (string) as v  -> v.Length     ,(writeString v)
      | :? (byte[]) as v  -> v.Length     ,(writeBytes  v)
      | _                 -> invalidOp "Invalid data type"
    let setter (size,buffer) =
      write buffer
      let size' = int size
      let okay = zmq.setsockopt.Invoke(socket.SocketPtr,(int)opts,buffer,size')
      if  okay <> 0 then new ZException() |> raise 
    useBuffer size setter

  /// Sets the given block of option values for the given Socket
  [<Extension;CompiledName("Configure")>]
  let configure socket socketOptions =
    Seq.iter (fun (opt:ZSocketOption * obj) -> setOption socket opt) socketOptions

  (* subscriptions *)

  /// Adds one subscription for each of the given topics
  [<Extension;CompiledName("Subscribe")>]
  let subscribe socket topics =
    Seq.iter (fun (t:byte[]) -> setOption socket (ZMQ.SUBSCRIBE,t)) topics

  /// Removes one subscription for each of the given topics
  [<Extension;CompiledName("Unsubscribe")>]
  let unsubscribe socket topics =
    Seq.iter (fun (t:byte[]) -> setOption socket (ZMQ.UNSUBSCRIBE,t)) topics

    (* message sending *)

  /// Sends a frame, with the given flags, returning true (or false)
  /// if the send was successful (or should be re-tried)
  [<Extension;CompiledName("TrySend")>]
  let trySend (socket:Socket) (frame:byte[]) (flags: ZSocketFlags) : bool =
    let mutable err = ZError.None
    match socket.Send(new ZFrame(frame), flags, &err) with
      | true  -> true
      | false -> new ZException(err) |> raise

  /// Sends a frame (blocking), indicating no more frames will follow
  [<Extension;CompiledName("Send")>]
  let send socket frame = 
    Message.waitForOkay (fun () -> trySend socket frame ZMQ.WAIT)

  /// Sends a frame (blocking), indicating more frames will follow, and returning the given socket
  [<Extension;CompiledName("SendMore")>]
  let sendMore socket frame : Socket =
    Message.waitForOkay (fun () -> trySend socket frame (ZMQ.WAIT ||| ZMQ.SNDMORE))
    socket

  /// Operator equivalent to `Socket.send`
  let (<<|) socket = send socket
  /// Operator equivalent to `Socket.sendMore`
  let (<~|) socket = sendMore socket

  /// Operator equivalent to `Socket.send` (with arguments reversed)
  let (|>>) data socket = socket <<| data
  /// Operator equivalent to `Socket.sendMore` (with arguments reversed)
  let (|~>) data socket = socket <~| data

  /// Sends all frames of a given message
  /// If message is empty, sends a single empty frame for convenience
  [<Extension;CompiledName("SendAll")>]
  let sendAll socket message =
    let len = Seq.length message
    match len with 
    | 0 -> send socket Array.empty
    | 1 -> send socket (Seq.exactlyOne message)
    | _ -> message
           |> Seq.take (len - 1)
           |> Seq.fold sendMore socket
           |> (fun socket -> send socket (Seq.last message))

(* message receiving *)

  /// Gets the next available frame from a socket, returning a frame option
  /// where None indicates the operation should be re-attempted
  [<Extension;CompiledName("TryRecv")>]
  let tryRecv (socket:Socket) length flags =
    let mutable err = ZError.None
    let buffer = Array.zeroCreate length
    match socket.ReceiveBytes(buffer, 0, length, flags, &err) with
    | Message.Okay -> Some(buffer)
    | Message.Busy -> None
    | Message.Fail -> new ZException(err) |> raise

  /// Gets the next available frame from a socket, 
  /// returning false if the operation should be re-attempted
  ///
  /// This function is named TryRecv in compiled assemblies.
  /// If you are accessing the function from a language other than F#, or through reflection, use this name.
  [<Extension;CompiledName("TryRecv")>]
  let tryRecvInto socket length flags ([<Out>]frame:byref<byte[]>) =
    match tryRecv socket length flags with
    | Some buffer ->  frame <- buffer
                      true
    | None        ->  false

  /// Waits for (and returns) the next available frame from a socket
  /// If no message is received before RCVTIMEO expires, throws a TimeoutException
  [<Extension;CompiledName("Recv")>]
  let recv socket =
    use msg = new Message ()
    Message.recv msg socket
    Message.data msg

  /// Returns true if more message frames are available
  [<Extension;CompiledName("RecvMore")>]
  let recvMore socket = getOption<bool> socket ZMQ.RCVMORE

  /// Retrieves all frames of the next available message
  [<Extension;CompiledName("RecvAll")>]
  let recvAll socket  =
    [|  yield socket |> recv
        while socket |> recvMore do yield socket |> recv  |]

  (* monitoring *)

  /// Creates a `ZMQ.PAIR` socket, bound to the given address, which broadcasts
  /// events for the given socket. These events should be consumed by another `ZMQ.PAIR` socket
  /// connected to the given address (preferably on a background thread).
  [<Extension;CompiledName("Monitor")>]
  let monitor (socket:Socket) address events =
    let mutable err = ZError.None
    let good = ZMonitors.Monitor(socket, address, events, &err)
    if not good then
      new ZException(err) |> raise
