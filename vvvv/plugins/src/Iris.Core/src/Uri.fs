namespace Iris.Core

open Iris.Core.Types

[<RequireQualifiedAccess>]
module Uri =
  //  _   _      _ 
  // | | | |_ __(_)
  // | | | | '__| |
  // | |_| | |  | |
  //  \___/|_|  |_|
  //               
  let mkProjectUri (project : Project) = sprintf "iris.project/%s" project.Name
