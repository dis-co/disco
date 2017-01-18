namespace Iris.Client

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Client
open Iris.Zmq
open Iris.Serialization.Api

// * ApiClient module

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<AutoOpen>]
module ApiClient =

  // ** tag

  let private tag (str: string) = sprintf "ApiClient.%s" str

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<int, IObserver<ClientEvent>>

  // ** Server

  [<NoComparison;NoEquality>]
  type private Server =
    { Meta: IrisServer
      Socket: Req }

  // ** ApiClient module

  [<RequireQualifiedAccess>]
  module ApiClient =

    // ** create

    let create () = failwith "never"
