namespace Iris.Core

// * Imports

#if !FABLE_COMPILER

open System
open System.IO
open System.Linq
open LibGit2Sharp

// * Git

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
  open Path

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
      try
        repo.CreateBranch(name)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "create")
          |> Either.fail

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

    let item (path: FilePath) (branch: Branch) =
      branch.Item (unwrap path)

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

    /// ## setTracked
    ///
    /// Updates tracking information for all branches
    ///
    /// ### Signature:
    /// - remote: string
    /// - upstream: string
    ///
    /// Returns: Either<IrisError, unit>

    let setTracked (repo: Repository) (branch: Branch) (remote: Remote) =
      try
        let setRemote (updater: BranchUpdater) =
          updater.Remote <- remote.Name
          updater.UpstreamBranch <- branch.CanonicalName
        repo.Branches.Update (branch, setRemote)
        |> Either.ignore
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "setTracked")
          |> Either.fail

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
      let pth = path repo
      if pth.[pth.Length - 1] = Path.DirectorySeparatorChar then
        pth
        |> Path.GetDirectoryName
        |> Path.GetDirectoryName
        |> filepath
      else
        pth
        |> Path.GetDirectoryName
        |> filepath

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
        new Repository(Repository.Clone(remote, unwrap target))
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
    let reset (opts: ResetMode) (repo: Repository) =
      try
        repo.Reset opts
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "reset")
          |> Either.fail

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
    let resetTo (opts: ResetMode) (commit: Commit) (repo: Repository) =
      try
        repo.Reset(opts, commit)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "resetTo")
          |> Either.fail

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
        let normalized =
          if Path.endsWith ".git" path then
            path
          else
            path </> filepath ".git"

        new Repository(unwrap normalized)
        |> Either.succeed
      with
        | :? RepositoryNotFoundException as exn  ->
          sprintf "%s: %s" exn.Message (unwrap path)
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
        Path.map Repository.Init path |> ignore
        repository path
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "init")
          |> Either.fail

    let add (repo: Repository) (path: FilePath) =
      try
        if Path.isPathRooted path then
          path
          |> sprintf "Path must be relative to the project root: %O"
          |> Error.asGitError (tag "add")
          |> Either.fail
        else
          if File.exists path then
            Path.map repo.Index.Add path
          Either.succeed ()
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "add")
          |> Either.fail

    let stage (repo: Repository) (path: FilePath) =
      try
        if Path.isPathRooted path then
          path
          |> Path.map (fun path -> Commands.Stage(repo, path))
          |> Either.succeed
        else
          sprintf "Paths must be absolute: %O" path
          |> Error.asGitError (tag "stage")
          |> Either.fail
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "stage")
          |> Either.fail

    let stageAll (repo: Repository)  =
      let _stage (ety: StatusEntry) =
        parentPath repo </> filepath ety.FilePath
        |> Path.map (fun path -> Commands.Stage(repo, path))
      try
        repo.RetrieveStatus()
        |> Seq.iter _stage
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "stageAll")
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

    /// ## pull
    ///
    /// Pull changes from given remote.
    ///
    /// ### Signature:
    /// - repo: Repository
    /// - remote: string
    ///
    /// Returns: Either<IrisError,MergeResult>

    let pull (repo: Repository) (remote: Remote) (signature: Signature) =
      try
        either {
          let options =
            let fopts = new FetchOptions()
            let popts = new PullOptions()
            let mopts = new MergeOptions()
            mopts.FastForwardStrategy <- new FastForwardStrategy()
            popts.FetchOptions <- fopts
            popts.MergeOptions <- mopts
            popts

          return Commands.Pull(repo, signature, options)
        }
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "pull")
          |> Either.fail

  //   ____             __ _
  //  / ___|___  _ __  / _(_) __ _
  // | |   / _ \| '_ \| |_| |/ _` |
  // | |__| (_) | | | |  _| | (_| |
  //  \____\___/|_| |_|_| |_|\__, |
  //                         |___/

  module Config =
    let private tag (str: string) = String.Format("Git.Config.{0}", str)

    let tryFindRemote (repo: Repository) (name: string) =
      repo.Network.Remotes
      |> Seq.tryFind (fun (remote: Remote) -> remote.Name = name)

    let remotes (repo: Repository) =
      repo.Network.Remotes
      |> Seq.fold (fun lst (remote: Remote) -> (remote.Name,remote) :: lst) []
      |> Map.ofList

    let addRemote (repo: Repository) (name: string) (url: string) =
      try
        repo.Network.Remotes.Add(name, url)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "addRemote")
          |> Either.fail

    let updateRemote (repo: Repository) (remote: Remote) (url: string) =
      try
        let update (updater: RemoteUpdater) =
          updater.Url <- url
        repo.Network.Remotes.Update(remote.Name, update)
        repo.Network.Remotes.[remote.Name]
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "updateRemote")
          |> Either.fail

    let delRemote (repo: Repository) (name: string) : Either<IrisError,unit> =
      repo.Network.Remotes.Remove name
      |> Either.succeed

#endif

//  ____  _                                             _
// |  _ \| | __ _ _   _  __ _ _ __ ___  _   _ _ __   __| |
// | |_) | |/ _` | | | |/ _` | '__/ _ \| | | | '_ \ / _` |
// |  __/| | (_| | |_| | (_| | | | (_) | |_| | | | | (_| |
// |_|   |_|\__,_|\__, |\__, |_|  \___/ \__,_|_| |_|\__,_|
//                |___/ |___/

#if INTERACTIVE

open System.IO
open LibGit2Sharp
open Iris.Core

let path = "/home/k/tmp/meh4"

Repository.Init path

let repo = new Repository(path)

Git.Config.addRemote repo "origin" "git://localhost:6000/meh/.git"

let origin: Remote =
  Git.Config.remotes repo
  |> Map.find "origin"

let master = Git.Branch.current repo
let fix_issue = Git.Repo.checkout "fix_issue" repo

Git.Branch.setTracked repo master origin

repo.Network.Remotes

Git.Repo.reset ResetMode.Soft repo.Reset(Reset)

File.WriteAllText(path </> "README", "9")

let branch = Git.Branch.current repo

let options =
  let fo = new FetchOptions()
  let po = new PullOptions()
  po.FetchOptions <- fo
  po

Git.Repo.clean repo
Git.Repo.stageAll repo

let commit = Git.Repo.commit repo "incremented again" User.Admin.Signature

repo.Network.Fetch("git@bitbucket.org:krgn/meh.git", ["master"], "hello")

Commands.Pull(repo, User.Admin.Signature, options)

repo.Network.Push(fix_issue |> Either.get)

#endif
