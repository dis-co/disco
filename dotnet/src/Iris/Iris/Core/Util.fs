namespace Iris.Core

open System
open System.IO
open System.Net
open System.Linq
open System.Net.NetworkInformation
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module List =
  let reverse (lst : 'a list) : 'a list =
    let reverser acc elm = List.concat [[elm]; acc]
    List.fold reverser [] lst

[<AutoOpen>]
module Utils =

#if JAVASCRIPT
  //      _                  ____            _       _
  //     | | __ ___   ____ _/ ___|  ___ _ __(_)_ __ | |_
  //  _  | |/ _` \ \ / / _` \___ \ / __| '__| | '_ \| __|
  // | |_| | (_| |\ V / (_| |___) | (__| |  | | |_) | |_
  //  \___/ \__,_| \_/ \__,_|____/ \___|_|  |_| .__/ \__|
  //                                          |_|


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
  //    _   _ _____ _____
  //   | \ | | ____|_   _|
  //   |  \| |  _|   | |
  //  _| |\  | |___  | |
  // (_)_| \_|_____| |_|

  let isLinux : bool =
    int Environment.OSVersion.Platform
    |> fun p ->
      (p = 4) || (p = 6) || (p = 128)

  let warn = printfn "[warning] %s"

  /// ## Dispose of an IDisposable object.
  ///
  /// Convenience function to call Dispose on an IDisposable.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  let dispose (o : 't when 't :> IDisposable) = o.Dispose()

  // __        __         _     ____
  // \ \      / /__  _ __| | __/ ___| _ __   __ _  ___ ___
  //  \ \ /\ / / _ \| '__| |/ /\___ \| '_ \ / _` |/ __/ _ \
  //   \ V  V / (_) | |  |   <  ___) | |_) | (_| | (_|  __/
  //    \_/\_/ \___/|_|  |_|\_\|____/| .__/ \__,_|\___\___|
  //                                 |_|
  // Path:
  //
  // the standard location projects are create/cloned to.
  // Settable it via environment variable.
  let Workspace (_ : unit) : string =
    let wsp = Environment.GetEnvironmentVariable("IRIS_WORKSPACE")
    if isNull wsp || wsp.Length = 0
    then
      if isLinux
      then
        let usr = Security.Principal.WindowsIdentity.GetCurrent().Name
        sprintf @"/home/%s/iris" usr
      else @"C:\\Iris\"
    else wsp

  /// Iris File Extension
  let IrisExt = ".iris"

  let workspaceExists () =
    Directory.Exists <| Workspace()

  let createWorkspace () =
    if not <| workspaceExists()
    then Directory.CreateDirectory <| Workspace()
         |> ignore

  let sanitizeName (name : string) =
    let regex = new Regex("(\.|\ |\*|\^)")
    if regex.IsMatch(name)
    then regex.Replace(name, "_")
    else name

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

  //  _
  // | | ___   __ _  __ _  ___ _ __
  // | |/ _ \ / _` |/ _` |/ _ \ '__|
  // | | (_) | (_| | (_| |  __/ |
  // |_|\___/ \__, |\__, |\___|_|
  //          |___/ |___/
  let logger (tag : string) (str : string) : unit =
    let def = tag.Length + 2
    let verbose = Environment.GetEnvironmentVariable("IRIS_VERBOSE")
    if not (isNull verbose) && bool.Parse(verbose)
    then
      let var = "IRIS_LOGGING_OFFSET"
      let mutable offset = 0

      try
        offset <- int (Environment.GetEnvironmentVariable(var))
      with
        | _ ->
          offset <- def
          Environment.SetEnvironmentVariable(var, string offset)

      if def > offset
      then
        offset <- def
        Environment.SetEnvironmentVariable(var, string offset)

      let ws = Array.fold (fun m _ -> m + " ") "" [| 0 .. (offset - def) |]
      printfn "[%s]%s%s" tag ws str


  //  ____  _        _
  // / ___|| |_ _ __(_)_ __   __ _
  // \___ \| __| '__| | '_ \ / _` |
  //  ___) | |_| |  | | | | | (_| |
  // |____/ \__|_|  |_|_| |_|\__, |
  //                         |___/


  let inline trim< ^a when ^a : (member Trim : unit -> ^a)> str =
    (^a : (member Trim : unit -> ^a) str)

  let inline toLower< ^a when ^a : (member ToLower : unit -> ^a)> str =
    (^a : (member ToLower : unit -> ^a) str)

  let inline toUpper< ^a when ^a : (member ToUpper : unit -> ^a)> str =
    (^a : (member ToUpper : unit -> ^a) str)

  //  ____  _       ______       _   _
  // |  _ \(_)_ __ / /  _ \ __ _| |_| |__
  // | | | | | '__/ /| |_) / _` | __| '_ \
  // | |_| | | | / / |  __/ (_| | |_| | | |
  // |____/|_|_|/_/  |_|   \__,_|\__|_| |_|

  let (</>) p1 p2 = System.IO.Path.Combine(p1, p2)

  /// ## delete a file or directory
  ///
  /// recursively delete a directory or single File.
  ///
  /// ### Signature:
  /// - path: FilePath to delete
  ///
  /// Returns: unit
  let rec delete path =
    match System.IO.DirectoryInfo(path).Attributes with
      | System.IO.FileAttributes.Directory ->
        let children = System.IO.DirectoryInfo(path).EnumerateFileSystemInfos()
        if children.Count() > 0 then
          for child in children do
            delete child.FullName
          System.IO.Directory.Delete(path)
        else
          System.IO.Directory.Delete(path)
      | _ ->
        System.IO.File.Delete path

  //  _____ _
  // |_   _(_)_ __ ___   ___
  //   | | | | '_ ` _ \ / _ \
  //   | | | | | | | | |  __/
  //   |_| |_|_| |_| |_|\___|

  let createTimestamp () =
    let now = DateTime.Now
    now.ToString("u")

#endif
