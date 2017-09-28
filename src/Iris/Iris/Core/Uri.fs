namespace Iris.Core

// * Imports

open System
open Iris.Raft

// * Uri

[<RequireQualifiedAccess>]
module Uri =

  // ** Protocol

  type private Protocol =
    | WS
    | WSS
    | TCP
    | UDP
    | PGM
    | IPC
    | EPGM
    | HTTP
    | HTTPS
    | LOCALGIT
    | REMOTEGIT

    override protocol.ToString() =
      match protocol with
      | WS        -> "ws"
      | WSS       -> "wss"
      | TCP       -> "tcp"
      | UDP       -> "udp"
      | PGM       -> "pgm"
      | IPC       -> "ipc"
      | EPGM      -> "epgm"
      | HTTP      -> "http"
      | HTTPS     -> "https"
      | LOCALGIT  -> "git"
      | REMOTEGIT -> "git"

  //  _   _      _
  // | | | |_ __(_)
  // | | | | '__| |
  // | |_| | |  | |
  //  \___/|_|  |_|
  //

  // ** toUri

  let private toUri (proto:  Protocol)
                    (prefix: string option)
                    (path:   string option)
                    (ip:     string)
                    (port:   Port option) =

    match proto, prefix, path, port with
    | LOCALGIT, _, Some path, Some port  -> String.Format("{0}://{1}:{2}/{3}/.git",string proto, ip, port, path)
    | REMOTEGIT, _, Some path, Some port -> String.Format("{0}://{1}:{2}/{3}",string proto, ip, port, path)
    | PGM,  Some prefix, _, Some port
    | EPGM, Some prefix, _, Some port    -> String.Format("{0}://{1};{2}:{3}",string proto, prefix, ip, port)
    | _, _, Some path, None              -> String.Format("{0}://{1}/{2}",string proto, ip, path)
    | _, _, Some path, Some port         -> String.Format("{0}://{1}:{2}/{3}", string proto, ip, port, path)
    | _, _, None, Some port              -> String.Format("{0}://{1}:{2}", string proto, ip, port)
    | _, _, None, None                   -> String.Format("{0}://{1}", string proto, ip)
    |> url

  // ** gitUri

  /// ## gitUri
  ///
  /// Format a git remote string
  ///
  /// ### Signature:
  /// - name: project name
  /// - mem: RaftMember
  ///
  /// Returns: string

  let gitUri (name: Name) (mem: RaftMember) =
    let path =
      #if FABLE_COMPILER
      name
      |> unwrap
      |> Fable.Import.JS.encodeURI
      #else
      name
      |> unwrap
      |> System.Web.HttpUtility.UrlEncode
      #endif
    toUri HTTP None (Some path) (string mem.IpAddr) (mem.GitPort |> Some)
