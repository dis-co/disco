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
open System.Net.NetworkInformation
open System.Runtime.CompilerServices

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

// * Network

[<RequireQualifiedAccess>]
module Network =

  // *** getHostName

  /// ## Get the current machine's host name
  ///
  /// Get the current machine's host name.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: string
  let getHostName () =
  #if FABLE_COMPILER
    Browser.window.location.hostname
  #else
    try
      System.Net.Dns.GetHostName()
    with
      | _ -> System.Environment.MachineName
  #endif

  #if !FABLE_COMPILER

  // *** getIpAddress

  /// ## getIpAddress
  ///
  /// Find and return the IP address of the current machine.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: IPAddress option
  let getIpAddress (_ : unit) : IPAddress option =
    let mutable outip : IPAddress option = None
    for iface in NetworkInterface.GetAllNetworkInterfaces() do
      if iface.NetworkInterfaceType = NetworkInterfaceType.Wireless80211 ||
        iface.NetworkInterfaceType = NetworkInterfaceType.Ethernet
      then
        for ip in iface.GetIPProperties().UnicastAddresses do
          if ip.Address.AddressFamily = Sockets.AddressFamily.InterNetwork
          then outip <- Some(ip.Address)
    outip

  #endif

// * Platform

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Platform =

  // ** isUnix

  /// ## isUnix
  ///
  /// Returns true if currently run on MacOS or other Unices.
  ///
  let isUnix : bool =
    int Environment.OSVersion.Platform
    |> fun p ->
      (p = 4) ||                         // Unix
      (p = 6) ||                         // MacOS
      (p = 128)                         // old Mono Unix


  // ** isWindows

  /// ## isWindows
  ///
  /// True if the current platform is not a unix.
  ///
  /// Returns: bool
  let isWindows = not isUnix

#endif


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

// * FileSystem

[<AutoOpen>]
module FileSystem =

  // *** </>

  /// ## </>
  ///
  /// Combine two FilePath (string) into one with the proper separator.
  ///
  /// ### Signature:
  /// - path1: first path
  /// - path2: second path
  ///
  /// Returns: FilePath (string)
  let (</>) p1 p2 =
    #if FABLE_COMPILER
    sprintf "%s/%s" p1 p2
    #else
    Path.Combine(p1, p2)
    #endif

  // *** moveFile

  #if !FABLE_COMPILER

  /// ## moveFile
  ///
  /// Move a file or directory from source to dest.
  ///
  /// ### Signature:
  /// - source: FilePath
  /// - dest: FilePath
  ///
  /// Returns: unit
  let moveFile (source: FilePath) (dest: FilePath) =
    try
      let info = new FileInfo(source)
      let attrs = info.Attributes
      if attrs.HasFlag(FileAttributes.Directory) then
        Directory.Move(source,dest)
      else
        File.Move(source, dest)
    with | _ -> ()

  #endif

  // *** rmDir

  #if !FABLE_COMPILER

  /// ## delete a file or directory
  ///
  /// recursively delete a directory or single File.
  ///
  /// ### Signature:
  /// - path: FilePath to delete
  ///
  /// Returns: Either<IrisError, unit>
  let rec rmDir path : Either<IrisError,unit>  =
    try
      let attrs = File.GetAttributes(path)
      if (attrs &&& FileAttributes.Directory) = FileAttributes.Directory then
        let children = DirectoryInfo(path).EnumerateFileSystemInfos()
        if children.Count() > 0 then
          either {
            do! Seq.fold
                  (fun (m: Either<IrisError, unit>) (child: FileSystemInfo) -> either {
                    return! rmDir child.FullName
                  })
                  (Right ())
                  children
            return Directory.Delete(path)
          }
        else
          Directory.Delete(path)
          |> Either.succeed
      else
        File.Delete path
        |> Either.succeed
    with
      | exn ->
        ("FileSystem.rmDir", exn.Message)
        |> IOError
        |> Either.fail

  #endif

  // *** mkDir

  #if !FABLE_COMPILER

  /// ## create a new directory
  ///
  /// Create a directory at Path.
  ///
  /// ### Signature:
  /// - path: FilePath
  ///
  /// Returns: Either<IrisError, unit>
  let mkDir path =
    try
      if not (Directory.Exists path) then
        path
        |> Directory.CreateDirectory
        |> ignore
        |> Either.succeed
      else
        Either.succeed ()
    with
      | exn ->
        ("FileSystem.mkDir", exn.Message)
        |> IOError
        |> Either.fail

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
    let now = DateTime.Now
    now.ToString("u")

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
    let epoch = new DateTime(1970, 1, 1)
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

// * Asset

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Asset =

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

  /// ## save
  ///
  /// Description
  ///
  /// ### Signature:
  /// - location: FilePath to asset
  /// - payload: string payload to save
  ///
  /// Returns: Either<IrisError,FileInfo>
  let save (location: FilePath) (payload: string) =
    either {
      try
        let info = FileInfo location
        do! FileSystem.mkDir info.Directory.FullName
        File.WriteAllText(location, payload, Encoding.UTF8)
        info.Refresh()
        return info
      with
        | exn ->
          return!
            ("Asset.save",exn.Message)
            |> AssetError
            |> Either.fail
    }

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
            ("Asset.delete",exn.Message)
            |> AssetError
            |> Either.fail
    }

  // ** load

  /// ## load
  ///
  /// Load a text file from disk. If the file could not be loaded,
  /// return IOError.
  ///
  /// ### Signature:
  /// - locationg: FilePath to asset
  ///
  /// Returns: Either<IrisError,string>
  let inline load (location: FilePath) : Either<IrisError, string> =
    either {
      if File.Exists location then
        try
          return File.ReadAllText location
        with
          | exn ->
            return!
              ("Asset.load",exn.Message)
              |> AssetError
              |> Either.fail
      else
        return!
          ("Asset.load",sprintf "File not found: %s" location)
          |> AssetError
          |> Either.fail
    }

#endif

// * Functional

#if FABLE_COMPILER

[<AutoOpen>]
module Functional =

  let flip (f: 'a -> 'b -> 'c) (b: 'b) (a: 'a) = f a b

#endif
