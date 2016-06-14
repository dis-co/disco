(* ------------------------------------------------------------------------
This file is part of fszmq.

This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
------------------------------------------------------------------------ *)
namespace fszmq

open System
open System.Globalization
open System.Runtime.InteropServices

open ZeroMQ
open ZeroMQ.Monitoring
open ZeroMQ.lib

/// Report the version of the underlying (native) ZMQ library
[<StructuredFormatDisplay("{Text}")>]
type Version = 
  /// Components of native versioning info
  | Version of major:int * minor:int * revision:int
  /// Unable to determine versioning info
  | Unknown

  // textual representation of Verison
  member private V.Text = match V with
                          | Version(m,n,b) -> sprintf "%i.%i.%i" m n b
                          | Unknown        -> "<unknown>"

  override V.ToString() = V.Text


/// Report if the underlying (native) ZMQ library supports a named capability
[<StructuredFormatDisplay("{Text}")>]
type Capability = 
  /// Test passed; `yesOrNo` is true when `name` is supported
  | Supported of name:string * yesOrNo:bool
  /// Test for capability failed; support status is unknown
  | Unknown

  // textual representation of Capability
  member private V.Text = match V with
                          | Supported (name,ok) -> sprintf "%s = %b" name ok
                          | Unknown             -> "<unknown>"

  override V.ToString() = V.Text


/// Represents any error raised by the native ZMQ library,
/// with a human-readable summary in the Message property
type ZMQError internal(errnum,errmsg) =
  inherit Exception(errmsg)

  /// the ZeroMQ-defined, or OS-defined, error code
  member __.ErrorNumber = errnum


/// Contains commonly-used pre-defined ZMQ values
[<RequireQualifiedAccess>]
module ZMQ =

  /// Version of the underlying (native) ZMQ library
  [<CompiledName("Version")>]
  let version =
    try
      let mutable major,minor,patch = 0,0,0
      zmq.version.Invoke(&major,&minor,&patch)
      match (major,minor,patch) with
      | 0,0,0 -> Version.Unknown
      | m,n,b -> Version(m,n,b)
    with
    | _ -> Version.Unknown


  /// Tests if the underlying (native) ZMQ library supports a given capability
  [<CompiledName("Has")>]
  let has (capability: int) =
    try
      Supported (string capability,zmq.has.Invoke(nativeint capability) = 1)
    with
      | _ -> Capability.Unknown

  (* capabilities *)
  /// Used to test if library supports the IPC transport protocol
  let [<Literal>] CAP_IPC     = "ipc"
  /// Used to test if library supports the PGM transport protocol
  let [<Literal>] CAP_PGM     = "pgm"
  /// Used to test if library supports the TIPC transport protocol
  let [<Literal>] CAP_TIPC    = "tipc"
  /// Used to test if library supports the NORM transport protocol
  let [<Literal>] CAP_NORM    = "norm"
  /// Used to test if library supports the CURVE security mechanism
  let [<Literal>] CAP_CURVE   = "curve"
  /// Used to test if library supports the GSSAPI security mechanism
  let [<Literal>] CAP_GSSAPI  = "gssapi"


  // (* error codes *)
  // let [<Literal>] internal EINTR        =  4
  // let [<Literal>] internal EFAULT       = 14 
  // let [<Literal>] internal POSIX_EAGAIN = 11
  // let [<Literal>] internal BSD_EAGAIN   = 35
  // // !!! HACK !!! This whole setup is bad and wrong and should be replaced
  // let internal eagain =
  //   try
  //     let mutable info = lib.Platform.e
  //     C.uname (&info) |> ignore //TODO: handle this better
  //     match info.sysname.ToLowerInvariant () with
  //     | "linux"   -> POSIX_EAGAIN  // Linux
  //     | "darwin"  -> BSD_EAGAIN   // Mac OS X
  //     //NOTE: this assumes all Unixes are BSD-derived, which is bad and wrong
  //     | _         -> BSD_EAGAIN
  //     //TODO: extend this to include other OSes
  //   with
  //     | _ -> POSIX_EAGAIN  // Windows
  //   (* :: NOTE ::
  //   if _anything_ goes wrong, we assume "libc::uname" doesn't exist (i.e. we're on Windows);
  //   this is probably bad and wrong and really ought to be replaced with _something_ else.*)

  // helper function for build native-to-managed errors
  let inline internal buildError (num: int) = new ZError(num)

  // constructs and raises native-to-managed errors
  let inline internal error() =
    new ZException(zmq.errno.Invoke()) |> raise

  // helpers for "faking" native errors
  let inline internal einval msg = new ZException(22,msg) |> raise

  // /// Non-blocking mode was requested and the message cannot be sent at the moment
  // let (|EAGAIN|_|) errno =
  //   if errno = eagain then Some () else None

  
  (* message size *)
  let internal ZMQ_MSG_T_SIZE = match version with
                                | Version(m,n,_) when m >= 4 && n > 0 -> 64
                                | _                                   -> 32

  (* context options *)
  /// (Int32) Set number of OS-level I/O threads
  let [<Literal>] IO_THREADS          =  ZContextOption.IO_THREADS
  /// (Int32) Set maximum number of sockets for a context
  let [<Literal>] MAX_SOCKETS         =  ZContextOption.MAX_SOCKETS
  /// (Int32) Get largest configurable number of sockets
  let [<Literal>] SOCKET_LIMIT        =  ZContextOption.SOCKET_LIMIT
  /// (Int32) Change thread scheduling priority (only valid on systems which use pthread)
  let [<Literal>] THREAD_PRIORITY     =  ZContextOption.THREAD_PRIORITY
  /// (Int32) Set thread scheduling policy (only valid on systems which use pthread)
  let [<Literal>] THREAD_SCHED_POLICY =  ZContextOption.THREAD_SCHED_POLICY

  (* default for new contexts *)
  /// Default number of OS-level I/O threads (1)
  let [<Literal>] IO_THREADS_DFLT           =    1
  /// Default maximum number of sockets (1024)
  let [<Literal>] MAX_SOCKETS_DFLT          = 1024
  /// Default thread scheduling priority (-1)
  let [<Literal>] THREAD_PRIORITY_DFLT      = -1
  /// Default thread scheduling policy (-1)
  let [<Literal>] THREAD_SCHED_POLICY_DFLT  = -1

  (* event codes *)
  let internal EVENT_DETAIL_SIZE = sizeof<uint16> + sizeof<int32>

  /// Socket connection established
  let [<Literal>] EVENT_CONNECTED       = ZMonitorEvents.Connected
  /// Synchronous connection failed; socket is being polled
  let [<Literal>] EVENT_CONNECT_DELAYED = ZMonitorEvents.ConnectDelayed
  /// Asynchronous (re)connection attempt
  let [<Literal>] EVENT_CONNECT_RETRIED = ZMonitorEvents.ConnectRetried
  /// Socket bound to address; ready to accept connections
  let [<Literal>] EVENT_LISTENING       = ZMonitorEvents.Listening
  /// Socket could not bind to address
  let [<Literal>] EVENT_BIND_FAILED     = ZMonitorEvents.BindFailed
  /// Connection accepted to bound interface
  let [<Literal>] EVENT_ACCEPTED        = ZMonitorEvents.Accepted
  /// Could not accept client connection
  let [<Literal>] EVENT_ACCEPT_FAILED   = ZMonitorEvents.AcceptFailed
  /// Socket connection closed
  let [<Literal>] EVENT_CLOSED          = ZMonitorEvents.Closed
  /// Connection could not be closed (only for ipc transport)
  let [<Literal>] EVENT_CLOSE_FAILED    = ZMonitorEvents.CloseFailed
  /// Broken session (specific to ipc and tcp transports)
  let [<Literal>] EVENT_DISCONNECTED    = ZMonitorEvents.Disconnected
  /// Event monitoring has been disabled
  let [<Literal>] EVENT_MONITOR_STOPPED = ZMonitorEvents.Stopped
  /// Monitor all possible events
  let [<Literal>] EVENT_ALL             = ZMonitorEvents.AllEvents

  (* socket types *)

  /// An exclusive pair of two sockets (primarily for use with inproc transport)
  let [<Literal>] PAIR    =  ZSocketType.PAIR
  /// A publisher which broadcasts topic-prefixed messages
  let [<Literal>] PUB     =  ZSocketType.PUB
  /// A subscribe which receives topic-prefixed messages
  let [<Literal>] SUB     =  ZSocketType.SUB
  /// Makes synchronous requests of a server (i.e. ZMQ.REP, ZMQ.ROUTER), awaits replies
  let [<Literal>] REQ     =  ZSocketType.REQ
  /// Awaits synchronous requests of a client (i.e. ZMQ.REQ, ZMQ.DEALER), makes replies
  let [<Literal>] REP     =  ZSocketType.REP
  /// Participates in asynchronous request/reply exchanges with compatible peers (i.e. ZMQ.REP, ZMQ.DEALER, ZMQ.ROUTER)
  let [<Literal>] DEALER  =  ZSocketType.DEALER
  /// Participates in asynchronous request/reply exchanges with compatible peers (i.e. ZMQ.REQ, ZMQ.DEALER, ZMQ.ROUTER)
  let [<Literal>] ROUTER  =  ZSocketType.ROUTER
  /// Collects messages in a fair-queued fashion from across all upstream (i.e. ZMQ.PUSH) nodes
  let [<Literal>] PULL    =  ZSocketType.PULL
  /// Delivers messages in a round-robin fashion to across all downstream (i.e. ZMQ.PULL) nodes
  let [<Literal>] PUSH    =  ZSocketType.XPUB
  /// A publisher like ZMQ.PUB, but does not automatically receive forwarded topic subscriptions
  let [<Literal>] XPUB    =  ZSocketType.XPUB
  /// A publisher like ZMQ.SUB, but does not automatically forward topic subscriptions
  let [<Literal>] XSUB    =  ZSocketType.XSUB
  /// Exchanges raw data with a non-ZeroMQ peer via the tcp transport
  let [<Literal>] STREAM  =  ZSocketType.STREAM

  (* socket options *)

  /// (UInt64) I/O thread affinity bit-mask
  let [<Literal>] AFFINITY                  =  ZSocketOption.AFFINITY
  /// (Byte[]) Socket identifier
  let [<Literal>] IDENTITY                  =  ZSocketOption.IDENTITY
  /// (Byte[]) Add subscription filter
  let [<Literal>] SUBSCRIBE                 =  ZSocketOption.SUBSCRIBE
  /// (Byte[]) Remove subscription filter
  let [<Literal>] UNSUBSCRIBE               =  ZSocketOption.UNSUBSCRIBE
  /// (Int32) Multicast data rate in kilobits per second
  let [<Literal>] RATE                      =  ZSocketOption.RATE
  /// (Int32) Multicast recovery period in milliseconds
  let [<Literal>] RECOVERY_IVL              =  ZSocketOption.RECOVERY_IVL
  /// (Int32) Send-message buffer size in bytes
  let [<Literal>] SNDBUF                    =  ZSocketOption.SNDBUF
  /// (Int32) Receive-message buffer size in bytes
  let [<Literal>] RCVBUF                    =  ZSocketOption.RCVBUF
  /// (Int32) 1 if more message frames are available, 0 otherwise
  let [<Literal>] RCVMORE                   =  ZSocketOption.RCVMORE
  /// (IntPtr) native file descriptor
  let [<Literal>] FD                        =  ZSocketOption.FD
  /// (Int32) Socket event state, see all: Polling
  let [<Literal>] EVENTS                    =  ZSocketOption.EVENTS
  /// (Int32) Socket type
  let [<Literal>] TYPE                      =  ZSocketOption.TYPE
  /// (Int32) Pause before shutdown in milliseconds
  let [<Literal>] LINGER                    =  ZSocketOption.LINGER
  /// (Int32) Pause before reconnect in milliseconds
  let [<Literal>] RECONNECT_IVL             =  ZSocketOption.RECONNECT_IVL
  /// (Int32) Maximum reconnection interval in milliseconds
  let [<Literal>] RECONNECT_IVL_MAX         =  ZSocketOption.RECONNECT_IVL_MAX
  /// (Int32) Maximum number of queued peers
  let [<Literal>] BACKLOG                   =  ZSocketOption.BACKLOG
  /// (Int64) Maximum inbound message size in bytes
  let [<Literal>] MAXMSGSIZE                =  ZSocketOption.MAX_MSG_SIZE
  /// (Int32) Maximum number of outbound queued messages
  let [<Literal>] SNDHWM                    =  ZSocketOption.SNDHWM
  /// (Int32) Maximum number of inbound queued messages
  let [<Literal>] RCVHWM                    =  ZSocketOption.RCVHWM
  /// (Int32) Time-to-live for each multicast packet in network-hops
  let [<Literal>] MULTICAST_HOPS            =  ZSocketOption.MULTICAST_HOPS
  /// (Int32) Timeout period for inbound messages in milliseconds
  let [<Literal>] RCVTIMEO                  =  ZSocketOption.RCVTIMEO
  /// (Int32) Timeout period for outbound messages in milliseconds
  let [<Literal>] SNDTIMEO                  =  ZSocketOption.SNDTIMEO
  /// (String) Last address bound to endpoint
  let [<Literal>] LAST_ENDPOINT             =  ZSocketOption.LAST_ENDPOINT
  /// (Int32) 1 to error on unroutable messages, 0 to silently ignore
  let [<Literal>] ROUTER_MANDATORY          =  ZSocketOption.ROUTER_MANDATORY
  /// (Int32) Override OS-level TCP keep-alive
  let [<Literal>] TCP_KEEPALIVE             =  ZSocketOption.TCP_KEEPALIVE
  /// (Int32) Override OS-level TCP keep-alive
  let [<Literal>] TCP_KEEPALIVE_CNT         =  ZSocketOption.TCP_KEEPALIVE_CNT
  /// (Int32) Override OS-level TCP keep-alive
  let [<Literal>] TCP_KEEPALIVE_IDLE        =  ZSocketOption.TCP_KEEPALIVE_IDLE
  /// (Int32) Override OS-level TCP keep-alive
  let [<Literal>] TCP_KEEPALIVE_INTVL       =  ZSocketOption.TCP_KEEPALIVE_INTVL
  /// (Int32) 1 to limit queuing to only completed connections, 0 otherwise
  let [<Literal>] IMMEDIATE                 =  ZSocketOption.IMMEDIATE
  /// (Int32) 1 will resend duplicate messages
  let [<Literal>] XPUB_VERBOSE              =  ZSocketOption.XPUB_VERBOSE
  /// (Int32) 1 to enable IPv6 on the socket, 0 to restrict to only IPv4
  let [<Literal>] IPV6                      =  ZSocketOption.IPV6
  /// (Int32) Returns the current security mechanism
  let [<Literal>] MECHANISM                 =  ZSocketOption.MECHANISM
  /// (Int32) 1 to make socket act as server for PLAIN security, 0 otherwise
  let [<Literal>] PLAIN_SERVER              =  ZSocketOption.PLAIN_SERVER
  /// (String) Sets the user name for outgoing connections over TCP or IPC
  let [<Literal>] PLAIN_USERNAME            =  ZSocketOption.PLAIN_USERNAME
  /// (String) Sets the password for outgoing connections over TCP or IPC
  let [<Literal>] PLAIN_PASSWORD            =  ZSocketOption.PLAIN_PASSWORD
  /// (Int32) 1 to make socket act as server for CURVE security, 0 otherwise
  let [<Literal>] CURVE_SERVER              =  ZSocketOption.CURVE_SERVER
  /// (String or Byte[]) sets the long-term public key on a client or server socket
  let [<Literal>] CURVE_PUBLICKEY           =  ZSocketOption.CURVE_PUBLICKEY
  /// (String or Byte[]) sets the long-term secret key on a client socket
  let [<Literal>] CURVE_SECRETKEY           =  ZSocketOption.CURVE_SECRETKEY
  /// (String or Byte[]) sets the long-term server key on a client socket
  let [<Literal>] CURVE_SERVERKEY           =  ZSocketOption.CURVE_SERVERKEY
  /// (Int32) 1 to automatically send an empty message on new connection, 0 otherwise
  let [<Literal>] PROBE_ROUTER              =  ZSocketOption.PROBE_ROUTER
  /// (Int32) 1 to prefix messages with explicit request ID, 0 otherwise
  let [<Literal>] REQ_CORRELATE             =  ZSocketOption.REQ_CORRELATE
  /// (Int32) 1 to relax strict alternation between ZMQ.REQ and ZMQ.REP, 0 otherwise
  let [<Literal>] REQ_RELAXED               =  ZSocketOption.REQ_RELAXED
  /// (Int32) 1 to keep last message in queue (ignores high-water mark options), 0 otherwise
  let [<Literal>] CONFLATE                  =  ZSocketOption.CONFLATE
  /// (String) Sets authentication domain
  let [<Literal>] ZAP_DOMAIN                =  ZSocketOption.ZAP_DOMAIN
  /// (Int32) 0 to reject clients which use an existing identity, 1 to transfer the connection
  let [<Literal>] ROUTER_HANDOVER           =  ZSocketOption.ROUTER_HANDOVER
  /// (Int32) ToS field is typically used to specify a packets priority; 
  /// The availability of this option is dependent on intermediate network equipment
  let [<Literal>] TOS                       =  ZSocketOption.TOS
  /// (Byte[]) Sets the peer ID of the next connected host, and immediately 
  /// readies that connection for data transfer with the named ID
  let [<Literal>] CONNECT_RID               =  ZSocketOption.CONNECT_RID
  /// (Int32) 1 means the socket will act as GSSAPI server; 0 means the socket will act as GSSAPI client
  let [<Literal>] GSSAPI_SERVER             =  ZSocketOption.GSSAPI_SERVER
  /// (String) The name of the pricipal for whom GSSAPI credentials should be acquired
  let [<Literal>] GSSAPI_PRINCIPAL          =  ZSocketOption.GSSAPI_PRINCIPAL
  /// (String) The name of the pricipal of the GSSAPI server to which a GSSAPI client intends to connect
  let [<Literal>] GSSAPI_SERVICE_PRINCIPAL  =  ZSocketOption.GSSAPI_SERVICE_PRINCIPAL
  /// (Int32) 1 means that GSSAPI communication will be plaintext, 0 means communications will be encrypted
  let [<Literal>] GSSAPI_PLAINTEXT          =  ZSocketOption.GSSAPI_PLAINTEXT
  /// (Int32) The maximum handshake interval in milliseconds for the specified socket
  let [<Literal>] HANDSHAKE_IVL             =  ZSocketOption.HANDSHAKE_IVL
  /// (String) SOCKS5 proxy
  let [<Literal>] SOCKS_PROXY               =  ZSocketOption.SOCKS_PROXY
  /// (Int32) 0 drops the message silently when the peers SNDHWM is reached, 1 returns an 'EAGAIN' error code (if ZMQ_DONTWAIT was used)
  let [<Literal>] XPUB_NODROP               =  ZSocketOption.XPUB_NODROP

  (* security mechanisms *)
  /// Indicates there is currently no security mechanism in use
  let [<Literal>] SECURITY_NULL   = 0
  /// Indicates PLAIN security mechanism is currently in use
  let [<Literal>] SECURITY_PLAIN  = 1
  /// Indicates CURVE security mechanism is currently in use
  let [<Literal>] SECURITY_CURVE  = 2
  /// Indicates GSSAPI security mechanism is currently in use
  let [<Literal>] SECURITY_GSSAPI = 3

  (* common values *)

  /// (Int32) the value needed to disable lingering on a socket's outbound queue
  let [<Literal>] NO_LINGER = 0

  (* message options *)

  /// (Int32) 1 if more message frames are available, 0 otherwise
  let [<Literal>] MORE    = ZFrameOption.MORE
  /// (IntPtr) The file descriptor of the socket from which the 'message' was read
  let [<Literal>] SRCFD   = ZFrameOption.SRCFD
  /// (Int32) 1 indicates that a message MAY share underlying storage, 0 otherwise
  let [<Literal>] SHARED  = ZFrameOption.SHARED

  (* transmission options *)

  /// Block thread until message frame is sent
  let [<Literal>] WAIT      =   ZSocketFlags.None
  /// Queue message frame for sending (return immediately)
  let [<Literal>] DONTWAIT  =   ZSocketFlags.DontWait
  /// More message frames will follow the current frame
  let [<Literal>] SNDMORE   =   ZSocketFlags.More

  (* polling *)
  /// no polling 
  let [<Literal>] POLLNONE = ZPoll.None
  /// poll for inbound messages
  let [<Literal>] POLLIN   = ZPoll.In
  /// poll for outbound messages
  let [<Literal>] POLLOUT  = ZPoll.Out
  /// poll for messages on stderr (for use with file descriptors)
  let [<Literal>] POLLERR  = ZPoll.Err

  (* common timeout lengths for polling *)

  /// indicates polling should exit immediately
  let [<Literal>] NOW     =  0L
  /// indicates polling should wait indefinitely
  let [<Literal>] FOREVER = -1L

  (* proxying *)

  /// Command used to temporarily suspend a steerable proxy
  let PAUSE     = "PAUSE"B
  /// Command used to resume a suspended steerable proxy
  let RESUME    = "RESUME"B
  /// Command used to cleanly shutdown a steerable proxy
  let TERMINATE = "TERMINATE"B
