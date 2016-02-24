#I @"./src/Iris.Web/bin/Debug"
#r @"./packages/Suave/lib/net40/Suave.dll"
#r @"./src/Iris.Web/bin/Debug/WebSharper.Core.JavaScript.dll"
#r @"./src/Iris.Web/bin/Debug/WebSharper.JavaScript.dll"
#r @"./src/Iris.Web/bin/Debug/WebSharper.Core.dll"
#r @"./src/Iris.Web/bin/Debug/Iris.Core.dll"
#r @"./src/Iris.Web/bin/Debug/Iris.Web.Core.dll"

open Suave
open Suave.Http;
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Web
open System.Net
open System.Net.Sockets
open Iris.Web.Views

// Add more mime-types here if necessary
// the following are for fonts, source maps etc.
let mimeTypes = defaultMimeTypesMap

let index =
  Index.compileIndex (__SOURCE_DIRECTORY__ + "/src/Iris.Web/bin/Debug/assets/js")

let noCache = 
  setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
  >=> setHeader "Need-Help" "k@ioct.it"
  >=> setHeader "Pragma" "no-cache"
  >=> setHeader "Expires" "0"

// our application only needs to serve files off the disk
// but we do need to specify what to do in the base case, i.e. "/"
let app =
  choose [ GET >=> choose [ path "/"                   >=> noCache >=> OK index
                            path "/tests"              >=> noCache >=> file (__SOURCE_DIRECTORY__ + "/src/Iris.Web.Tests/index.html")
                            path "/Iris.Web.Worker.js" >=> noCache >=> file (__SOURCE_DIRECTORY__ + "/src/Iris.Web.Worker/bin/Debug/assets/Iris.Web.Worker.js")
                            path "/Iris.Web.Tests.js"  >=> noCache >=> file (__SOURCE_DIRECTORY__ + "/src/Iris.Web.Tests/bin/Debug/assets/Iris.Web.Tests.js")
                            browseHome ] ]
let ip = IPAddress.Parse "127.0.0.1"
let port = Sockets.Port.Parse "3000"

let config =
  { defaultConfig
    with homeFolder = Some(__SOURCE_DIRECTORY__ + "/src/Iris.Web/bin/Debug/assets")
         bindings   = [ HttpBinding.mk HTTP ip port ]
         mimeTypesMap = mimeTypes }

startWebServer config app
