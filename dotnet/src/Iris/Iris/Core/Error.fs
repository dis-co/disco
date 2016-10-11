namespace Iris.Core

type Error<'a> =
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

  // LITEDB
  | DatabaseCreateError    of string
  | DatabaseNotFound       of string
  | MetaDataNotFound

  // CLI
  | MissingStartupDir
  | CliParseError

  // Node
  | MissingNodeId
  | MissingNode            of string

  | AssetSaveError         of string
  | AssetDeleteError       of string

  | Other                  of 'a

[<RequireQualifiedAccess>]
module Error =

  let inline toMessage (error: Error<'a>) =
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

    // LITEDB
    | DatabaseCreateError   e -> sprintf "Database could not be created: %s" e
    | DatabaseNotFound      e -> sprintf "Database could not be found: %s" e
    | MetaDataNotFound        -> sprintf "Metadata could not be loaded from db"

    // CLI
    | MissingStartupDir       ->         "Startup directory missing"
    | CliParseError           ->         "Command line parse error"

    | MissingNodeId           ->         "Node Id missing in environment"
    | MissingNode           e -> sprintf "Node with Id %s missing in Project configuration" e

    | AssetSaveError        e -> sprintf "Could not save asset to disk: %s" e
    | AssetDeleteError      e -> sprintf "Could not delete asset from disl: %s" e

    | Other                 e -> sprintf "Other error occurred: %s" (string e)

    | OK                      ->         "All good."

  let inline toExitCode (error: Error<'a>) =
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
    | DatabaseCreateError   _ -> 13
    | DatabaseNotFound      _ -> 14
    | MetaDataNotFound        -> 15

    // CLI
    | MissingStartupDir       -> 16
    | CliParseError           -> 17

    | AssetSaveError        _ -> 18
    | AssetDeleteError      _ -> 19

    | Other                 _ -> 20


  let inline isOk (error: Error<'a>) =
    match error with
    | OK -> true
    | _  -> false

  let inline exitWith (error: Error<'a>) =
    if not (isOk error) then
      toMessage error
      |> printfn "Fatal: %s"
    error |> toExitCode |> exit
