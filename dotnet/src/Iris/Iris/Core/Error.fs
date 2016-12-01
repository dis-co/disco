namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

// * IrisError

type IrisError =
  | OK

  // GIT
  | BranchNotFound         of string
  | BranchDetailsNotFound  of string
  | RepositoryNotFound     of string
  | RepositoryInitFailed   of string
  | CommitError            of string
  | GitError               of string

  // PROJECT
  | ProjectNotFound        of string
  | ProjectParseError      of string
  | ProjectPathError
  | ProjectSaveError       of string
  | ProjectInitError       of string

  | MetaDataNotFound

  | SocketError            of string
  | ParseError             of string
  | IOError                of string

  // CLI
  | MissingStartupDir
  | CliParseError

  // Node
  | MissingNodeId
  | MissingNode            of string

  | AssetNotFoundError     of string
  | AssetLoadError         of string
  | AssetSaveError         of string
  | AssetDeleteError       of string

  | Other                  of string

  // RAFT
  | RaftError              of string
  | AlreadyVoted
  | AppendEntryFailed
  | CandidateUnknown
  | EntryInvalidated
  | InvalidCurrentIndex
  | InvalidLastLog
  | InvalidLastLogTerm
  | InvalidTerm
  | LogFormatError
  | LogIncomplete
  | NoError
  | NoNode
  | NotCandidate
  | NotLeader
  | NotVotingState
  | ResponseTimeout
  | SnapshotFormatError
  | StaleResponse
  | UnexpectedVotingChange
  | VoteTermMismatch

  // ** FromFB

  static member FromFB (fb: ErrorFB) =
    match fb.Type with
#if FABLE_COMPILER
    | x when x = ErrorTypeFB.OKFB                     -> Right OK
    | x when x = ErrorTypeFB.BranchNotFoundFB         -> Right (BranchNotFound fb.Message)
    | x when x = ErrorTypeFB.BranchDetailsNotFoundFB  -> Right (BranchDetailsNotFound fb.Message)
    | x when x = ErrorTypeFB.RepositoryNotFoundFB     -> Right (RepositoryNotFound fb.Message)
    | x when x = ErrorTypeFB.RepositoryInitFailedFB   -> Right (RepositoryInitFailed fb.Message)
    | x when x = ErrorTypeFB.CommitErrorFB            -> Right (CommitError fb.Message)
    | x when x = ErrorTypeFB.GitErrorFB               -> Right (GitError fb.Message)
    | x when x = ErrorTypeFB.ProjectNotFoundFB        -> Right (ProjectNotFound fb.Message)
    | x when x = ErrorTypeFB.ProjectParseErrorFB      -> Right (ProjectParseError fb.Message)
    | x when x = ErrorTypeFB.ProjectPathErrorFB       -> Right ProjectPathError
    | x when x = ErrorTypeFB.ProjectSaveErrorFB       -> Right (ProjectSaveError fb.Message)
    | x when x = ErrorTypeFB.ProjectInitErrorFB       -> Right (ProjectInitError fb.Message)
    | x when x = ErrorTypeFB.MetaDataNotFoundFB       -> Right MetaDataNotFound
    | x when x = ErrorTypeFB.MissingStartupDirFB      -> Right MissingStartupDir
    | x when x = ErrorTypeFB.CliParseErrorFB          -> Right CliParseError
    | x when x = ErrorTypeFB.MissingNodeIdFB          -> Right MissingNodeId
    | x when x = ErrorTypeFB.MissingNodeFB            -> Right (MissingNode fb.Message)
    | x when x = ErrorTypeFB.AssetNotFoundErrorFB     -> Right (AssetNotFoundError fb.Message)
    | x when x = ErrorTypeFB.AssetLoadErrorFB         -> Right (AssetLoadError fb.Message)
    | x when x = ErrorTypeFB.AssetSaveErrorFB         -> Right (AssetSaveError fb.Message)
    | x when x = ErrorTypeFB.AssetDeleteErrorFB       -> Right (AssetDeleteError fb.Message)
    | x when x = ErrorTypeFB.OtherFB                  -> Right (Other fb.Message)
    | x when x = ErrorTypeFB.RaftErrorFB              -> Right (RaftError fb.Message)
    | x when x = ErrorTypeFB.AlreadyVotedFB           -> Right AlreadyVoted
    | x when x = ErrorTypeFB.AppendEntryFailedFB      -> Right AppendEntryFailed
    | x when x = ErrorTypeFB.CandidateUnknownFB       -> Right CandidateUnknown
    | x when x = ErrorTypeFB.EntryInvalidatedFB       -> Right EntryInvalidated
    | x when x = ErrorTypeFB.InvalidCurrentIndexFB    -> Right InvalidCurrentIndex
    | x when x = ErrorTypeFB.InvalidLastLogFB         -> Right InvalidLastLog
    | x when x = ErrorTypeFB.InvalidLastLogTermFB     -> Right InvalidLastLogTerm
    | x when x = ErrorTypeFB.InvalidTermFB            -> Right InvalidTerm
    | x when x = ErrorTypeFB.LogFormatErrorFB         -> Right LogFormatError
    | x when x = ErrorTypeFB.LogIncompleteFB          -> Right LogIncomplete
    | x when x = ErrorTypeFB.NoErrorFB                -> Right NoError
    | x when x = ErrorTypeFB.NoNodeFB                 -> Right NoNode
    | x when x = ErrorTypeFB.NotCandidateFB           -> Right NotCandidate
    | x when x = ErrorTypeFB.NotLeaderFB              -> Right NotLeader
    | x when x = ErrorTypeFB.NotVotingStateFB         -> Right NotVotingState
    | x when x = ErrorTypeFB.ResponseTimeoutFB        -> Right ResponseTimeout
    | x when x = ErrorTypeFB.SnapshotFormatErrorFB    -> Right SnapshotFormatError
    | x when x = ErrorTypeFB.StaleResponseFB          -> Right StaleResponse
    | x when x = ErrorTypeFB.UnexpectedVotingChangeFB -> Right UnexpectedVotingChange
    | x when x = ErrorTypeFB.VoteTermMismatchFB       -> Right VoteTermMismatch
    | x when x = ErrorTypeFB.ParseErrorFB             -> Right (ParseError fb.Message)
    | x when x = ErrorTypeFB.SocketErrorFB            -> Right (SocketError fb.Message)
    | x when x = ErrorTypeFB.IOErrorFB                -> Right (IOError fb.Message)
    | x ->
      sprintf "Could not parse unknown ErrorTypeFB: %A" x
      |> ParseError
      |> Either.fail
#else
    | ErrorTypeFB.OKFB                     -> Right OK
    | ErrorTypeFB.BranchNotFoundFB         -> Right (BranchNotFound fb.Message)
    | ErrorTypeFB.BranchDetailsNotFoundFB  -> Right (BranchDetailsNotFound fb.Message)
    | ErrorTypeFB.RepositoryNotFoundFB     -> Right (RepositoryNotFound fb.Message)
    | ErrorTypeFB.RepositoryInitFailedFB   -> Right (RepositoryInitFailed fb.Message)
    | ErrorTypeFB.CommitErrorFB            -> Right (CommitError fb.Message)
    | ErrorTypeFB.GitErrorFB               -> Right (GitError fb.Message)
    | ErrorTypeFB.ProjectNotFoundFB        -> Right (ProjectNotFound fb.Message)
    | ErrorTypeFB.ProjectParseErrorFB      -> Right (ProjectParseError fb.Message)
    | ErrorTypeFB.ProjectPathErrorFB       -> Right ProjectPathError
    | ErrorTypeFB.ProjectSaveErrorFB       -> Right (ProjectSaveError fb.Message)
    | ErrorTypeFB.ProjectInitErrorFB       -> Right (ProjectInitError fb.Message)
    | ErrorTypeFB.MetaDataNotFoundFB       -> Right MetaDataNotFound
    | ErrorTypeFB.MissingStartupDirFB      -> Right MissingStartupDir
    | ErrorTypeFB.CliParseErrorFB          -> Right CliParseError
    | ErrorTypeFB.MissingNodeIdFB          -> Right MissingNodeId
    | ErrorTypeFB.MissingNodeFB            -> Right (MissingNode fb.Message)
    | ErrorTypeFB.AssetNotFoundErrorFB     -> Right (AssetNotFoundError fb.Message)
    | ErrorTypeFB.AssetLoadErrorFB         -> Right (AssetLoadError fb.Message)
    | ErrorTypeFB.AssetSaveErrorFB         -> Right (AssetSaveError fb.Message)
    | ErrorTypeFB.AssetDeleteErrorFB       -> Right (AssetDeleteError fb.Message)
    | ErrorTypeFB.OtherFB                  -> Right (Other fb.Message)
    | ErrorTypeFB.RaftErrorFB              -> Right (RaftError fb.Message)
    | ErrorTypeFB.AlreadyVotedFB           -> Right AlreadyVoted
    | ErrorTypeFB.AppendEntryFailedFB      -> Right AppendEntryFailed
    | ErrorTypeFB.CandidateUnknownFB       -> Right CandidateUnknown
    | ErrorTypeFB.EntryInvalidatedFB       -> Right EntryInvalidated
    | ErrorTypeFB.InvalidCurrentIndexFB    -> Right InvalidCurrentIndex
    | ErrorTypeFB.InvalidLastLogFB         -> Right InvalidLastLog
    | ErrorTypeFB.InvalidLastLogTermFB     -> Right InvalidLastLogTerm
    | ErrorTypeFB.InvalidTermFB            -> Right InvalidTerm
    | ErrorTypeFB.LogFormatErrorFB         -> Right LogFormatError
    | ErrorTypeFB.LogIncompleteFB          -> Right LogIncomplete
    | ErrorTypeFB.NoErrorFB                -> Right NoError
    | ErrorTypeFB.NoNodeFB                 -> Right NoNode
    | ErrorTypeFB.NotCandidateFB           -> Right NotCandidate
    | ErrorTypeFB.NotLeaderFB              -> Right NotLeader
    | ErrorTypeFB.NotVotingStateFB         -> Right NotVotingState
    | ErrorTypeFB.ResponseTimeoutFB        -> Right ResponseTimeout
    | ErrorTypeFB.SnapshotFormatErrorFB    -> Right SnapshotFormatError
    | ErrorTypeFB.StaleResponseFB          -> Right StaleResponse
    | ErrorTypeFB.UnexpectedVotingChangeFB -> Right UnexpectedVotingChange
    | ErrorTypeFB.VoteTermMismatchFB       -> Right VoteTermMismatch
    | ErrorTypeFB.ParseErrorFB             -> Right (ParseError fb.Message)
    | ErrorTypeFB.SocketErrorFB            -> Right (SocketError fb.Message)
    | ErrorTypeFB.IOErrorFB                -> Right (IOError fb.Message)
    | x ->
      sprintf "Could not parse unknown ErrotTypeFB: %A" x
      |> ParseError
      |> Either.fail
#endif

  // ** ToOffset

  member error.ToOffset (builder: FlatBufferBuilder) =
    let tipe =
      match error with
      | OK                       -> ErrorTypeFB.OKFB
      | BranchNotFound         _ -> ErrorTypeFB.BranchNotFoundFB
      | BranchDetailsNotFound  _ -> ErrorTypeFB.BranchDetailsNotFoundFB
      | RepositoryNotFound     _ -> ErrorTypeFB.RepositoryNotFoundFB
      | RepositoryInitFailed   _ -> ErrorTypeFB.RepositoryInitFailedFB
      | CommitError            _ -> ErrorTypeFB.CommitErrorFB
      | GitError               _ -> ErrorTypeFB.GitErrorFB
      | ProjectNotFound        _ -> ErrorTypeFB.ProjectNotFoundFB
      | ProjectParseError      _ -> ErrorTypeFB.ProjectParseErrorFB
      | ProjectPathError         -> ErrorTypeFB.ProjectPathErrorFB
      | ProjectSaveError       _ -> ErrorTypeFB.ProjectSaveErrorFB
      | ProjectInitError       _ -> ErrorTypeFB.ProjectInitErrorFB
      | MetaDataNotFound         -> ErrorTypeFB.MetaDataNotFoundFB
      | MissingStartupDir        -> ErrorTypeFB.MissingStartupDirFB
      | CliParseError            -> ErrorTypeFB.CliParseErrorFB
      | MissingNodeId            -> ErrorTypeFB.MissingNodeIdFB
      | MissingNode            _ -> ErrorTypeFB.MissingNodeFB
      | AssetNotFoundError     _ -> ErrorTypeFB.AssetNotFoundErrorFB
      | AssetLoadError         _ -> ErrorTypeFB.AssetLoadErrorFB
      | AssetSaveError         _ -> ErrorTypeFB.AssetSaveErrorFB
      | AssetDeleteError       _ -> ErrorTypeFB.AssetDeleteErrorFB
      | ParseError             _ -> ErrorTypeFB.ParseErrorFB
      | SocketError            _ -> ErrorTypeFB.SocketErrorFB
      | IOError                _ -> ErrorTypeFB.IOErrorFB
      | Other                  _ -> ErrorTypeFB.OtherFB

      | RaftError              _ -> ErrorTypeFB.RaftErrorFB
      | AlreadyVoted             -> ErrorTypeFB.AlreadyVotedFB
      | AppendEntryFailed        -> ErrorTypeFB.AppendEntryFailedFB
      | CandidateUnknown         -> ErrorTypeFB.CandidateUnknownFB
      | EntryInvalidated         -> ErrorTypeFB.EntryInvalidatedFB
      | InvalidCurrentIndex      -> ErrorTypeFB.InvalidCurrentIndexFB
      | InvalidLastLog           -> ErrorTypeFB.InvalidLastLogFB
      | InvalidLastLogTerm       -> ErrorTypeFB.InvalidLastLogTermFB
      | InvalidTerm              -> ErrorTypeFB.InvalidTermFB
      | LogFormatError           -> ErrorTypeFB.LogFormatErrorFB
      | LogIncomplete            -> ErrorTypeFB.LogIncompleteFB
      | NoError                  -> ErrorTypeFB.NoErrorFB
      | NoNode                   -> ErrorTypeFB.NoNodeFB
      | NotCandidate             -> ErrorTypeFB.NotCandidateFB
      | NotLeader                -> ErrorTypeFB.NotLeaderFB
      | NotVotingState           -> ErrorTypeFB.NotVotingStateFB
      | ResponseTimeout          -> ErrorTypeFB.ResponseTimeoutFB
      | SnapshotFormatError      -> ErrorTypeFB.SnapshotFormatErrorFB
      | StaleResponse            -> ErrorTypeFB.StaleResponseFB
      | UnexpectedVotingChange   -> ErrorTypeFB.UnexpectedVotingChangeFB
      | VoteTermMismatch         -> ErrorTypeFB.VoteTermMismatchFB

    let str =
      match error with
      | BranchNotFound         msg -> builder.CreateString msg |> Some
      | BranchDetailsNotFound  msg -> builder.CreateString msg |> Some
      | RepositoryNotFound     msg -> builder.CreateString msg |> Some
      | RepositoryInitFailed   msg -> builder.CreateString msg |> Some
      | CommitError            msg -> builder.CreateString msg |> Some
      | GitError               msg -> builder.CreateString msg |> Some
      | ProjectNotFound        msg -> builder.CreateString msg |> Some
      | ProjectParseError      msg -> builder.CreateString msg |> Some
      | ProjectSaveError       msg -> builder.CreateString msg |> Some
      | ProjectInitError       msg -> builder.CreateString msg |> Some
      | MissingNode            msg -> builder.CreateString msg |> Some
      | AssetNotFoundError     msg -> builder.CreateString msg |> Some
      | AssetSaveError         msg -> builder.CreateString msg |> Some
      | AssetLoadError         msg -> builder.CreateString msg |> Some
      | AssetDeleteError       msg -> builder.CreateString msg |> Some
      | ParseError             msg -> builder.CreateString msg |> Some
      | SocketError            msg -> builder.CreateString msg |> Some
      | IOError                msg -> builder.CreateString msg |> Some
      | Other                  msg -> builder.CreateString msg |> Some
      | RaftError              msg -> builder.CreateString msg |> Some
      | _                          -> None

    ErrorFB.StartErrorFB(builder)
    ErrorFB.AddType(builder, tipe)
    match str with
    | Some payload -> ErrorFB.AddMessage(builder, payload)
    | _            -> ()
    ErrorFB.EndErrorFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> ErrorFB.GetRootAsErrorFB
    |> IrisError.FromFB


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
  let inline toMessage (error: IrisError) =
    match error with
    | BranchNotFound        e -> sprintf "Branch does not exist: %s" e
    | BranchDetailsNotFound e -> sprintf "Branch details could not be found: %s" e
    | RepositoryNotFound    e -> sprintf "Repository was not found: %s" e
    | RepositoryInitFailed  e -> sprintf "Repository could not be initialized: %s" e
    | CommitError           e -> sprintf "Could not commit changes: %s" e
    | GitError              e -> sprintf "Git error: %s" e

    | ProjectNotFound       e -> sprintf "Project could not be found: %s" e
    | ProjectPathError        ->         "Project has no path"
    | ProjectSaveError      e -> sprintf "Project could not be saved: %s" e
    | ProjectParseError     e -> sprintf "Project could not be parsed: %s" e

    | ParseError            e -> sprintf "Parse Error: %s" e
    | SocketError           e -> sprintf "Socket Error: %s" e
    | IOError               e -> sprintf "IO Error: %s" e

    // LITEDB
    | ProjectInitError      e -> sprintf "Database could not be created: %s" e
    | MetaDataNotFound        -> sprintf "Metadata could not be loaded from db"

    // CLI
    | MissingStartupDir       ->         "Startup directory missing"
    | CliParseError           ->         "Command line parse error"

    | MissingNodeId           ->         "Node Id missing in environment"
    | MissingNode           e -> sprintf "Node with Id %s missing in Project configuration" e

    | AssetNotFoundError    e -> sprintf "Could not find asset on disk: %s" e
    | AssetSaveError        e -> sprintf "Could not save asset to disk: %s" e
    | AssetLoadError        e -> sprintf "Could not load asset to disk: %s" e
    | AssetDeleteError      e -> sprintf "Could not delete asset from disl: %s" e

    | Other                 e -> sprintf "Other error occurred: %s" (string e)

    // RAFT
    | RaftError             e -> sprintf "RaftError: %s" e
    | AlreadyVoted            -> "Already voted"
    | AppendEntryFailed       -> "AppendEntry request has failed"
    | CandidateUnknown        -> "Election candidate not known to Raft"
    | EntryInvalidated        -> "Entry was invalidated"
    | InvalidCurrentIndex     -> "Invalid CurrentIndex"
    | InvalidLastLog          -> "Invalid last log"
    | InvalidLastLogTerm      -> "Invalid last log term"
    | InvalidTerm             -> "Invalid term"
    | LogFormatError          -> "Log format error"
    | LogIncomplete           -> "Log is incomplete"
    | NoError                 -> "No error"
    | NoNode                  -> "No node"
    | NotCandidate            -> "Not currently candidate"
    | NotLeader               -> "Not currently leader"
    | NotVotingState          -> "Not in voting state"
    | ResponseTimeout         -> "Response timeout"
    | SnapshotFormatError     -> "Snapshot was malformed"
    | StaleResponse           -> "Unsolicited response"
    | UnexpectedVotingChange  -> "Unexpected voting change"
    | VoteTermMismatch        -> "Vote term mismatch"

    | OK                      -> "All good."

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
    | OK                      -> 0
    | BranchNotFound        _ -> 1
    | BranchDetailsNotFound _ -> 2
    | RepositoryNotFound    _ -> 3
    | RepositoryInitFailed  _ -> 4
    | CommitError           _ -> 5
    | GitError              _ -> 6

    | ProjectNotFound       _ -> 7
    | ProjectPathError        -> 8
    | ProjectSaveError      _ -> 9
    | ProjectParseError     _ -> 10

    | MissingNodeId         _ -> 11
    | MissingNode           _ -> 12

    // LITEDB
    | ProjectInitError      _ -> 13
    | MetaDataNotFound        -> 14

    // CLI
    | MissingStartupDir       -> 15
    | CliParseError           -> 16

    | AssetNotFoundError    _ -> 18
    | AssetSaveError        _ -> 19
    | AssetLoadError        _ -> 19
    | AssetDeleteError      _ -> 21

    | ParseError            _ -> 21
    | SocketError           _ -> 22
    | IOError               _ -> 22

    | Other                 _ -> 24

    // RAFT
    | RaftError             _ -> 25
    | AlreadyVoted            -> 26
    | AppendEntryFailed       -> 27
    | CandidateUnknown        -> 28
    | EntryInvalidated        -> 29
    | InvalidCurrentIndex     -> 30
    | InvalidLastLog          -> 31
    | InvalidLastLogTerm      -> 32
    | InvalidTerm             -> 33
    | LogFormatError          -> 34
    | LogIncomplete           -> 35
    | NoError                 -> 36
    | NoNode                  -> 37
    | NotCandidate            -> 38
    | NotLeader               -> 39
    | NotVotingState          -> 40
    | ResponseTimeout         -> 41
    | SnapshotFormatError     -> 42
    | StaleResponse           -> 43
    | UnexpectedVotingChange  -> 44
    | VoteTermMismatch        -> 45


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
