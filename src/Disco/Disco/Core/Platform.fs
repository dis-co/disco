namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Disco.Serialization

#endif

// * Platform

type Platform =
  | Windows
  | Unix

  // ** ToOffset

  member platform.ToOffset(_) =
    match platform with
    | Platform.Windows -> PlatformFB.WindowsFB
    | Platform.Unix -> PlatformFB.UnixFB

  // ** FromFB

  static member FromFB(fb: PlatformFB) =
    match fb with
    #if FABLE_COMPILER
    | x when x = PlatformFB.WindowsFB -> Right Windows
    | x when x = PlatformFB.UnixFB -> Right Unix
    #else
    | PlatformFB.WindowsFB -> Right Windows
    | PlatformFB.UnixFB -> Right Unix
    #endif
    | other ->
      string other + " is not a recognized platform identifier"
      |> Error.asParseError "Platform.FromFB"
      |> Either.fail

// * Platform module

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

  // ** get

  let get () =
    if isUnix
    then Unix
    else Windows

#endif
