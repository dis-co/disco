namespace Iris.Core

[<RequireQualifiedAccess>]
module Uri =
  //  _   _      _ 
  // | | | |_ __(_)
  // | | | | '__| |
  // | |_| | |  | |
  //  \___/|_|  |_|
  //               
  let mkUri resource project group =
    sprintf "iris.%s/%s/%s" resource project group 

  let mkProjectUri (project : Project) =
    match project.CurrentBranch with
      | Some(branch) -> mkUri "project" project.Name branch.CanonicalName
      | _ -> mkUri "project" project.Name "<nobranch>"

  let mkCueUri (project : Project) (group : string) =
    mkUri "cues" project.Name group

  let mkPinUri (project : Project) (group : string) =
    mkUri "pins" project.Name group
