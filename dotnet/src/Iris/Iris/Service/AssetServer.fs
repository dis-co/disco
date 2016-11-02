namespace Iris.Service

open Suave
open Suave.Http;
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Logging
open Suave.Logging.Loggers
open Suave.Web

open System.Threading
open System.IO
open System.Net
open System.Net.Sockets
open System.Diagnostics

open Iris.Core

type FileName = string

type AssetServer(?config: IrisConfig) =
  let [<Literal>] defaultIP = "127.0.0.1"
  let [<Literal>] defaultPort = "8080"
  let cts = new CancellationTokenSource()

  let noCache =
    setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
    >=> setHeader "Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "0"

  let locate dir str =
    noCache >=> file (dir </> str)

  let basePath = Path.GetFullPath(".") </> "assets" </> "frontend"

  let widgetPath = basePath </> "widgets"

  let listFiles (path: FilePath) : FileName list =
    DirectoryInfo(widgetPath).EnumerateFiles()
    |> Seq.map (fun file -> file.Name)
    |> Seq.toList

  let importStmt (name: FileName) =
    sprintf """<link rel="import" href="widgets/%s" />""" name

  let indexHtml () =
    listFiles widgetPath
    |> List.map importStmt
    |> List.fold (+) ""
    |> sprintf """
<!doctype html>
<!--
 ___ ____  ___ ____
|_ _|  _ \|_ _/ ___|
 | || |_) || |\___ \
 | ||  _ < | | ___) |
|___|_| \_\___|____/ © Nsynk GmbH, 2015

-->
<html lang="en">
  <head>
    <title>Iris</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1" />
    <script data-main="react/main" src="js/require.min.js"></script>
    <script src="js/react.min.js"></script>
    <script src="js/react-dom.min.js"></script>
    <script src="js/radium.min.js"></script>
    <!--<script src="node_modules/virtual-dom/dist/virtual-dom.js"></script>-->

    %s

    <link href="css/iris.css" rel="stylesheet" />
  </head>
  <body>
    <header>Iris</header>

    <nav><a href="/">Home</a></nav>

    <main>
      <article>Content</article>
      <aside>
        <p>More information</p>
      </aside>
    </main>

    <footer>© 2016 Nsynk GmbH</footer>
    <!--<script type="text/javascript" src="js/iris.js"></script>--->
    <div id="app"></div>
  </body>
</html>
"""

  // Add more mime-types here if necessary
  // the following are for fonts, source maps etc.
  let mimeTypes = defaultMimeTypesMap

  // our application only needs to serve files off the disk
  // but we do need to specify what to do in the base case, i.e. "/"
  let app =
    choose [
      GET >=> choose [
        path "/" >=> Successful.OK(indexHtml ())
        browseHome
      ]
    ]

  let appConfig: SuaveConfig =
    either {
      try
        let! addr, port =
          match config with
          | Some config ->
            either {
              let! nid = Config.getNodeId ()
              let! node = Config.findNode config nid
              return string node.IpAddr, string node.WebPort
            }
          | None ->
            either { return defaultIP, defaultPort }

        let addr = IPAddress.Parse addr
        let port = Sockets.Port.Parse port

        printfn "Suave Web Server ready to start on: %A:%A" addr port

        return
          { defaultConfig with
              logger            = ConsoleWindowLogger(Suave.Logging.LogLevel.Info)
              cancellationToken = cts.Token
              homeFolder        = Some(basePath)
              bindings          = [ HttpBinding.mk HTTP addr port ]
              mimeTypesMap      = mimeTypes }
      with
        | exn ->
          return!
            exn.Message
            |> Other
            |> Error.exitWith
    }
    |> Error.orExit id

  let thread = new Thread(new ThreadStart(fun _ ->
    try startWebServer appConfig app
    with
      | :? System.OperationCanceledException -> ()
      | ex -> printfn "Exception: %s" ex.Message))

  member this.Start() : unit =
    thread.Start ()

  interface System.IDisposable with
    member this.Dispose() : unit =
      cts.Cancel ()
      cts.Dispose ()
