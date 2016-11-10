namespace Iris.Core

// * Imports

open System
open System.IO
open System.Net
open System.Linq
open System.Management
open System.Diagnostics
open System.Net.NetworkInformation
open System.Text.RegularExpressions
open System.Runtime.CompilerServices

#if JAVASCRIPT

open Fable.Core

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

  #if JAVASCRIPT

  /// ## Dispose of an object that implements the method Dispose
  ///
  /// This is slightly different to the .NET based version, as I have discovered problems with the
  /// `use` keyword of IDisposable members in JAVASCRIPT. Hence we manage manualy and ensure that
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

  #if !JAVASCRIPT

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

  // ** isLinux

  #if !JAVASCRIPT

  let isLinux : bool =
    int Environment.OSVersion.Platform
    |> fun p ->
      (p = 4) || (p = 6) || (p = 128)

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

  // ** santitizeName

  #if !JAVASCRIPT

  /// ## sanitizeName
  ///
  /// Sanitize the given string by removing any punktuation or other special characters.
  ///
  /// ### Signature:
  /// - name: string to sanitize
  ///
  /// Returns: string
  let sanitizeName (name : string) =
    let regex = new Regex("(\.|\ |\*|\^)")
    if regex.IsMatch(name)
    then regex.Replace(name, "_")
    else name

  #endif

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

  #if !JAVASCRIPT

  /// ## Get the current machine's host name
  ///
  /// Get the current machine's host name.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: string
  let getHostName () =
    System.Net.Dns.GetHostName()

  #endif

  // *** getIpAddress

  #if !JAVASCRIPT

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


// * String

[<AutoOpen>]
module String =

  // *** logger

  #if !JAVASCRIPT

  /// ## logger
  ///
  /// Formant and output string log messages to the console.
  ///
  /// ### Signature:
  /// - tag: string tag identifying the call site
  /// - str: string payload to output
  ///
  /// Returns: unit
  let logger (tag : string) (str : string) : unit =
    let def = tag.Length + 2
    let verbose = Environment.GetEnvironmentVariable IRIS_VERBOSE
    if not (isNull verbose) && bool.Parse(verbose)
    then
      let mutable offset = 0

      try
        offset <- int (Environment.GetEnvironmentVariable IRIS_LOGGING_OFFSET)
      with
        | _ ->
          offset <- def
          Environment.SetEnvironmentVariable(IRIS_LOGGING_OFFSET, string offset)

      if def > offset
      then
        offset <- def
        Environment.SetEnvironmentVariable(IRIS_LOGGING_OFFSET, string offset)

      let ws = Array.fold (fun m _ -> m + " ") "" [| 0 .. (offset - def) |]
      printfn "[%s]%s%s" tag ws str

  #endif

  // *** toLower

  /// ## toLower
  ///
  /// Transform all upper-case characters into lower-case ones.
  ///
  /// ### Signature:
  /// - string: string to transform
  ///
  /// Returns: string
  #if JAVASCRIPT

  [<Emit("$0.toLowerCase()")>]
  let inline toLower (_: string) : string = failwith "ONLY JS"

  #else

  let inline toLower< ^a when ^a : (member ToLower : unit -> ^a)> str =
    (^a : (member ToLower : unit -> ^a) str)

  #endif

  // *** trim

  #if !JAVSCRIPT

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

  #if !JAVASCRIPT

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

  #if !JAVASCRIPT

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

  #endif

  // *** indent

  #if !JAVASCRIPT

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


// * FileSystem

#if !JAVASCRIPT

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
  let (</>) p1 p2 = System.IO.Path.Combine(p1, p2)

  // *** rmDir

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
      let attrs = IO.File.GetAttributes(path)
      if (attrs &&& IO.FileAttributes.Directory) = IO.FileAttributes.Directory then
        let children = IO.DirectoryInfo(path).EnumerateFileSystemInfos()
        if children.Count() > 0 then
          either {
            do! Seq.fold
                  (fun (m: Either<IrisError, unit>) (child: FileSystemInfo) -> either {
                    return! rmDir child.FullName
                  })
                  (Right ())
                  children
            return System.IO.Directory.Delete(path)
          }
        else
          System.IO.Directory.Delete(path)
          |> Either.succeed
      else
        System.IO.File.Delete path
        |> Either.succeed
    with
      | exn ->
        exn.Message
        |> sprintf "rmDir: %s"
        |> IOError
        |> Either.fail

  // *** mkDir

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
      if System.IO.Directory.Exists path |> not then
        System.IO.Directory.CreateDirectory path
        |> ignore
        |> Either.succeed
      else
        Either.succeed ()
    with
      | exn ->
        exn.Message
        |> sprintf "mkDir: %s"
        |> IOError
        |> Either.fail

#endif

// * Time

#if !JAVASCRIPT

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

#endif

// * Process

#if !JAVASCRIPT

///////////////////////////////////////////////////
//  ____                                         //
// |  _ \ _ __ ___   ___ ___  ___ ___  ___  ___  //
// | |_) | '__/ _ \ / __/ _ \/ __/ __|/ _ \/ __| //
// |  __/| | | (_) | (_|  __/\__ \__ \  __/\__ \ //
// |_|   |_|  \___/ \___\___||___/___/\___||___/ //
///////////////////////////////////////////////////

[<RequireQualifiedAccess>]
module Process =

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
    if isLinux
    then
      Process.Start("kill", string pid)
      |> ignore
    else
      let query = sprintf "Select * From Win32_Process Where ParentProcessID=%d" pid
      let searcher = new ManagementObjectSearcher(query);
      let moc = searcher.Get();
      for mo in moc do
        kill <| (mo.GetPropertyValue("ProcessID") :?> int)
      let proc = Process.GetProcessById(pid)
      proc.Kill();

#endif



// * WorkSpace

[<RequireQualifiedAccess>]
module WorkSpace =

  // ** find

  #if !JAVASCRIPT
  /// ## find
  ///
  /// The standard location projects are create/cloned to. Currently only settable it via
  /// the `IRIS_WORKSPACE` environment variable.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: FilePath
  let find () : FilePath =
    let wsp = Environment.GetEnvironmentVariable IRIS_WORKSPACE
    if isNull wsp || wsp.Length = 0
    then
      if isLinux then
        let usr = Security.Principal.WindowsIdentity.GetCurrent().Name
        sprintf @"/home/%s/iris" usr
      else @"C:\\Iris\"
    else wsp

  #endif

  // ** exists

  #if !JAVASCRIPT

  /// ## workSpaceExists
  ///
  /// Check if the workspace exists on disk.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: boolean
  let exists () =
    find () |> Directory.Exists

  #endif

  // ** create

  #if !JAVASCRIPT

  /// ## createWorkSpace
  ///
  /// Create the new workspace
  ///
  /// ### Signature:
  /// - arg: arg
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: unit
  let create () =
    if not (exists ()) then
      find()
      |> Directory.CreateDirectory
      |> ignore

  #endif

  // ** listProjects

  /// ## listProjects
  ///
  /// Enumerate all projects found in a workspace and return a list of them.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: Project list
  let listProjects () =
    implement "listProjects"
