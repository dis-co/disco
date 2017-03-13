namespace Iris.Nodes

// * Imports

open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Core.Logging

// * Constants

[<RequireQualifiedAccess>]
module Settings =

  [<Literal>]
  let NODES_CATEGORY = "Iris"

// * Util

[<RequireQualifiedAccess>]
module Util =

  let inline isNullReference (o: 't) =
    obj.ReferenceEquals(o, null)

  let inline log< ^t when ^t : (member Logger: ILogger) and ^t : (member InDebug: ISpread<bool>)> (state: ^t) (level: LogType) (msg: string) : unit =
    let logger = (^t : (member Logger: ILogger) state)
    logger.Log(level, msg)

  let inline debug (state: ^t) (msg: string) =
    let debug = (^t : (member InDebug: ISpread<bool>) state)
    if debug.[0] then
      log state LogType.Debug msg

  let inline error (state: ^t) (msg: string) =
    log state LogType.Error msg
