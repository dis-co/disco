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
open System.Diagnostics
open System.Text
open System.Security.Cryptography
open System.Runtime.CompilerServices

#endif

#if !IRIS_NODES && !FABLE_COMPILER

open System.Management

#endif

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

  // ** endsWith

  let endsWith (suffix: string) (str: string) =
    str.EndsWith suffix

  // ** replace

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

  // ** join

  /// ## join
  ///
  /// Join a string using provided separator.
  ///
  /// ### Signature:
  /// - sep: string separator
  /// - arr: string array to join
  ///
  /// Returns: string
  let join (sep: string) (arr: string array) = String.Join(sep, arr)

  // ** toLower

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

  // ** trim

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

  // ** toUpper

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

  // ** split

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

  // ** indent

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

  // ** subString

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
  // ** santitize

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
    let regex = Regex("(\\\|\/|\.|\ |\*|\^)")
    if regex.IsMatch(payload)
    then regex.Replace(payload, "_")
    else payload

  /// *** encodeBase64

  let encodeBase64 (bytes: byte[]) =
    #if FABLE_COMPILER
    let mutable str = ""
    let arr = bytes
    for i in 0 .. (int arr.Length - 1) do
      str <- str + Fable.Import.JS.String.fromCharCode(float arr.[i])
    Fable.Import.Browser.window.btoa str
    #else
    Convert.ToBase64String(bytes)
    #endif

  /// *** decodeBase64

  let decodeBase64 (buffer: string) : byte[] =
    #if FABLE_COMPILER
    let binary = Fable.Import.Browser.window.atob buffer
    let bytes = Array.zeroCreate<byte> binary.Length
    for i in 0 .. (binary.Length - 1) do
      bytes.[i] <- charCodeAt binary i
    bytes
    #else
    Convert.FromBase64String(buffer)
    #endif

  // ** format

  let format (format: string) (o: obj) =
    String.Format(format, o)

// * NameUtils

module NameUtils =

  let (+) (name1: Name) (name2: Name) = name (unwrap name1 + unwrap name2)

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

#if !FABLE_COMPILER && !IRIS_NODES

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

// * Crypto

#if !FABLE_COMPILER

[<RequireQualifiedAccess>]
module Crypto =

  /// Turn a byte array into a string.
  let private toString (buf: byte array) =
    let hashedString = StringBuilder()
    for byte in buf do
      hashedString.AppendFormat("{0:x2}", byte)
      |> ignore
    hashedString.ToString()

  /// Compute the SHA1 checksum of the passed byte array.
  let sha1sum (buf: byte array) : Hash =
    use sha1 = new SHA1Managed()
    sha1.ComputeHash(buf)
    |> toString
    |> checksum

  /// Compute the SHA256 checksum of the passed byte array.
  let sha256sum (buf: byte array) : Hash =
    use sha256 = new SHA256Managed()
    sha256.ComputeHash(buf)
    |> toString
    |> checksum

  /// Generate a random salt value for securing passwords.
  let generateSalt (n: int) : Salt =
    let buf : byte array = Array.zeroCreate n
    let random = Random()
    random.NextBytes(buf)
    sha1sum buf

  /// Generate a salted and hashed checksum for the given password.
  let hashPassword (pw: Password) (salt: Salt) : Hash =
    let concat:string = unwrap salt + unwrap pw
    concat
    |> Encoding.UTF8.GetBytes
    |> sha256sum

  /// Hashes the given password with a generated random salt value. Returns a tuple of the generated
  /// hash and the salt used in the process.
  let hash (pw: Password) : Hash * Salt =
    let salt = generateSalt 50
    hashPassword pw salt, salt

#endif

// * Functional

[<AutoOpen>]
module Functional =

  // ** konst

  let konst (a: 'a) = fun _ -> a

  // ** flip

  let flip (f: 'a -> 'b -> 'c) (b: 'b) (a: 'a) = f a b

  // ** flip2

  let flip2 (f: 'a -> 'b -> 'c -> 'd) (b: 'b) (c: 'c) (a: 'a) = f a b c

  // ** cons

  let cons (xs: 'a list) (x: 'a) = x::xs

// * Tuple

module Tuple =

  let inline mapFst (f: 'a -> 'c) (a, b) =
    (f a, b)

  let inline mapSnd (f: 'b -> 'c) (a, b) =
    (a, f b)

  let inline mapFst3 (f: 'a -> 'd) (a, b, c) =
    (f a, b, c)

  let inline mapSnd3 (f: 'b -> 'd) (a, b, c) =
    (a, f b, c)

  let inline mapThrd3 (f: 'c -> 'd) (a, b, c) =
    (a, b, f c)

// * Option

module Option =

  let inline ofNull (f: ^a -> ^b option) (value: ^a) =
    match value with
    | null -> None
    | _ -> f value

  let inline mapNull< ^a, ^b when ^a : null > (f: ^a -> ^b) (value: ^a) =
    match value with
    | null -> None
    | _ -> Some (f value)

// * Console

#if !FABLE_COMPILER && !IRIS_NODES

module Console =

  // ** addExitHandlers

  let addExitHandlers (disposables: 't list when 't :> IDisposable) =
    let inline disposer _ = List.iter dispose disposables
    Console.CancelKeyPress.Add (fun _ -> disposer(); exit 0)
    AppDomain.CurrentDomain.ProcessExit.Add  disposer
    AppDomain.CurrentDomain.DomainUnload.Add disposer

  // ** withForeground

  let private withForeground pat fg (o: obj) =
    let prevFg = Console.ForegroundColor
    Console.ForegroundColor <- fg
    Console.Write(pat,o)
    Console.ForegroundColor <- prevFg

  // ** colors

  // Black         - The color black.
  // Blue          - The color blue.
  // Cyan          - The color cyan (blue-green).
  // DarkBlue      - The color dark blue.
  // DarkCyan      - The color dark cyan (dark blue-green).
  // DarkGray      - The color dark gray.
  // DarkGreen     - The color dark green.
  // DarkMagenta   - The color dark magenta (dark purplish-red).
  // DarkRed       - The color dark red.
  // DarkYellow    - The color dark yellow (ochre).
  // Gray          - The color gray.
  // Green         - The color green.
  // Magenta       - The color magenta (purplish-red).
  // Red           - The color red.
  // White         - The color white.
  // Yellow        - The color yellow.

  let black pat (thing: obj) =
    withForeground pat ConsoleColor.Black thing

  let white pat (thing: obj) =
    withForeground pat ConsoleColor.White thing

  let blue pat (thing: obj) =
    withForeground pat ConsoleColor.Blue thing

  let darkBlue pat (thing: obj) =
    withForeground pat ConsoleColor.DarkBlue thing

  let cyan pat (thing: obj) =
    withForeground pat ConsoleColor.Cyan thing

  let darkCyan pat (thing: obj) =
    withForeground pat ConsoleColor.DarkCyan thing

  let gray pat (thing: obj) =
    withForeground pat ConsoleColor.Gray thing

  let darkGray pat (thing: obj) =
    withForeground pat ConsoleColor.DarkGray thing

  let green pat (thing: obj) =
    withForeground pat ConsoleColor.Green thing

  let darkGreen pat (thing: obj) =
    withForeground pat ConsoleColor.DarkGreen thing

  let magenta pat (thing: obj) =
    withForeground pat ConsoleColor.Magenta thing

  let darkMagenta pat (thing: obj) =
    withForeground pat ConsoleColor.DarkMagenta thing

  let red pat (thing: obj) =
    withForeground pat ConsoleColor.Red thing

  let darkRed pat (thing: obj) =
    withForeground pat ConsoleColor.DarkRed thing

  let yellow pat (thing: obj) =
    withForeground pat ConsoleColor.Yellow thing

  let darkYellow pat (thing: obj) =
    withForeground pat ConsoleColor.DarkYellow thing

  // ** isColorTerm

  let isColorTerm () =
    match Environment.GetEnvironmentVariable "TERM" with
    | "st"    | "st-256color"
    | "xterm" | "xterm-256color" -> true
    | _ -> false


#endif

// * Guid

module Guid =

  // ** ofId

  let ofId (id: IrisId) =
    id.Guid

  // ** toId

  let toId (guid: Guid) =
    IrisId.FromGuid guid
