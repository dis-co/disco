(* ------------------------------------------------------------------------
This file is part of fszmq.

This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
------------------------------------------------------------------------ *)
namespace fszmq

open System
open System.Runtime.InteropServices

open ZeroMQ
open ZeroMQ.lib


/// Provides a memory-managed wrapper over ZMQ message operations
type Message() as this =
  inherit ZFrame()
  
  let mutable disposed  = false
       
  /// Creates a new Message from the given byte array
  new (source: byte array) = new Message (source)

  /// Creates a new Message from the given string
  new (source: string) = new Message (source)

  override __.ToString () =
    sprintf "Message (%i)" <| this.DataPtr()

  override __.Finalize() =
    if not disposed then
      disposed <- true
      this.Dispose()

/// An abstraction of an asynchronous message queue,
/// with the exact queuing and message-exchange
/// semantics determined by the socket type
[<Sealed>]
type Socket internal(context,socketType) =
  inherit ZSocket(context,socketType)
  
  let mutable disposed  = false

  override this.ToString () = sprintf "Socket(%A)" this

  override this.Finalize() =
    if not disposed then
      disposed <- true
      this.Dispose()

/// Represents the container for a group of sockets in a node
[<Sealed>]
type Context() = 
  inherit ZContext()

  let mutable disposed  = false

  override this.ToString () = sprintf "Context(%A)" this 

  override this.Finalize() =
    if not disposed then
      disposed <- true
      this.Dispose()
