namespace Iris.Web

open Suave
open Suave.Http;
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types
open Suave.Web
open System.Threading

type AppServer(addr : string, port : int, dir : string) =
  let tag = "[AppServer] "
  let cts = new CancellationTokenSource()

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
    choose [ GET >>= choose [ path "/" >>= browseFileHome "index.html"
                              browseHome ] ]

  let config =
    { defaultConfig with cancellationToken = cts.Token
                         homeFolder        = Some(dir)
                         bindings          = [ HttpBinding.mk' HTTP addr port ]
                         mimeTypesMap      = mimeTypes }

  let Debug(it : string) = ()

  let thread = new Thread(new ThreadStart(fun _ ->
    try
        Debug(tag + "starting..")
        startWebServer config app
        Debug(tag + "stopped.")
    with
        | :? System.OperationCanceledException -> ()
        | ex -> Debug("could not start AppServer: " + ex.Message)))

  member this.Start() : unit =
    thread.Start()
    Debug(tag + "done.")

  member this.Dispose()  : unit =
    Debug(tag + "quitting...")
    cts.Cancel()
    cts.Dispose();
    Debug(tag + "done.")
