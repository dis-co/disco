namespace Disco.Core

// * Imports

#if !FABLE_COMPILER

open System
open System.IO
open System.Diagnostics
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
module Git =
  open Path

  // ** tag

  let private tag (str: string) = String.format "Git.{0}" str

  // ** runGit

  let runGit (basepath: string) (cmd: string) (origin: string) (branch: string) =
    use proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- cmd + " " + origin + " "  + branch
    proc.StartInfo.WorkingDirectory <- basepath
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    if proc.Start() then
      let lines = ResizeArray()
      while not proc.StandardOutput.EndOfStream do
        proc.StandardOutput.ReadLine()
        |> lines.Add
      while not proc.StandardError.EndOfStream do
        proc.StandardError.ReadLine()
        |> lines.Add
      proc.WaitForExit()
      lines.ToArray()
      |> String.join "\n"
    else
      proc.WaitForExit()
      proc.StandardError.ReadToEnd()
      |> failwithf "Error: %s"

  // ** lsRemote

  /// ## lsRemote
  ///
  /// List all remote references and return a sequence of them.
  ///
  /// ### Signature:
  /// - url: string
  ///
  /// Returns: seq<Reference>

  let lsRemote (url: string) =
    try
      url
      |> Repository.ListRemoteReferences
      |> Seq.cast<Reference>
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asGitError (tag "lsRemote")
        |> Either.fail

  // ** Branch module

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

    // *** tag

    let private tag (str:string) = String.format "Git.Branch.{0}" str

    // *** create

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

    // *** current

    /// ## Get the currently checked out branch
    ///
    /// Get the currently checked out Branch of a given Repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Branch
    let current (repo: Repository) = repo.Head

    // *** tracked

    /// ## Get the remote tracked branch
    ///
    /// Gets the remotely tracked branch, inidicating that none was set by returning `None`.
    ///
    /// ### Signature:
    /// - branch: Branch
    ///
    /// Returns: Either<string,Branch>
    let tracked (branch: Branch) : Either<DiscoError,Branch> =
      match branch.TrackedBranch with
        | null      ->
          "No tracked branch"
          |> Error.asGitError (tag "tracked")
          |> Either.fail
        | branch -> Either.succeed branch

    // *** tracking

    /// ## Get details about the remote tracking branch
    ///
    /// Get details about the remote tracked by the passed in branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get details for
    ///
    /// Returns: BranchTrackingDetails option
    let tracking (branch: Branch) : Either<DiscoError,BranchTrackingDetails> =
      match branch.TrackingDetails with
        | null       ->
          "No tracked branch"
          |> Error.asGitError (tag "tracking")
          |> Either.fail
        | details -> Either.succeed details

    // *** tip

    /// ## Get the lastest commit object.
    ///
    /// The tip of the branch is the latest commit made to it.
    ///
    /// ### Signature:
    /// - branch: Branch to get the tip of
    ///
    /// Returns: Commit
    let tip (branch: Branch) = branch.Tip

    // *** remoteName

    /// ## Remote name of current branch
    ///
    /// Get the remote name of current branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get remote name for
    ///
    /// Returns: string
    let remoteName (branch: Branch) = branch.RemoteName

    // *** reference

    /// ## Get Reference object for Branch
    ///
    /// Get the `Reference`
    ///
    /// ### Signature:
    /// - branch: Branch to get reference for
    ///
    /// Returns: Reference
    let reference (branch: Branch) = branch.Reference

    // *** item

    let item (path: FilePath) (branch: Branch) =
      branch.Item (unwrap path)

    // *** isRemote

    /// ## Is the passed branch a remote?
    ///
    /// Return true if the passed branch is a remote.
    ///
    /// ### Signature:
    /// - branch: Branch
    ///
    /// Returns: boolean
    let isRemote (branch: Branch) = branch.IsRemote

    // *** isTracking

    /// ## Check if branch tracks a remote
    ///
    /// Return true if the passed branch tracks any remote branch.
    ///
    /// ### Signature:
    /// - branch: Branch to check
    ///
    /// Returns: boolean
    let isTracking (branch: Branch) = branch.IsRemote

    // *** isCurrentRepositoryHead

    /// ## Check if branch is current repository head.
    ///
    /// Check if the passed branch is the current repository HEAD.
    ///
    /// ### Signature:
    /// - branch: Branch to check
    ///
    /// Returns: boolean
    let isCurrentRepositoryHead (branch: Branch) = branch.IsCurrentRepositoryHead

    // *** canonicalName

    /// ## Get the canonical name of a branch
    ///
    /// Get the canonical name of a branch.
    ///
    /// ### Signature:
    /// - branch: Branch
    ///
    /// Returns: string
    let canonicalName (branch: Branch) = branch.CanonicalName

    // *** upstreamCanonicalName

    /// ## Get the canonical name of upstream branch
    ///
    /// Get the canonical name of the passed branches upstream branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get name for
    ///
    /// Returns: string
    let upstreamCanonicalName (branch: Branch) = branch.UpstreamBranchCanonicalName

    // *** friendlyName

    /// ## Get friendly name of Branch
    ///
    /// Get the human-friendly name of a branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get name of
    ///
    /// Returns: string
    let friendlyName (branch: Branch) = branch.FriendlyName

    // *** commits

    /// ## Get commit log of branch
    ///
    /// Get the commit log for the passed branch.
    ///
    /// ### Signature:
    /// - branch: Branch to get commits for
    ///
    /// Returns: ICommitLog
    let commits (branch: Branch) : seq<Commit> =
      branch.Commits |> Seq.cast<Commit>

    // *** setTracked

    /// ## setTracked
    ///
    /// Updates tracking information for all branches
    ///
    /// ### Signature:
    /// - remote: string
    /// - upstream: string
    ///
    /// Returns: Either<DiscoError, unit>

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

  // ** Repo

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

    // *** tag

    let private tag (str: string) = String.format "Git.Repo.{0}" str

    // *** path

    let path (repo: Repository) =
      repo.Info.Path

    // *** parentPath

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

    // *** tags

    /// ## Get all tags for repository
    ///
    /// Look up and enumerate all tags in the passed repository
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: TagsCollection
    let tags (repo: Repository) : seq<Tag> =
      repo.Tags |> Seq.cast<Tag>

    // *** submodules

    /// ## Get submodules of a repository
    ///
    /// Get all registered submodules for a collection
    ///
    /// ### Signature:
    /// - repo: Repository to get submodules for
    ///
    /// Returns: SubmoduleCollection
    let submodules (repo: Repository) : seq<Submodule> =
      repo.Submodules |> Seq.cast<Submodule>

    // *** setReceivePackConfig

    #if !FABLE_COMPILER && !DISCO_NODES

    let setReceivePackConfig (repo: Repository) =
      try
        repo.Config.Set("receive.denyCurrentBranch", "updateInstead")
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "setReceivePackConfig")
          |> Either.fail

    #endif

    // *** clone

    /// ## clone
    ///
    /// Clone a remote repository.
    ///
    /// ### Signature:
    /// - target: FilePath to target directory
    /// - remote: string specifiying the remote repository address
    ///
    /// Returns: Either<DiscoError, Respository>
    let clone (target: FilePath) (remote: string) =
      try
        either {
          let path = Repository.Clone(remote, unwrap target)
          let repo = new Repository(path)
          do! setReceivePackConfig repo
        }
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "clone")
          |> Either.fail

    // *** branches

    /// ## Get all branches in repository.
    ///
    /// Get all branches in repository.
    ///
    /// ### Signature:
    /// - repo: Repository to get branches for
    ///
    /// Returns: Branch list
    let branches (repo: Repository) : seq<Branch> =
      repo.Branches |> Seq.cast<Branch>

    // *** stashes

    /// ## Get all stashes in this repository
    ///
    /// Gets all stashes in current repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Stash list
    let stashes (repo: Repository) : seq<Stash> =
      repo.Stashes |> Seq.cast<Stash>

    // *** revert

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

    // *** reset

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

    // *** resetTo

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

    // *** clean

    /// ## Clean repository
    ///
    /// Remove all untracked files from repository.
    ///
    /// ### Signature:
    /// - repo: Repository to clean
    ///
    /// Returns: unit
    let clean (repo: Repository) = repo.RemoveUntrackedFiles()

    // *** refs

    /// ## List all Refs in repository
    ///
    /// Lists all Reference objects in a repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Reference list
    let refs (repo: Repository) : seq<Reference> =
      repo.Refs |> Seq.cast<Reference>

    // *** database

    /// ## Get the object database for repository
    ///
    /// get all objects in repository
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: ObjectDatabase
    let database (repo: Repository) = repo.ObjectDatabase

    // *** index

    /// ### Signature:
    /// - repo: Repository
    /// - arg: arg
    /// - arg: arg
    ///
    /// Returns: Index
    let index (repo: Repository) = repo.Index

    // *** ignored

    /// ## Gets the currently ignored files
    ///
    /// Gets the currently ignored files for a repository.
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Ingnore
    let ignored (repo: Repository) = repo.Ignore

    // *** diff

    /// ## Get a diff between current working changes and last commit
    ///
    /// Get a diff for current changes
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Diff
    let diff (repo: Repository) = repo.Diff

    // *** config

    /// ## Get repository configuration
    ///
    /// Gets the current repository configuration
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: Configuration
    let config (repo: Repository) = repo.Config

    // *** merge

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

    // *** checkout

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
        spec
        |> String.format "{0} not found"
        |> Error.asGitError (tag "checkout")
        |> Either.fail
      | branch -> Either.succeed branch

    // *** repository

    /// ## Find and return Repository object
    ///
    /// Get a repo for the specified path. Indicate failure to find it by returning `None`
    ///
    /// ### Signature:
    /// - path: FilePath to search for the .git folder
    ///
    /// Returns: Repository option
    let repository (path: FilePath) : Either<DiscoError,Repository> =
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
          exn.Message
          |> String.format (unwrap path + ": {0}")
          |> Error.asGitError (tag "repository")
          |> Either.fail
        | exn ->
          exn.Message
          |> Error.asGitError (tag "repository")
          |> Either.fail

    // *** init

    /// ## Initialize a new repository
    ///
    /// Initialize a new repository at the location specified
    ///
    /// ### Signature:
    /// - path: FilePath pointing to the target directory
    ///
    /// Returns: Either<DiscoError<string>,Repository>
    let init (path: FilePath) =
      try
        Path.map Repository.Init path |> ignore
        repository path
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "init")
          |> Either.fail

    // *** add

    let add (repo: Repository) (path: FilePath) =
      try
        if Path.isPathRooted path then
          path
          |> String.format "Path must be relative to the project root: {0}"
          |> Error.asGitError (tag "add")
          |> Either.fail
        else
          if File.exists path || Directory.exists path then
            runGit repo.Info.WorkingDirectory "add" "." ""
            |> Either.ignore
          else
            Either.succeed ()
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "add")
          |> Either.fail

    // *** stage

    let stage (repo: Repository) (path: FilePath) =
      try
        if Path.isPathRooted path && not (repo.Ignore.IsPathIgnored (unwrap path))then
          runGit repo.Info.WorkingDirectory "stage" "." ""
          |> Either.ignore
        else
          path
          |> String.format "Paths must be absolute: {0}"
          |> Error.asGitError (tag "stage")
          |> Either.fail
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "stage")
          |> Either.fail

    // *** stageAll

    let stageAll (repo: Repository)  =
      let _stage (ety: StatusEntry) =
        if not (repo.Ignore.IsPathIgnored ety.FilePath) then
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

    // *** commit

    let commit (repo: Repository) (msg: string) (committer: Signature) =
      try
        repo.Commit(msg, committer, committer)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "commit")
          |> Either.fail

    // *** status

    /// ## Retrieve current repository status object
    ///
    /// Retrieve status information on the current repository
    ///
    /// ### Signature:
    /// - repo: Repository to fetch status for
    ///
    /// Returns: RepositoryStatus
    let status (repo: Repository) : Either<DiscoError,RepositoryStatus> =
      try
        repo.RetrieveStatus()
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "status")
          |> Either.fail

    // *** isDirty

    /// ## Check if repository is currently dirty
    ///
    /// Check if the current repository is dirty or nor.
    ///
    /// ### Signature:
    /// - repo: Repository to check
    ///
    /// Returns: boolean
    let isDirty (repo: Repository) : Either<DiscoError, bool> =
      either {
        let! status = status repo
        return status.IsDirty
      }

    // *** untracked

    /// ## untracked
    ///
    /// List all untracked files in the repository
    ///
    /// ### Signature:
    /// - repo: Repository
    ///
    /// Returns: seq<StatusEntry>
    let untracked (repo: Repository) : Either<DiscoError,seq<StatusEntry>> =
      either {
        let! status = status repo
        return status.Untracked
      }

    // *** commits

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

    // *** elementAt

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
    let elementAt (idx: int) (t: IQueryableCommitLog) : Either<DiscoError,Commit> =
      try
        t.ElementAt(idx)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "elementAt")
          |> Either.fail

    // *** commitCount

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

    // *** push

    let push (repo: Repository) (remote: Remote) =
      try
        let branch = Branch.current repo
        let basepath = Path.GetDirectoryName repo.Info.Path
        branch.FriendlyName
        |> runGit basepath "push" remote.Name
        |> Either.ignore
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "push")
          |> Either.fail

    // *** pull

    /// ## pull
    ///
    /// Pull changes from given remote.
    ///
    /// ### Signature:
    /// - repo: Repository
    /// - remote: string
    ///
    /// Returns: Either<DiscoError,MergeResult>

    let pull (repo: Repository) (signature: Signature) =
      try
        either {
          let options =
            let fopts = FetchOptions()
            let popts = PullOptions()
            let mopts = MergeOptions()
            mopts.MergeFileFavor <- MergeFileFavor.Theirs
            mopts.FastForwardStrategy <- FastForwardStrategy()
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

    // *** lsRemote

    let lsRemote (repo: Repository) (remote: Remote) =
      try
        remote
        |> repo.Network.ListReferences
        |> Seq.cast<Reference>
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "lsRemote")
          |> Either.fail

  // ** Config

  //   ____             __ _
  //  / ___|___  _ __  / _(_) __ _
  // | |   / _ \| '_ \| |_| |/ _` |
  // | |__| (_) | | | |  _| | (_| |
  //  \____\___/|_| |_|_| |_|\__, |
  //                         |___/

  module Config =

    // *** tag

    let private tag (str: string) = String.Format("Git.Config.{0}", str)

    // *** tryFindRemote

    let tryFindRemote (repo: Repository) (name: string) =
      repo.Network.Remotes
      |> Seq.tryFind (fun (remote: Remote) -> remote.Name = name)

    // *** remotes

    let remotes (repo: Repository) : Map<string,Remote> =
      repo.Network.Remotes
      |> Seq.cast<Remote>
      |> Seq.map (fun (remote: Remote) -> remote.Name,remote)
      |> Map.ofSeq

    // *** addRemote

    let addRemote (repo: Repository) (name: string) (url: Url) =
      try
        repo.Network.Remotes.Add(name, unwrap url)
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "addRemote")
          |> Either.fail

    // *** updateRemote

    let updateRemote (repo: Repository) (remote: Remote) (url: Url) =
      try
        let update (updater: RemoteUpdater) =
          updater.Url <- unwrap url
        repo.Network.Remotes.Update(remote.Name, update)
        repo.Network.Remotes.[remote.Name]
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "updateRemote")
          |> Either.fail

    // *** delRemote

    let delRemote (repo: Repository) (name: string) : Either<DiscoError,unit> =
      repo.Network.Remotes.Remove name
      |> Either.succeed

  #endif

// * Playground

//  ____  _                                             _
// |  _ \| | __ _ _   _  __ _ _ __ ___  _   _ _ __   __| |
// | |_) | |/ _` | | | |/ _` | '__/ _ \| | | | '_ \ / _` |
// |  __/| | (_| | |_| | (_| | | | (_) | |_| | | | | (_| |
// |_|   |_|\__,_|\__, |\__, |_|  \___/ \__,_|_| |_|\__,_|
//                |___/ |___/

#if INTERACTIVE

open System.IO
open LibGit2Sharp
open Disco.Core

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
