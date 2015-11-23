#I @"./src/Iris.Web/bin/Debug"
#r @"./packages/Suave/lib/net40/Suave.dll"
#r @"./src/Iris.Web/bin/Debug/WebSharper.Core.JavaScript.dll"
#r @"./src/Iris.Web/bin/Debug/WebSharper.JavaScript.dll"
#r @"./src/Iris.Web/bin/Debug/WebSharper.Core.dll"
#r @"./src/Iris.Web/bin/Debug/Iris.Core.dll"
#r @"./src/Iris.Web/bin/Debug/Iris.Web.Core.dll"

open Suave
open Suave.Http;
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types
open Suave.Web

open Iris.Web.Views

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

let index =
  Index.compileIndex (__SOURCE_DIRECTORY__ + "/src/Iris.Web/bin/Debug/assets/js")

// our application only needs to serve files off the disk
// but we do need to specify what to do in the base case, i.e. "/"
let app =
  choose [ GET >>= choose [ path "/"                   >>= OK index
                            path "/tests"              >>= file (__SOURCE_DIRECTORY__ + "/src/Iris.Web.Tests/index.html")
                            path "/Iris.Web.Worker.js" >>= file (__SOURCE_DIRECTORY__ + "/src/Iris.Web.Worker/bin/Debug/assets/Iris.Web.Worker.js")
                            path "/Iris.Web.Tests.js"  >>= file (__SOURCE_DIRECTORY__ + "/src/Iris.Web.Tests/bin/Debug/assets/Iris.Web.Tests.js")
                            browseHome ] ]
let config =
  { defaultConfig
    with homeFolder = Some(__SOURCE_DIRECTORY__ + "/src/Iris.Web/bin/Debug/assets")
         bindings   = [ HttpBinding.mk' HTTP "127.0.0.1" 3000 ]
         mimeTypesMap = mimeTypes }

startWebServer config app
