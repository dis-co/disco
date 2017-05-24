namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization

#endif

// * IrisError

type IrisError =
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
    | x when x = ErrorTypeFB.OKFB           -> Right OK
    | x when x = ErrorTypeFB.OtherFB        -> Right (Other        (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.GitErrorFB     -> Right (GitError     (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.ProjectErrorFB -> Right (ProjectError (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.AssetErrorFB   -> Right (AssetError   (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.RaftErrorFB    -> Right (RaftError    (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.ParseErrorFB   -> Right (ParseError   (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.SocketErrorFB  -> Right (SocketError  (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.ClientErrorFB  -> Right (ClientError  (fb.Location,fb.Message))
    | x when x = ErrorTypeFB.IOErrorFB      -> Right (IOError      (fb.Location,fb.Message))
    | x ->
      ("IrisError.FromFB", sprintf "Could not parse unknown ErrorTypeFB: %A" x)
      |> ParseError
      |> Either.fail
    #else
    | ErrorTypeFB.OKFB           -> Right OK
    | ErrorTypeFB.OtherFB        -> Right (Other        (fb.Location,fb.Message))
    | ErrorTypeFB.GitErrorFB     -> Right (GitError     (fb.Location,fb.Message))
    | ErrorTypeFB.ProjectErrorFB -> Right (ProjectError (fb.Location,fb.Message))
    | ErrorTypeFB.AssetErrorFB   -> Right (AssetError   (fb.Location,fb.Message))
    | ErrorTypeFB.RaftErrorFB    -> Right (RaftError    (fb.Location,fb.Message))
    | ErrorTypeFB.ParseErrorFB   -> Right (ParseError   (fb.Location,fb.Message))
    | ErrorTypeFB.SocketErrorFB  -> Right (SocketError  (fb.Location,fb.Message))
    | ErrorTypeFB.ClientErrorFB  -> Right (ClientError  (fb.Location,fb.Message))
    | ErrorTypeFB.IOErrorFB      -> Right (IOError      (fb.Location,fb.Message))
    | x ->
      ("IrisError.FromFB", sprintf "Could not parse unknown ErrotTypeFB: %A" x)
      |> ParseError
      |> Either.fail
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



// * Error Module
[<RequireQualifiedAccess>]
module Error =

  // ** toMessage

  /// ## toMessage
  ///
  /// Convert a rigid `IrisError` into a puffy string message to be displayed.
  ///
  /// ### Signature:
  /// - error: `IrisError` - error to convert into a human understable message
  ///
  /// Returns: string
  let inline toMessage (error: IrisError) = error.ToString()

  // ** toExitCode

  /// ## toExitCode
  ///
  /// Convert a rigid `IrisError` into an integer exit code to be used with `exit`.
  ///
  /// ### Signature:
  /// - error: `IrisError` - error to return exit code for
  ///
  /// Returns: int
  let inline toExitCode (error: IrisError) =
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
  /// Check if an `IrisError` value is the `OK` constructor.
  ///
  /// ### Signature:
  /// - error: `IrisError` - error to check
  ///
  /// Returns: bool
  let inline isOk (error: IrisError) =
    match error with
    | OK -> true
    | _  -> false

  // ** exitWith

  /// ## exitWith
  ///
  /// Exit the program with the specified `IrisError` value, displaying its message and generating
  /// its correspondonding exit code.
  ///
  /// ### Signature:
  /// - error: `IrisError` - error to exit with
  ///
  /// Returns: unit
  let inline exitWith (error: IrisError) =
    if not (isOk error) then
      toMessage error
      |> printfn "Fatal: %s"
    error |> toExitCode |> exit

  // ** throw

  /// ## throw
  ///
  /// `failwith` the passed `IrisError` value.
  ///
  /// ### Signature:
  /// - error: `IrisError` - value to fail with
  ///
  /// Returns: 'a
  let throw (error: IrisError) =
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
  let inline orExit (f: ^a -> ^b) (a: Either< IrisError, ^a>) : ^b =
    match a with
    | Right value -> f value
    | Left  error -> exitWith error

  let asGitError     loc err = GitError(loc,err)
  let asProjectError loc err = ProjectError(loc,err)
  let asParseError   loc err = ParseError(loc,err)
  let asSocketError  loc err = SocketError(loc,err)
  let asClientError  loc err = ClientError(loc,err)
  let asIOError      loc err = IOError(loc,err)
  let asAssetError   loc err = AssetError(loc,err)
  let asOther        loc err = Other(loc,err)
  let asRaftError    loc err = RaftError(loc,err)
