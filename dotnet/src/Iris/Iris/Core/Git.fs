namespace Iris.Core


open System.IO

//   ____ _ _
//  / ___(_) |_
// | |  _| | __|
// | |_| | | |_
//  \____|_|\__|

/// ## Git module
///
/// Provides a more functional API over LibGit2Sharp.
///
[<RequireQualifiedAccess>]
module Git =

  open System.Linq
  open LibGit2Sharp

  //  ____                       _
  // | __ ) _ __ __ _ _ __   ___| |__
  // |  _ \| '__/ _` | '_ \ / __| '_ \
  // | |_) | | | (_| | | | | (__| | | |
  // |____/|_|  \__,_|_| |_|\___|_| |_|

  /// ## Git.Branch
  ///
  /// Manage Git branches.
  ///
  module Branch =

    let private tag (str:string) = sprintf "Git.Branch.%s" str

    /// ## Create a new brnac
    ///
    /// Creates a new branch with the specified name with current HEAD branch as origin.
    ///
    /// ### Signature:
    /// - name: name of the new Branch
    /// - repo: Repository to create branch in
    ///
    /// Returns: Branch
    let create (name: string) (repo: Repository) =
      repo.CreateBranch(name)

    /// ## Get the currently checked out branch
    ///
    /// Get the currently checked out Branch of a given Repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Branch
    let current (repo: Repository) = repo.Head

    /// ## Get the remote tracked branch
    ///
    /// Gets the remotely tracked branch, inidicating that none was set by returning `None`.
    ///
    /// ### Signature:
    /// - branch: Branch
    ///
    /// Returns: Either<string,Branch>
    let tracked (branch: Branch) : Either<IrisError,Branch> =
      match branch.TrackedBranch with
        | null      ->
          "No tracked branch"
          |> Error.asGitError (tag "tracked")
          |> Either.fail
        | branch -> Either.succeed branch

    /// ## Get details about the remote tracking branch
    ///
    /// Get details about the remote tracked by the passed in branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get details for
    ///
    /// Returns: BranchTrackingDetails option
    let tracking (branch: Branch) : Either<IrisError,BranchTrackingDetails> =
      match branch.TrackingDetails with
        | null       ->
          "No tracked branch"
          |> Error.asGitError (tag "tracking")
          |> Either.fail
        | details -> Either.succeed details

    /// ## Get the lastest commit object.
    ///
    /// The tip of the branch is the latest commit made to it.
    ///
    /// ### Signature:
    /// - branch: Branch to get the tip of
    ///
    /// Returns: Commit
    let tip (branch: Branch) = branch.Tip

    /// ## Remote name of current branch
    ///
    /// Get the remote name of current branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get remote name for
    ///
    /// Returns: string
    let remoteName (branch: Branch) = branch.RemoteName

    /// ## Get Reference object for Branch
    ///
    /// Get the `Reference`
    ///
    /// ### Signature:
    /// - branch: Branch to get reference for
    ///
    /// Returns: Reference
    let reference (branch: Branch) = branch.Reference

    let item (path: FilePath) (branch: Branch) = branch.Item path

    /// ## Is the passed branch a remote?
    ///
    /// Return true if the passed branch is a remote.
    ///
    /// ### Signature:
    /// - branch: Branch
    ///
    /// Returns: boolean
    let isRemote (branch: Branch) = branch.IsRemote

    /// ## Check if branch tracks a remote
    ///
    /// Return true if the passed branch tracks any remote branch.
    ///
    /// ### Signature:
    /// - branch: Branch to check
    ///
    /// Returns: boolean
    let isTracking (branch: Branch) = branch.IsRemote

    /// ## Check if branch is current repository head.
    ///
    /// Check if the passed branch is the current repository HEAD.
    ///
    /// ### Signature:
    /// - branch: Branch to check
    ///
    /// Returns: boolean
    let isCurrentRepositoryHead (branch: Branch) = branch.IsCurrentRepositoryHead

    /// ## Get the canonical name of a branch
    ///
    /// Get the canonical name of a branch.
    ///
    /// ### Signature:
    /// - branch: Branch
    ///
    /// Returns: string
    let canonicalName (branch: Branch) = branch.CanonicalName

    /// ## Get the canonical name of upstream branch
    ///
    /// Get the canonical name of the passed branches upstream branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get name for
    ///
    /// Returns: string
    let upstreamCanonicalName (branch: Branch) = branch.UpstreamBranchCanonicalName

    /// ## Get friendly name of Branch
    ///
    /// Get the human-friendly name of a branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get name of
    ///
    /// Returns: string
    let friendlyName (branch: Branch) = branch.FriendlyName

    /// ## Get commit log of branch
    ///
    /// Get the commit log for the passed branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get commits for
    ///
    /// Returns: ICommitLog
    let commits (branch: Branch) =
      let commits = ref []
      for commit in branch.Commits.Reverse() do
        commits := commit :: !commits
      !commits

  //  ____
  // |  _ \ ___ _ __   ___
  // | |_) / _ \ '_ \ / _ \
  // |  _ <  __/ |_) | (_) |
  // |_| \_\___| .__/ \___/
  //           |_|

  /// ## Git.Repo
  ///
  /// Git repository management code.
  ///
  module Repo =
    let private tag (str: string) = sprintf "Git.Repo.%s" str

    let path (repo: Repository) =
      repo.Info.Path

    let parentPath (repo: Repository) =
      let p = path repo
      if p.[p.Length - 1] = Path.DirectorySeparatorChar then
        p |> Path.GetDirectoryName
        |> Path.GetDirectoryName
      else
        p |> Path.GetDirectoryName

    /// ## Get all tags for repository
    ///
    /// Look up and enumerate all tags in the passed repository
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: TagsCollection
    let tags (repo: Repository) =
      let tags = ref []
      for tag in repo.Tags.Reverse() do
        tags := tag :: !tags
      !tags

    /// ## Get submodules of a repository
    ///
    /// Get all registered submodules for a collection
    ///
    /// ### Signature:
    /// - repo: Repository to get submodules for
    ///
    /// Returns: SubmoduleCollection
    let submodules (repo: Repository) =
      let modewls = ref []
      for modewl in repo.Submodules do
        modewls := modewl :: !modewls
      !modewls

    /// ## clone
    ///
    /// Clone a remote repository.
    ///
    /// ### Signature:
    /// - target: FilePath to target directory
    /// - remote: string specifiying the remote repository address
    ///
    /// Returns: Either<IrisError, Respository>
    let clone (target: FilePath) (remote: string) =
      try
        new Repository(Repository.Clone(remote, target))
        |> Either.succeed
      with
        | exn ->
          Left (Error.asGitError (tag "clone") exn.Message)

    /// ## Get all branches in repository.
    ///
    /// Get all branches in repository.
    ///
    /// ### Signature:
    /// - repo: Repository to get branches for
    ///
    /// Returns: Branch list
    let branches (repo: Repository) =
      let branches = ref []
      for branch in repo.Branches do
        branches := branch :: !branches
      !branches

    /// ## Get all stashes in this repository
    ///
    /// Gets all stashes in current repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Stash list
    let stashes (repo: Repository) =
      let stashes = ref []
      for stash in repo.Stashes do
        stashes := stash :: !stashes
      !stashes

    /// ## Revert specified commit
    ///
    /// Reverts the specified commit on passed repository.
    ///
    /// ### Signature:
    /// - commit: Commit object to revert
    /// - committer: Signature of the commtter
    /// - repo: Repository to revert comit on
    ///
    /// Returns: RevertResult
    let revert (commit: Commit) (committer: Signature) (repo: Repository) =
      repo.Revert(commit, committer)

    /// ## Reset current repository.
    ///
    /// Reset entire repository according to passed in mode. Hard resets are destructive.
    ///
    /// ### Signature:
    /// - opts: ResetMode (Soft, Mixed, Hard)
    /// - repo: Repository to reset
    ///
    /// Returns: unit
    let reset (opts: ResetMode) (repo: Repository) = repo.Reset opts

    /// ## Reset current repository to specified commit
    ///
    /// Reset current respository to specified commit.
    ///
    /// ### Signature:
    /// - opts: ResetMode (Soft, Mixed, Hard)
    /// - commit: Commit
    /// - repo: Repository
    ///
    /// Returns: unit
    let resetTo (opts: ResetMode) (commit: Commit) (repo: Repository) = repo.Reset(opts, commit)


    /// ## Clean repository
    ///
    /// Remove all untracked files from repository.
    ///
    /// ### Signature:
    /// - repo: Repository to clean
    ///
    /// Returns: unit
    let clean (repo: Repository) = repo.RemoveUntrackedFiles()

    /// ## List all Refs in repository
    ///
    /// Lists all Reference objects in a repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Reference list
    let refs (repo: Repository) =
      let refs = ref []
      for rev in repo.Refs do
        refs := rev :: !refs
      !refs

    /// ## Get the object database for repository
    ///
    /// get all objects in repository
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: ObjectDatabase
    let database (repo: Repository) = repo.ObjectDatabase

    /// ### Signature:
    /// - repo: Repository
    /// - arg: arg
    /// - arg: arg
    ///
    /// Returns: Index
    let index (repo: Repository) = repo.Index

    /// ## Gets the currently ignored files
    ///
    /// Gets the currently ignored files for a repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Ingnore
    let ignored (repo: Repository) = repo.Ignore

    /// ## Get a diff between current working changes and last commit
    ///
    /// Get a diff for current changes
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Diff
    let diff (repo: Repository) = repo.Diff

    /// ## Get repository configuration
    ///
    /// Gets the current repository configuration
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Configuration
    let config (repo: Repository) = repo.Config

    /// ## Merge the specified branch
    ///
    /// Merge the specified branch into the current branch pointed to by HEAD.
    ///
    /// ### Signature:
    /// - branch: Branch to merge
    /// - committer: Signature of committer
    /// - repo: Repository
    ///
    /// Returns: unit
    let merge (branch: Branch) (committer: Signature) (repo: Repository) =
      repo.Merge(branch, committer)

    /// ## Checkout specified Branch/SHA1/Tag etc
    ///
    /// Check out the specified branch, commit by sha1 hash or tag spec.
    ///
    /// ### Signature:
    /// - spec: Branch name, SHA! commit hash or tag name
    /// - repo: Repository
    ///
    /// Returns: Branch
    let checkout (spec: string) (repo: Repository) =
      match LibGit2Sharp.Commands.Checkout(repo, spec) with
      | null      ->
        sprintf "%s not found" spec
        |> Error.asGitError (tag "checkout")
        |> Either.fail
      | branch -> Either.succeed branch

    /// ## Find and return Repository object
    ///
    /// Get a repo for the specified path. Indicate failure to find it by returning `None`
    ///
    /// ### Signature:
    /// - path: FilePath to search for the .git folder
    ///
    /// Returns: Repository option
    let repository (path: FilePath) : Either<IrisError,Repository> =
      try
        new Repository(path </> ".git")
        |> Either.succeed
      with
        | :? RepositoryNotFoundException as exn  ->
          sprintf "Repository not found: %s" path
          |> Error.asGitError (tag "repository")
          |> Either.fail
        | exn ->
          exn.Message
          |> Error.asGitError (tag "repository")
          |> Either.fail

    /// ## Initialize a new repository
    ///
    /// Initialize a new repository at the location specified
    ///
    /// ### Signature:
    /// - path: FilePath pointing to the target directory
    ///
    /// Returns: Either<IrisError<string>,Repository>
    let init (path: FilePath) =
      try
        Repository.Init path |> ignore
        repository path
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "init")
          |> Either.fail

    let add (repo: Repository) (filepath: FilePath) =
      try
        if Path.IsPathRooted filepath then
          sprintf "Path must be relative to the project root: %s" filepath
          |> Error.asGitError (tag "add")
          |> Either.fail
        else
          if File.Exists filepath then
            repo.Index.Add filepath
          Either.succeed ()
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "add")
          |> Either.fail

    let stage (repo: Repository) (filepath: FilePath) =
      try
        Commands.Stage(repo, filepath)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "stage")
          |> Either.fail

    let commit (repo: Repository) (msg: string) (committer: Signature) =
      try
        repo.Commit(msg, committer, committer)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "commit")
          |> Either.fail

    /// ## Retrieve current repository status object
    ///
    /// Retrieve status information on the current repository
    ///
    /// ### Signature:
    /// - repo: Repository to fetch status for
    ///
    /// Returns: RepositoryStatus
    let status (repo: Repository) : Either<IrisError,RepositoryStatus> =
      try
        repo.RetrieveStatus()
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "status")
          |> Either.fail

    /// ## Check if repository is currently dirty
    ///
    /// Check if the current repository is dirty or nor.
    ///
    /// ### Signature:
    /// - repo: Repository to check
    ///
    /// Returns: boolean
    let isDirty (repo: Repository) : Either<IrisError, bool> =
      either {
        let! status = status repo
        return status.IsDirty
      }

    /// ## untracked
    ///
    /// List all untracked files in the repository
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: seq<StatusEntry>
    let untracked (repo: Repository) : Either<IrisError,seq<StatusEntry>> =
      either {
        let! status = status repo
        return status.Untracked
      }

    /// ## Shorthand to work with the commit log of a repository
    ///
    /// Get the list of commits for a repository.
    ///
    /// ### Signature:
    /// - repo: Repository to get commits for
    ///
    /// Returns: IQueryableCommitLog
    let commits (repo: Repository) : IQueryableCommitLog =
      repo.Commits

    /// ## Get the commit object at given index.
    ///
    /// Get the commit object at the given commit index in the
    /// IQueryableCommitLog.
    ///
    /// ### Signature:
    /// - idx: index of commit
    /// - t: IQueryableCommitLog
    ///
    /// Returns: Commit
    let elementAt (idx: int) (t: IQueryableCommitLog) : Either<IrisError,Commit> =
      try
        t.ElementAt(idx)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "elementAt")
          |> Either.fail

    /// ## Count number of commits
    ///
    /// Count number of commits at the repository level.
    ///
    /// ### Signature:
    /// - repo: Git repository object
    ///
    /// Returns: int
    let commitCount (repo: Repository) =
      commits repo
      |> fun lst -> lst.Count()

  //   ____             __ _
  //  / ___|___  _ __  / _(_) __ _
  // | |   / _ \| '_ \| |_| |/ _` |
  // | |__| (_) | | | |  _| | (_| |
  //  \____\___/|_| |_|_| |_|\__, |
  //                         |___/

  module Config =
    open System.Text.RegularExpressions

    let remoteUrl name =
      sprintf "remote.%s.url" name

    let remoteFetch name =
      sprintf "remote.%s.fetch" name

    let fetchSetting name =
      sprintf "+refs/heads/*:refs/remotes/%s/*" name

    let remotes (repo: Repository) =
      let result = ref Map.empty
      let url = new Regex("(?<=remote\.).*?(?=\.url)")

      for cfg in repo.Config do
        let mtch = url.Match cfg.Key
        if mtch.Success then
          result := Map.add mtch.Value cfg.Value !result

      !result

    let addRemote (repo: Repository) (name: string) (url: string) =
      repo.Config.Set<string>(remoteUrl name, url)
      repo.Config.Set<string>(remoteFetch name, fetchSetting name)

    let delRemote (repo: Repository) (name: string) =
      repo.Config.Unset(remoteUrl name)
      repo.Config.Unset(remoteFetch name)
