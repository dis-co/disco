#I @"./src/Iris/bin/Debug"
#r @"./packages/Suave/lib/net40/Suave.dll"

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

let baseDir = __SOURCE_DIRECTORY__ + "/src/Iris"

let assetsDir = baseDir + "/assets/frontend"

// Add more mime-types here if necessary
// the following are for fonts, source maps etc.
let mimeTypes = defaultMimeTypesMap

let noCache = 
  setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
  >=> setHeader "Need-Help" "k@ioct.it"
  >=> setHeader "Pragma" "no-cache"
  >=> setHeader "Expires" "0"

// our application only needs to serve files off the disk
// but we do need to specify what to do in the base case, i.e. "/"
let app =
  choose [
    GET >=> choose [
      path "/"                    >=> noCache >=> file (assetsDir + "/index.html")
      path "/tests"               >=> noCache >=> file (assetsDir + "/tests.html")
      path "/js/iris.js"          >=> noCache >=> file (baseDir   + "/bin/iris.js")
      path "/js/iris.js.map"      >=> noCache >=> file (baseDir   + "/bin/iris.js.map")
      path "/js/worker.js"        >=> noCache >=> file (baseDir   + "/bin/worker.js")
      path "/js/worker.js.map"    >=> noCache >=> file (baseDir   + "/bin/worker.js.map")
      path "/js/web.tests.js"     >=> noCache >=> file (baseDir   + "/bin/web.tests.js")
      path "/js/web.tests.js.map" >=> noCache >=> file (baseDir   + "/bin/web.tests.js.map")
      browseHome ] ]

let ip = IPAddress.Parse "127.0.0.1"
let port = Sockets.Port.Parse "3000"

let config =
  { defaultConfig
    with homeFolder   = Some assetsDir
         bindings     = [ HttpBinding.mk HTTP ip port ]
         mimeTypesMap = mimeTypes }

startWebServer config app
