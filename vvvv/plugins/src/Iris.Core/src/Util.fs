namespace Iris.Core

open System
open System.IO
open System.Net
open System.Net.NetworkInformation
open System.Text.RegularExpressions

module Utils =

  let isLinux : bool =
    int Environment.OSVersion.Platform
    |> fun p ->
      (p = 4) || (p = 6) || (p = 128)

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

