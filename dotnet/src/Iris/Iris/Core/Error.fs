namespace Iris.Core

type ExitCode =
  | OK                = 0
  | MissingNodeId     = 1
  | MissingNode       = 2
  | MissingStartupDir = 3
  | CliParseError     = 4
  | ProjectMissing    = 5
  | GeneralError      = 6

type IrisError<'a> =
  // GIT
  | BranchNotFound
  | BranchDetailsNotFound
  | RepositoryNotFound
  | RepositoryInitFailed   of string
  | CommitError            of string
  | GitError               of string

  | ProjectNotFound
  | ProjectPathError
  | ProjectSaveError       of string

  | AssetSaveError         of string
  | AssetDeleteError       of string

  | Other                  of 'a
