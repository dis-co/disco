namespace Disco.Service

open Argu
open Disco.Core
open System
open System.Threading
open Disco.Raft
open Disco.Client
open Disco.Service
open Disco.Service.Interfaces
open Disco.Service.CommandLine

[<AutoOpen>]
module Main =

  let private setup (parsed:ParseResults<CLIArguments>) =
    /// // Init machine config
    /// parsed.TryGetResult <@ Machine @>
    /// |> Option.map (filepath >> Path.getFullPath)
    /// |> MachineConfig.init getBindIp (parsed.TryGetResult <@ Shift_Defaults @>)
    /// |> Error.orExit ignore
    Either.nothing

  let private start (parsed:ParseResults<CLIArguments>) =
    let machine =
      parsed.TryGetResult <@ Machine @>
      |> Option.map filepath
      |> MachineConfig.load
    match machine with
    | Left error -> Error.exitWith error
    | Right machine ->
      let validation = MachineConfig.validate machine
      if not validation.IsEmpty then
        printfn "Machine configuration file is invalid, please check the following settings:"
        printfn ""
        for KeyValue(name, _) in validation do
          printfn "    %A must not be empty" name
        printfn ""
        exit 1

      let dir =
        parsed.TryGetResult <@ Project @>
        |> Option.map (fun projectName ->
          machine.WorkSpace </> filepath projectName)

      let frontend =
        parsed.TryGetResult <@ Frontend @>
        |> Option.map filepath

      startService machine dir frontend

  ////////////////////////////////////////
  //  __  __       _                    //
  // |  \/  | __ _(_)_ __               //
  // | |\/| |/ _` | | '_ \              //
  // | |  | | (_| | | | | |             //
  // |_|  |_|\__,_|_|_| |_|             //
  ////////////////////////////////////////

  [<EntryPoint>]
  let main args =
    let parsed =
      try parser.ParseCommandLine args
      with exn ->
        exn.Message
        |> Error.asOther "Main"
        |> Error.exitWith

    let result =
      match parsed.GetResult <@ Cmd @> with
      | Start   -> start parsed
      | Setup   -> setup parsed
      | Help  _ -> help ()

    result |> Error.orExit ignore

    Error.exitWith DiscoError.OK
