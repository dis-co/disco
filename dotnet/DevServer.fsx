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

let npmDir = __SOURCE_DIRECTORY__ + "/node_modules"

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

let locate dir str =
  noCache >=> file (dir + str)

// our application only needs to serve files off the disk
// but we do need to specify what to do in the base case, i.e. "/"
let app =
  choose [
    GET >=> choose [
      path "/"                     >=> locate assetsDir "/index.html"
      path "/tests"                >=> locate assetsDir "/tests.html"
      path "/js/iris.js"           >=> locate baseDir   "/bin/iris.js"
      path "/js/iris.js.map"       >=> locate baseDir   "/bin/iris.js.map"
      path "/js/worker.js"         >=> locate baseDir   "/bin/worker.js"
      path "/js/worker.js.map"     >=> locate baseDir   "/bin/worker.js.map"
      path "/js/iris.tests.js"     >=> locate baseDir   "/bin/iris.tests.js"
      path "/js/iris.tests.js.map" >=> locate baseDir   "/bin/iris.tests.js.map"

      path "/css/mocha.css"       >=> locate npmDir "/mocha/mocha.css"
      path "/js/mocha.js"         >=> locate npmDir "/mocha/mocha.js"

      path "/js/expect.js"      >=> locate npmDir "/expect.js/index.js"
      path "/js/virtual-dom.js" >=> locate npmDir "/virtual-dom/dist/virtual-dom.js"

      browseHome ] ]

let ip = IPAddress.Parse "127.0.0.1"
let port = Sockets.Port.Parse "3000"

let config =
  { defaultConfig
    with homeFolder   = Some assetsDir
         bindings     = [ HttpBinding.mk HTTP ip port ]
         mimeTypesMap = mimeTypes }

startWebServer config app
