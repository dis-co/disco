namespace Iris.Service.Types

open Suave
open Suave.Http;
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Web
open System.Threading
open System.IO
open System.Net
open System.Net.Sockets
open System.Diagnostics


[<AutoOpen>]
module AssetServer =

  type AssetServer(addr : string, port : int) =
    let cts = new CancellationTokenSource()

    let port = Sockets.Port.Parse (string port)
    let ipAddr = IPAddress.Parse addr

    let basepath =
      let fn = Process.GetCurrentProcess().MainModule.FileName
      Path.GetDirectoryName(fn) + "/assets"

    // Add more mime-types here if necessary
    // the following are for fonts, source maps etc.
    let mimeTypes = defaultMimeTypesMap

    // our application only needs to serve files off the disk
    // but we do need to specify what to do in the base case, i.e. "/"
    let app =
      choose [ GET >=> choose [ path "/"      >=> OK "should serve index"
                                path "/tests" >=> OK "should serve tests page"
                                browseHome ] ]

    let config =
      { defaultConfig with cancellationToken = cts.Token
                           homeFolder        = Some(basepath)
                           bindings          = [ HttpBinding.mk HTTP ipAddr port ]
                           mimeTypesMap      = mimeTypes }

    let thread = new Thread(new ThreadStart(fun _ ->
      try startWebServer config app
      with
        | :? System.OperationCanceledException -> ()
        | ex -> printfn "Exception: %s" ex.Message))

    member this.Dispose() : unit =
      cts.Cancel ()
      cts.Dispose ()

    member this.Start() : unit =
      thread.Start ()
