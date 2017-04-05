namespace Iris.Core

// * Imports

open System
open System.Text.RegularExpressions

#if FABLE_COMPILER

open Fable.Core
open Fable.Import

#else

open System.IO
open System.Net
open System.Linq
open System.Management
open System.Diagnostics
open System.Text
open System.Security.Cryptography
open System.Runtime.CompilerServices
open Hopac

#endif

// * List

[<RequireQualifiedAccess>]
module List =
  let reverse (lst : 'a list) : 'a list =
    let reverser acc elm = List.concat [[elm]; acc]
    List.fold reverser [] lst

// * Utils

[<AutoOpen>]
module Utils =

  // ** dispose

  #if FABLE_COMPILER

  /// ## Dispose of an object that implements the method Dispose
  ///
  /// This is slightly different to the .NET based version, as I have discovered problems with the
  /// `use` keyword of IDisposable members in FABLE_COMPILER. Hence we manage manualy and ensure that
  /// dispose reminds us that the object needs to have the member, not interface implemented.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  let inline dispose< ^t when ^t : (member Dispose : unit -> unit)> (o : ^t) =
    (^t : (member Dispose : unit -> unit) o)

  #else

  /// ## Dispose of an IDisposable object.
  ///
  /// Convenience function to call Dispose on an IDisposable.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  let dispose (o : 't when 't :> IDisposable) = o.Dispose()

  #endif

  // ** tryDispose

  #if !FABLE_COMPILER

  /// ## tryDispose
  ///
  /// Try to dispose a resource. Run passed handler if Dispose fails.
  ///
  /// ### Signature:
  /// - o: ^t to dispose of
  /// - handler: (Exception -> unit) handler to run on failure
  ///
  /// Returns: unit
  let tryDispose (o: 't when 't :> IDisposable) (handler: Exception -> unit) =
    try
      dispose o
    with
      | exn -> handler exn

  #endif

  // ** warn

  let warn = printfn "[WARNING] %s"


  // ** implement

  /// ## implement
  ///
  /// Fail with a FIXME.b
  ///
  /// ### Signature:
  /// - str: string call site to implement
  ///
  /// Returns: 'a
  let implement (str: string) =
    failwithf "FIXME: implement %s" str


  // ** toPair

  /// ## toPair on types with Id member
  ///
  /// Create a tuple from types that have an `Id` member/field.
  ///
  /// ### Signature:
  /// - a: type with member `Id` to create tuple of
  ///
  /// Returns: Id * ^t
  let inline toPair< ^t, ^i when ^t : (member Id : ^i)> (a: ^t) : ^i * ^t =
    ((^t : (member Id : ^i) a), a)

// * String

[<RequireQualifiedAccess>]
module String =

  /// ## replace
  ///
  /// Replace `oldchar` with `newchar` in `str`.
  ///
  /// ### Signature:
  /// - oldchar: char to replace
  /// - newchar: char to substitute
  /// - str: string to work on
  ///
  /// Returns: string
  let replace (oldchar: char) (newchar: char) (str: string) =
    str.Replace(oldchar, newchar)

  // *** join

  /// ## join
  ///
  /// Join a string using provided separator.
  ///
  /// ### Signature:
  /// - sep: string separator
  /// - arr: string array to join
  ///
  /// Returns: string
  let join sep (arr: string array) = String.Join(sep, arr)

  // *** toLower

  /// ## toLower
  ///
  /// Transform all upper-case characters into lower-case ones.
  ///
  /// ### Signature:
  /// - string: string to transform
  ///
  /// Returns: string
  #if FABLE_COMPILER

  [<Emit("$0.toLowerCase()")>]
  let toLower (_: string) : string = failwith "ONLY JS"

  #else

  let inline toLower< ^a when ^a : (member ToLower : unit -> ^a)> str =
    (^a : (member ToLower : unit -> ^a) str)

  #endif

  // *** trim

  #if !FABLE_COMPILER

  /// ## trim
  ///
  /// Trim whitespace off of strings beginning and end.
  ///
  /// ### Signature:
  /// - string: string to trim
  ///
  /// Returns: string
  let inline trim< ^a when ^a : (member Trim : unit -> ^a)> str =
    (^a : (member Trim : unit -> ^a) str)

  #endif

  // *** toUpper

  #if !FABLE_COMPILER

  /// ## toUpper
  ///
  /// Transform all lower-case characters in a string to upper-case.
  ///
  /// ### Signature:
  /// - string: string to transformb
  ///
  /// Returns: string
  let inline toUpper< ^a when ^a : (member ToUpper : unit -> ^a)> str =
    (^a : (member ToUpper : unit -> ^a) str)

  #endif

  // *** split

  /// ## split
  ///
  /// Split a string into an array of strings by a series of characters in an array.
  ///
  /// ### Signature:
  /// - chars: char array
  /// - str: string to split
  ///
  /// Returns: string array
  let split (chars: char array) (str: string) =
    str.Split(chars)

  // *** indent

  #if !FABLE_COMPILER

  /// ## indent
  ///
  /// Indent a string by the defined number of spaces.
  ///
  /// ### Signature:
  /// - num: int number of spaces to indent by
  /// - str: string to indent
  ///
  /// Returns: string
  let indent (num: int) (str: string) =
    let spaces = Array.fold (fun m _ -> m + " ") "" [| 1 .. num |]
    str.Split('\n')
    |> Array.map (fun line -> spaces + line)
    |> Array.fold (fun m line -> sprintf "%s\n%s" m line) ""

  #endif

  // *** subString

  /// ## subString
  ///
  /// Return a sub-section of a passed string.
  ///
  /// ### Signature:
  /// - index: int index where to start in string
  /// - length: int number of characters to include
  /// - str: string to slice
  ///
  /// Returns: string
  let subString (index: int) (length: int) (str: string) =
    if index >= 0 && index < str.Length then
      let length = if length < str.Length then length else str.Length
      str.Substring(index, length)
    else
      ""
  // *** santitize

  /// ## sanitize
  ///
  /// Sanitize the given string by replacing any punktuation or other special characters with
  /// undercores.
  ///
  /// ### Signature:
  /// - payload: string to sanitize
  ///
  /// Returns: string
  let sanitize (payload: string) =
    let regex = new Regex("(\.|\ |\*|\^)")
    if regex.IsMatch(payload)
    then regex.Replace(payload, "_")
    else payload


  /// *** encodeBase64

  let encodeBase64 (bytes: Binary.Buffer) =
    #if FABLE_COMPILER
    let mutable str = ""
    let arr = Fable.Import.JS.Uint8Array.Create(bytes)
    for i in 0 .. (int arr.length - 1) do
      str <- str + Fable.Import.JS.String.fromCharCode arr.[i]
    Fable.Import.Browser.window.btoa str
    #else
    Convert.ToBase64String(bytes)
    #endif

  /// *** decodeBase64

  let decodeBase64 (buffer: string) : Binary.Buffer =
    #if FABLE_COMPILER
    let binary = Fable.Import.Browser.window.atob buffer
    let bytes = Fable.Import.JS.Uint8Array.Create(float binary.Length)
    for i in 0 .. (binary.Length - 1) do
      bytes.[i] <- charCodeAt binary i
    bytes.buffer
    #else
    Convert.FromBase64String(buffer)
    #endif

// * Path

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Path =

  let baseName (path: FilePath) =
    Path.GetFileName path

  let dirName (path: FilePath) =
    Path.GetDirectoryName path

#endif
// * Time

//  _____ _
// |_   _(_)_ __ ___   ___
//   | | | | '_ ` _ \ / _ \
//   | | | | | | | | |  __/
//   |_| |_|_| |_| |_|\___|

[<RequireQualifiedAccess>]
module Time =

  // *** createTimestamp

  /// ## createTimestamp
  ///
  /// Create a timestamp string for DateTime.Now.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: string
  let createTimestamp () =
    let now = DateTime.UtcNow
    now.ToString("o")

  // *** unixTime

  /// ## unixTime
  ///
  /// Return current unix-style epoch time.
  ///
  /// ### Signature:
  /// - date: DateTime to get epoch for
  ///
  /// Returns: int64
  let unixTime (date: DateTime) =
    let date = if date.Kind = DateTimeKind.Local then date.ToUniversalTime() else date
    let epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
    (date.Ticks - epoch.Ticks) / TimeSpan.TicksPerMillisecond

  let parse (str: string) =
    match DateTime.TryParse(str) with
    | (true, date) -> Either.succeed date
    | _ ->
      sprintf "Could not parse date string: %s" str
      |> Error.asParseError "Time.parse"
      |> Either.fail

// * Process

#if !FABLE_COMPILER

///////////////////////////////////////////////////
//  ____                                         //
// |  _ \ _ __ ___   ___ ___  ___ ___  ___  ___  //
// | |_) | '__/ _ \ / __/ _ \/ __/ __|/ _ \/ __| //
// |  __/| | | (_) | (_|  __/\__ \__ \  __/\__ \ //
// |_|   |_|  \___/ \___\___||___/___/\___||___/ //
///////////////////////////////////////////////////

[<RequireQualifiedAccess>]
module Process =

  // *** tryFind

  /// ## tryFind
  ///
  /// Try to find a Process by its process id.
  ///
  /// ### Signature:
  /// - pid: int
  ///
  /// Returns: Process option
  let tryFind (pid: int) =
    try
      Process.GetProcessById(pid)
      |> Some
    with
      | _ -> None

  // *** kill

  /// ## kill
  ///
  /// Kill the process with the specified PID.
  ///
  /// ### Signature:
  /// - pid: int (PID) of process to kill
  ///
  /// Returns: unit
  let rec kill (pid : int) =
    if Platform.isUnix then
      /// On Mono we need to kill the parent and children
      Process.Start("pkill", sprintf "-TERM -P %d" pid)
      |> ignore
    else
      try
        /// On Windows, we can use this trick to kill all child processes and finally the parent.
        let query = sprintf "Select * From Win32_Process Where ParentProcessID=%d" pid
        let searcher = new ManagementObjectSearcher(query);

        // kill all child processes
        for mo in searcher.Get() do
          // have to use explicit conversion using Convert here, or it breaks
          mo.GetPropertyValue "ProcessID"
          |> Convert.ToInt32
          |> kill

        // kill parent process
        let proc = Process.GetProcessById(pid)
        proc.Kill();
      with
        | _ -> ()

    // wait for this process to end properly
    while tryFind pid |> Option.isSome do
      System.Threading.Thread.Sleep 1


  /// ## isRunning
  ///
  /// Return true if a process with the given PID is currently running.
  ///
  /// ### Signature:
  /// - pid: int process id
  ///
  /// Returns: bool
  let isRunning (pid: int) =
    match tryFind pid with
    | Some _ -> true
    | _      -> false

#endif

// * Security

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Crypto =

  /// ## toString
  ///
  /// Turn a byte array into a string.
  ///
  /// ### Signature:
  /// - buf: byte array to turn into a string
  ///
  /// Returns: string
  let private toString (buf: byte array) =
    let hashedString = new StringBuilder ()
    for byte in buf do
      hashedString.AppendFormat("{0:x2}", byte)
      |> ignore
    hashedString.ToString()

  /// ## sha1sum
  ///
  /// Compute the SHA1 checksum of the passed byte array.
  ///
  /// ### Signature:
  /// - buf: byte array to checksum
  ///
  /// Returns: Hash
  let sha1sum (buf: byte array) : Hash =
    let sha256 = new SHA1Managed()
    sha256.ComputeHash(buf)
    |> toString

  /// ## sha256sum
  ///
  /// Compute the SHA256 checksum of the passed byte array.
  ///
  /// ### Signature:
  /// - buf: byte array to checksum
  ///
  /// Returns: Hash
  let sha256sum (buf: byte array) : Hash =
    let sha256 = new SHA256Managed()
    sha256.ComputeHash(buf)
    |> toString

  /// ## generateSalt
  ///
  /// Generate a random salt value for securing passwords.
  ///
  /// ### Signature:
  /// - n: int number of bytes to generate
  ///
  /// Returns: Salt
  let generateSalt (n: int) : Salt =
    let buf : byte array = Array.zeroCreate n
    let random = new Random()
    random.NextBytes(buf)
    sha1sum buf

  /// ## hashPassword
  ///
  /// Generate a salted and hashed checksum for the given password.
  ///
  /// ### Signature:
  /// - pw: string password to salt and hash
  /// - salt: string salt value to concatenate pw with
  ///
  /// Returns: string
  let hashPassword (pw: Password) (salt: Salt) : Hash =
    Encoding.UTF8.GetBytes(salt + pw)
    |> sha256sum

  /// ## hash
  ///
  /// Hashes the given password with a generated random salt value. Returns a tuple of the generated
  /// hash and the salt used in the process.
  ///
  /// ### Signature:
  /// - pw: Password to hash
  ///
  /// Returns: Hash * Salt
  let hash (pw: Password) : Hash * Salt =
    let salt = generateSalt 50
    hashPassword pw salt, salt

#endif

// * Git

#if !FABLE_COMPILER && !IRIS_NODES

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
        let normalized =
          if path.EndsWith ".git" then
            path
          else
            path </> ".git"

        new Repository(normalized)
        |> Either.succeed
      with
        | :? RepositoryNotFoundException as exn  ->
          sprintf "%s: %s" exn.Message path
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
        if Path.IsPathRooted filepath then
          Commands.Stage(repo, filepath)
          |> Either.succeed
        else
          sprintf "Paths must be absolute: %s" filepath
          |> Error.asGitError (tag "stage")
          |> Either.fail
      with
        | exn ->
          exn.Message
          |> Error.asGitError (tag "stage")
          |> Either.fail

    let stageAll (repo: Repository)  =
      let _stage (ety: StatusEntry) =
        parentPath repo </> ety.FilePath
        |> fun path -> Commands.Stage(repo, path)
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

#endif

// * Asset

[<RequireQualifiedAccess>]
module Asset =

  let private tag (site: string) = sprintf "Asset.%s" site

  // ** path

  /// ## path
  ///
  /// Return the realive path the given asset should be saved under.
  ///
  /// ### Signature:
  /// - thing: ^t
  ///
  /// Returns: FilePath
  let inline path< ^t when ^t : (member AssetPath : FilePath)> (thing: ^t) =
    (^t : (member AssetPath: FilePath) thing)


  // ** write

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## write
  ///
  /// Description
  ///
  /// ### Signature:
  /// - location: FilePath to asset
  /// - payload: string payload to save
  ///
  /// Returns: Either<IrisError,FileInfo>
  let write (location: FilePath) (payload: StringPayload) =
    either {
      try
        let data = match payload with | Payload data -> data
        let info = FileInfo location
        do! FileSystem.mkDir info.Directory.FullName
        File.WriteAllText(location, data, Encoding.UTF8)
        info.Refresh()
        return info
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError (tag "write")
            |> Either.fail
    }

  #endif

  // ** delete

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## delete
  ///
  /// Delete an asset from disk.
  ///
  /// ### Signature:
  /// - location: FilePath to asset
  ///
  /// Returns: Either<IrisError,bool>
  let delete (location: FilePath) =
    either {
      try
        if File.Exists location then
          File.Delete location
          return true
        else
          return false
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError (tag "delete")
            |> Either.fail
    }

  #endif

  // ** read

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## read
  ///
  /// Load a text file from disk. If the file could not be loaded,
  /// return IOError.
  ///
  /// ### Signature:
  /// - locationg: FilePath to asset
  ///
  /// Returns: Either<IrisError,string>
  let read (location: FilePath) : Either<IrisError, string> =
    either {
      if File.Exists location then
        try
          return File.ReadAllText location
        with
          | exn ->
            return!
              exn.Message
              |> Error.asAssetError (tag "read")
              |> Either.fail
      else
        return!
          sprintf "File not found: %s" location
          |> Error.asAssetError (tag "read")
          |> Either.fail
    }
  #endif

  // ** save

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline save< ^t when ^t : (member Save: FilePath -> Either<IrisError, unit>)>
                 (path: FilePath)
                 (t: ^t) =
    (^t : (member Save: FilePath -> Either<IrisError, unit>) (t, path))

  #endif

  // ** saveMap

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline saveMap (basepath: FilePath) (guard: Either<IrisError,unit>) _ (t: ^t) =
    either {
      do! guard
      do! save basepath t
    }

  #endif

  // ** load

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline load< ^t when ^t : (static member Load: FilePath -> Either<IrisError, ^t>)>
                 (path: FilePath) =
    (^t : (static member Load: FilePath -> Either<IrisError, ^t>) path)

  #endif

  // ** loadWithMachine

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline loadWithMachine< ^t when ^t : (static member Load: FilePath * IrisMachine -> Either<IrisError, ^t>)>
                 (path: FilePath)
                 (machine: IrisMachine) =
    (^t : (static member Load: FilePath * IrisMachine -> Either<IrisError, ^t>) (path,machine))

  #endif

  // ** loadAll

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline loadAll< ^t when ^t : (static member LoadAll: FilePath -> Either<IrisError, ^t array>)>
                    (basePath: FilePath) =
    (^t : (static member LoadAll: FilePath -> Either<IrisError, ^t array>) basePath)

  #endif

  // ** commit

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline commit (basepath: FilePath) (msg: string) (signature: LibGit2Sharp.Signature) (t: ^t) =
    either {
      use! repo = Git.Repo.repository basepath

      let target =
        if Path.IsPathRooted basepath then
          basepath </> path t
        else
          Path.GetFullPath basepath </> path t

      do! Git.Repo.stage repo target
      let! commit = Git.Repo.commit repo msg signature
      return commit
    }

  #endif

  // ** saveWithCommit

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline saveWithCommit (basepath: FilePath) (signature: LibGit2Sharp.Signature) (t: ^t) =
    either {
      do! save basepath t
      let filename = path t |> Path.GetFileName
      let msg = sprintf "%s saved %A" signature.Name filename
      return! commit basepath msg signature t
    }

  #endif

  // ** deleteWithCommit

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline deleteWithCommit (basepath: FilePath) (signature: LibGit2Sharp.Signature) (t: ^t) =
    either {
      let filepath = basepath </> path t
      let! _ = delete filepath
      let msg = sprintf "%s deleted %A" signature.Name (Path.GetFileName filepath)
      return! commit basepath msg signature t
    }

  #endif


// * Functional

#if FABLE_COMPILER

[<AutoOpen>]
module Functional =

  let flip (f: 'a -> 'b -> 'c) (b: 'b) (a: 'a) = f a b

#endif

// * Async

#if !FABLE_COMPILER

[<AutoOpen>]
module Async =

  let asynchronously (f: unit -> unit) =
    #if IRIS_NODES
    async { f() } |> Async.Start
    #else
    job { f() } |> Hopac.queue
    #endif

#endif
