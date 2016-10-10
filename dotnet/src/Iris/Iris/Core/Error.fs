namespace Iris.Core

type IrisError<'a> =
  // GIT
  | BranchNotFound
  | BranchDetailsNotFound
  | RepositoryNotFound
  | RepositoryInitFailed   of string
  | CommitError            of string
  | GitError               of string

  | ProjectPathError
  | ProjectSaveError       of string

  | AssetSaveError         of string
  | AssetDeleteError       of string

  | Other                  of 'a
