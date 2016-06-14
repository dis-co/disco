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

/// Contains methods for working with Message instances
[<Extension;CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Message =

  (* message options *)

  /// Gets the value of the given option for the given Message
  [<Extension;CompiledName("GetOption")>]
  let getOption (message:Message) (messageOption: ZFrameOption) =
    message.GetOption(messageOption)

  /// Sets the given option value for the given Message
  [<Extension;CompiledName("SetOption")>]
  let setOption (message:Message) (messageOption,value) =
    let result = zmq.msg_set.Invoke(message.DataPtr(),messageOption, value)
    if result <> 0 then
      new ZException(result)
      |> raise

  /// Sets the given block of option values for the given Message
  [<Extension;CompiledName("Configure")>]
  let configure message options =
    Seq.iter (fun (input:int * int) -> setOption message input) options

  /// Returns the content of the given Message
  [<Extension;CompiledName("Data")>]
  let data (message:Message) =
    message.Read()

  /// Returns the size (in bytes) of the given Message
  [<Extension;CompiledName("Size")>]
  let size (message:Message) =
    int message.Length

  /// Returns true if the given message is a frame in a multi-part message and more frames are available
  [<Extension;CompiledName("HasMore")>]
  let hasMore (message:Message) =
    zmq.msg_more.Invoke(message.DataPtr()) |> bool

  /// Tests if two `Message` instances have the same size and data
  [<Extension;CompiledName("IsMatch")>]
  let isMatch left right = (size left = size right) && (data left = data right)

  /// For a given message, returns the metadata associated with the given name
  /// as an `Option<string>` where `None` indicates no metadata is present
  [<Extension;CompiledName("TryGetMetadata")>]
  let tryGetMetadata (message:Message) name =
    match ZMQ.version with
    | Version (m,n,_) 
      when m >= 4 && n >= 1 ->  match zmq.msg_gets.Invoke(message.DataPtr(),name) with
                                |   0n -> None
                                | addr -> Some (Marshal.PtrToStringAnsi addr)
    | _                     ->  None

  /// For a given message, extracts the metadata associated with the given name,
  /// returning false if no metadata is present (for the given name)
  ///
  /// This function is named TryGetMetadata in compiled assemblies.
  /// If you are accessing the function from a language other than F#, or through reflection, use this name.
  [<Extension;CompiledName("TryGetMetadata")>]
  let tryLoadMetadata message name ([<Out>]value:byref<string>) =
    match tryGetMetadata message name with
    | Some prop ->  value <- prop
                    true
    | None      ->  false

(* message manipulation *)

  /// <summary>
  /// Copies the content from one message to another message.
  /// <para> Avoid modifying message content after a message has been copied,
  /// as this can result in undefined behavior.</para>
  /// </summary>
  [<Extension;CompiledName("Copy")>]
  let copy (source:Message) (target:Message) =
    if source = target then
      new ZException(ZError.EINVAL,"Can not copy message to itself")
      |> raise
    source.CopyZeroTo(target)

  /// <summary>
  /// Moves the content from one message to another message.
  /// <para> No actual copying of message content is performed, target is simply updated to reference the new content.
  /// source becomes an empty message after calling `Message.move()`. The original content of target, if any,
  /// shall be released. To preseve the content of source, see `Message.copy()`.</para>
  /// </summary>
  [<Extension;CompiledName("Move")>]
  let move (source:Message) (target:Message) =
    if source = target then
      new ZException(ZError.EINVAL,"Can not move message to itself")
      |> raise
    source.MoveZeroTo(target)

  /// Makes a new instance of the Message type, with an independent copy of the source content.
  [<Extension;CompiledName("Clone")>]
  let clone (source:Message) =
    let target = new Message()
    copy source target
    target

  (* message sending *)
  let internal (|Okay|Busy|Fail|) = function
    | -1  ->
      let eagain = ZError.EAGAIN.Number
      match zmq.errno.Invoke() with
              | err when err = eagain -> Busy
              | _                     -> Fail
    | _   ->  Okay

  let internal waitForOkay fn =
    let rec loop ()  =
      match fn () with
      | true  -> ((* okay *))
      | false -> loop ()
    loop ()

  /// Sends a message, with the given flags, returning true (or false)
  /// if the send was successful (or should be re-tried)
  [<Extension;CompiledName("TrySend")>]
  let trySend (message:Message) (socket:Socket) (flags: ZSocketFlags) : bool =
    let mutable err = ZError.None
    socket.SendFrame(message, flags, &err)

  /// Sends a message, indicating no more messages will follow
  [<Extension;CompiledName("Send")>]
  let send message socket  = 
    waitForOkay (fun () -> trySend message socket ZSocketFlags.None)

  /// Sends a message, indicating more messages will follow
  [<Extension;CompiledName("SendMore")>]
  let sendMore message socket  = 
    waitForOkay (fun () -> trySend message socket ZSocketFlags.More)

  /// Operator equivalent to `Message.send`
  let (<<-) socket message = send message socket
  /// Operator equivalent to `Message.sendMore`
  let (<<+) socket message = sendMore socket message

  /// Operator equivalent to `Message.send` (with arguments reversed)
  let (->>) message socket = socket <<- message
  /// Operator equivalent to `Message.sendMore` (with arguments reversed)
  let (+>>) message socket = socket <<+ message

  (* message receiving *)

  /// Updates the given `Message` instance with the next available message from a socket, 
  /// returning true (or false) if the recv was successful (or should be re-tried)
  [<Extension;CompiledName("TryRecv")>]
  let tryRecv (message:Message) (socket:Socket) flags =
    let mutable err = ZError.None
    let msg = socket.ReceiveFrame(flags, &err) :?> Message

    match err.Number with
      | err when err = ZError.None.Number -> 
        copy msg message
        true
      | other -> new ZException(other) |> raise
        
  /// Updates the given `Message` instance with the next available message from a socket;
  /// If no message is received before RCVTIMEO expires, throws a TimeoutException
  [<Extension;CompiledName("Recv")>]
  let recv message socket =
    if not <| tryRecv message socket ZSocketFlags.None then
      raise <| TimeoutException ()

  /// Operator equivalent to `Message.recv`
  let (|<<) message socket = recv message socket

  /// Operator equivalent to `Message.recv` (with arguments reversed)
  let (>>|) socket message = message |<< socket
