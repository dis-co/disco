namespace Iris.Core

// * Platform

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Platform =

  open System

  // ** isUnix

  /// ## isUnix
  ///
  /// Returns true if currently run on MacOS or other Unices.
  ///
  let isUnix : bool =
    int Environment.OSVersion.Platform
    |> fun p ->
      (p = 4) ||                         // Unix
      (p = 6) ||                         // MacOS
      (p = 128)                         // old Mono Unix


  // ** isWindows

  /// ## isWindows
  ///
  /// True if the current platform is not a unix.
  ///
  /// Returns: bool
  let isWindows = not isUnix

#endif
