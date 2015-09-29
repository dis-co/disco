namespace Iris.Service.AssetServer

open Suave
open Suave.Http;
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types
open Suave.Web
open System.Threading
open System.IO
open System.Diagnostics

open Iris.Web.Index
open Iris.Web.Build

type AssetServer(addr : string, port : int) =
  let cts = new CancellationTokenSource()

  let basepath =
    let fn = Process.GetCurrentProcess().MainModule.FileName
    Path.GetDirectoryName(fn) + "/assets"

  let index = compileIndex (basepath + "/js/")
  let js = compileJS Array.empty

  // Add more mime-types here if necessary
  // the following are for fonts, source maps etc.
  let mimeTypes =
    defaultMimeTypesMap >=> (function
    | ".map"   -> mkMimeType "text/plain"                    false
    | ".svg"   -> mkMimeType "image/svg+xml"                 false
    | ".otf"   -> mkMimeType "application/font-sfnt"         false
    | ".eot"   -> mkMimeType "application/vnd.ms-fontobject" false
    | ".woff"  -> mkMimeType "application/font-woff"         false
    | ".woff2" -> mkMimeType "application/font-woff"         false
    | _ -> None)

  // our application only needs to serve files off the disk
  // but we do need to specify what to do in the base case, i.e. "/"
  let app =
    choose [ GET >>= choose [ path "/" >>= OK index
                              browseHome ] ]

  let config =
    { defaultConfig with cancellationToken = cts.Token
                         homeFolder        = Some(basepath)
                         bindings          = [ HttpBinding.mk' HTTP addr port ]
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
