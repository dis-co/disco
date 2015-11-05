#r @"../../packages/Suave/lib/net40/Suave.dll"

open Suave
open Suave.Http;
open Suave.Http.Applicatives
open Suave.Http.Files
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types
open Suave.Web

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
  choose [ GET >>= choose [ path "/" >>= file "./index.html"
                            browseHome ] ]
let config =
  { defaultConfig
    with homeFolder = Some(__SOURCE_DIRECTORY__ + "/bin/Debug/assets")
         bindings   = [ HttpBinding.mk' HTTP "127.0.0.1" 3001 ]
         mimeTypesMap = mimeTypes }

startWebServer config app
