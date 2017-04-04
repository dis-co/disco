namespace Iris.Core

#if !IRIS_NODES

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

  let mkProjectUri (project : IrisProject) =
    match Project.currentBranch project with
      | Right branch -> mkUri "project" project.Name branch.CanonicalName
      | _            -> mkUri "project" project.Name "<nobranch>"

  let mkCueUri (project : IrisProject) (group : string) =
    mkUri "cues" project.Name group

  let mkPinUri (project : IrisProject) (group : string) =
    mkUri "pins" project.Name group

#endif
