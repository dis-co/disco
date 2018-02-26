(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Disco.Serialization

#endif

// * DiscoError

type DiscoError =
  | OK
  | GitError     of location:string * error:string
  | ProjectError of location:string * error:string
  | SocketError  of location:string * error:string
  | ParseError   of location:string * error:string
  | IOError      of location:string * error:string
  | AssetError   of location:string * error:string
  | RaftError    of location:string * error:string
  | ClientError  of location:string * error:string
  | Other        of location:string * error:string

  // ** Message

  member error.Message
    with get () =
      match error with
      | OK                      -> "Ok"
      | GitError     (_, error) -> error
      | ProjectError (_, error) -> error
      | SocketError  (_, error) -> error
      | ParseError   (_, error) -> error
      | IOError      (_, error) -> error
      | AssetError   (_, error) -> error
      | RaftError    (_, error) -> error
      | ClientError  (_, error) -> error
      | Other        (_, error) -> error

  // ** Location

  member error.Location
    with get () =
      match error with
      | OK                      -> "Ok"
      | GitError     (location, _) -> location
      | ProjectError (location, _) -> location
      | SocketError  (location, _) -> location
      | ParseError   (location, _) -> location
      | IOError      (location, _) -> location
      | AssetError   (location, _) -> location
      | RaftError    (location, _) -> location
      | ClientError  (location, _) -> location
      | Other        (location, _) -> location

  // ** ToString

  override error.ToString() =
    match error with
    | GitError     (loc,err) -> sprintf "Git error: %s in %s"     err loc
    | ProjectError (loc,err) -> sprintf "Project error: %s in %s" err loc
    | ParseError   (loc,err) -> sprintf "Parse Error: %s in %s"   err loc
    | SocketError  (loc,err) -> sprintf "Socket Error: %s in %s"  err loc
    | ClientError  (loc,err) -> sprintf "Client Error: %s in %s"  err loc
    | IOError      (loc,err) -> sprintf "IO Error: %s in %s"      err loc
    | AssetError   (loc,err) -> sprintf "Asset Error: %s in %s"   err loc
    | Other        (loc,err) -> sprintf "Other error: %s in %s"   err loc
    | RaftError    (loc,err) -> sprintf "Raft Error: %s in %s"    err loc
    | OK                     -> "Ok. All good."

  // ** FromFB

  static member FromFB (fb: ErrorFB) =
    match fb.Type with
    #if FABLE_COMPILER
    | x when x = ErrorTypeFB.OKFB           -> Ok OK
    | x when x = ErrorTypeFB.OtherFB        -> Ok (Other        (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.GitErrorFB     -> Ok (GitError     (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.ProjectErrorFB -> Ok (ProjectError (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.AssetErrorFB   -> Ok (AssetError   (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.RaftErrorFB    -> Ok (RaftError    (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.ParseErrorFB   -> Ok (ParseError   (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.SocketErrorFB  -> Ok (SocketError  (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.ClientErrorFB  -> Ok (ClientError  (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.IOErrorFB      -> Ok (IOError      (fb.Location,fb.Message))
    | x ->
      ("DiscoError.FromFB", sprintf "Could not parse unknown ErrorTypeFB: %A" x)
      |> ParseError
      |> Result.fail
    #else
    | ErrorTypeFB.OKFB           -> Ok OK
    | ErrorTypeFB.OtherFB        -> Ok (Other        (fb.Location,fb.Message))
    | ErrorTypeFB.GitErrorFB     -> Ok (GitError     (fb.Location,fb.Message))
    | ErrorTypeFB.ProjectErrorFB -> Ok (ProjectError (fb.Location,fb.Message))
    | ErrorTypeFB.AssetErrorFB   -> Ok (AssetError   (fb.Location,fb.Message))
    | ErrorTypeFB.RaftErrorFB    -> Ok (RaftError    (fb.Location,fb.Message))
    | ErrorTypeFB.ParseErrorFB   -> Ok (ParseError   (fb.Location,fb.Message))
    | ErrorTypeFB.SocketErrorFB  -> Ok (SocketError  (fb.Location,fb.Message))
    | ErrorTypeFB.ClientErrorFB  -> Ok (ClientError  (fb.Location,fb.Message))
    | ErrorTypeFB.IOErrorFB      -> Ok (IOError      (fb.Location,fb.Message))
    | x ->
      ("DiscoError.FromFB", sprintf "Could not parse unknown ErrotTypeFB: %A" x)
      |> ParseError
      |> Result.fail
    #endif

  // ** ToOffset

  member error.ToOffset (builder: FlatBufferBuilder) =
    let map (str: string) =
      match str with
      #if FABLE_COMPILER
      | null -> Unchecked.defaultof<Offset<string>>
      #else
      | null -> Unchecked.defaultof<StringOffset>
      #endif
      | _ -> builder.CreateString str

    let tipe =
      match error with
      | OK             -> ErrorTypeFB.OKFB
      | GitError     _ -> ErrorTypeFB.GitErrorFB
      | ProjectError _ -> ErrorTypeFB.ProjectErrorFB
      | AssetError   _ -> ErrorTypeFB.AssetErrorFB
      | ParseError   _ -> ErrorTypeFB.ParseErrorFB
      | SocketError  _ -> ErrorTypeFB.SocketErrorFB
      | ClientError  _ -> ErrorTypeFB.ClientErrorFB
      | IOError      _ -> ErrorTypeFB.IOErrorFB
      | Other        _ -> ErrorTypeFB.OtherFB
      | RaftError    _ -> ErrorTypeFB.RaftErrorFB

    let str =
      match error with
      | GitError     (loc,msg) -> (map loc, map msg) |> Some
      | ProjectError (loc,msg) -> (map loc, map msg) |> Some
      | AssetError   (loc,msg) -> (map loc, map msg) |> Some
      | ParseError   (loc,msg) -> (map loc, map msg) |> Some
      | SocketError  (loc,msg) -> (map loc, map msg) |> Some
      | ClientError  (loc,msg) -> (map loc, map msg) |> Some
      | IOError      (loc,msg) -> (map loc, map msg) |> Some
      | Other        (loc,msg) -> (map loc, map msg) |> Some
      | RaftError    (loc,msg) -> (map loc, map msg) |> Some
      | _                      -> None

    ErrorFB.StartErrorFB(builder)
    ErrorFB.AddType(builder, tipe)
    match str with
    | Some (loc,msg) ->
      ErrorFB.AddLocation(builder, loc)
      ErrorFB.AddMessage(builder, msg)
    | _ -> ()
    ErrorFB.EndErrorFB(builder)



// * DiscoResult

type DiscoResult<'t> = Result<'t,DiscoError>

// * Error Module
[<RequireQualifiedAccess>]
module Error =

  // ** message

  let message (error: DiscoError) = error.Message

  // ** toMessage

  /// ## toMessage
  ///
  /// Convert a rigid `DiscoError` into a puffy string message to be displayed.
  ///
  /// ### Signature:
  /// - error: `DiscoError` - error to convert into a human understable message
  ///
  /// Returns: string
  let inline toMessage (error: DiscoError) = error.Message

  // ** toExitCode

  /// ## toExitCode
  ///
  /// Convert a rigid `DiscoError` into an integer exit code to be used with `exit`.
  ///
  /// ### Signature:
  /// - error: `DiscoError` - error to return exit code for
  ///
  /// Returns: int
  let inline toExitCode (error: DiscoError) =
    match error with
    | OK             -> 0
    | GitError     _ -> 1
    | ProjectError _ -> 2
    | AssetError   _ -> 3
    | ParseError   _ -> 4
    | SocketError  _ -> 5
    | ClientError  _ -> 6
    | IOError      _ -> 7
    | Other        _ -> 8
    | RaftError    _ -> 9

  // ** isOk

  /// ## isOk
  ///
  /// Check if an `DiscoError` value is the `OK` constructor.
  ///
  /// ### Signature:
  /// - error: `DiscoError` - error to check
  ///
  /// Returns: bool
  let inline isOk (error: DiscoError) =
    match error with
    | OK -> true
    | _  -> false

  // ** exitWith

  /// ## exitWith
  ///
  /// Exit the program with the specified `DiscoError` value, displaying its message and generating
  /// its correspondonding exit code.
  ///
  /// ### Signature:
  /// - error: `DiscoError` - error to exit with
  ///
  /// Returns: unit
  let inline exitWith (error: DiscoError) =
    if not (isOk error) then
      toMessage error
      |> printfn "Fatal: %s"
    error |> toExitCode |> exit

  // ** throw

  /// ## throw
  ///
  /// `failwith` the passed `DiscoError` value.
  ///
  /// ### Signature:
  /// - error: `DiscoError` - value to fail with
  ///
  /// Returns: 'a
  let throw (error: DiscoError) =
    failwithf "ERROR: %A" error

  // ** orExit

  /// ## Exit with an exit code on failure
  ///
  /// Apply function `f` to inner value of `a` *if* `a` is a success,
  /// otherwise exit with an exit code derived from the error value.
  ///
  /// ### Signature:
  /// - `f`: function to apply to inner value of `a`
  /// - `a`: value to apply function
  ///
  /// Returns: ^b
  let inline orExit (f: ^a -> ^b) (a: DiscoResult< ^a >) : ^b =
    match a with
    | Ok value -> f value
    | Error  error -> exitWith error

  let asGitError     loc err = GitError(loc,err)
  let asProjectError loc err = ProjectError(loc,err)
  let asParseError   loc err = ParseError(loc,err)
  let asSocketError  loc err = SocketError(loc,err)
  let asClientError  loc err = ClientError(loc,err)
  let asIOError      loc err = IOError(loc,err)
  let asAssetError   loc err = AssetError(loc,err)
  let asOther        loc err = Other(loc,err)
  let asRaftError    loc err = RaftError(loc,err)
