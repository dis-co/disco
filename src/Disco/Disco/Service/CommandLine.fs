(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * CommandLine

#if !DISCO_NODES

module CommandLine =

  let private logtag (str: string) = sprintf "CommandLine.%s" str

  // ** Imports

  open Argu
  open Disco.Core
  open Disco.Raft
  open Disco.Service.Persistence
  open Disco.Service.Disco
  open Disco.Service.CommandActions
  open Disco.Service.Interfaces
  open System
  open System.Linq
  open System.Threading
  open System.Collections.Generic
  open System.Text.RegularExpressions

  // ** Command Line Argument Parser

  //     _
  //    / \   _ __ __ _ ___
  //   / _ \ | '__/ _` / __|
  //  / ___ \| | | (_| \__ \
  // /_/   \_\_|  \__, |___/
  //              |___/

  type SubCommand =
    | Help
    | Start
    | Setup

    static member Doc
      with get () =
        @"
 ____                                        _        _   _
|  _ \  ___   ___ _   _ _ __ ___   ___ _ __ | |_ __ _| |_(_) ___  _ __
| | | |/ _ \ / __| | | | '_ ` _ \ / _ \ '_ \| __/ _` | __| |/ _ \| '_ \
| |_| | (_) | (__| |_| | | | | | |  __/ | | | || (_| | |_| | (_) | | | |
|____/ \___/ \___|\__,_|_| |_| |_|\___|_| |_|\__\__,_|\__|_|\___/|_| |_|

----------------------------------------------------------------------
| setup                                                              |
----------------------------------------------------------------------

  Create a new machine configuration file. This sets the current
  machine's global identifier and also specifies the workspace
  directory used by Disco to scan for projects.

  You can specify the parent directory to create or update
  configuration in by using the --machine parameter:

  --machine=/path/to/directory

----------------------------------------------------------------------
| start                                                              |
----------------------------------------------------------------------

  Start the Disco daemon with the project specified. This flag is
  optional. When started without, the service will be in idle mode.

  You can specify the project to start with using:

  --project=project-name : Name of project directory in the workspace

  You can also optionally specify the directory in which machinecfg.yaml
  is located:

  --machine=/path/to/directory

----------------------------------------------------------------------
| help                                                               |
----------------------------------------------------------------------

  Show this help message.
"

  type CLIArguments =
    | [<Mandatory;MainCommand;CliPosition(CliPosition.First)>] Cmd of SubCommand
    | [<EqualsAssignment>]     Project  of string
    | [<EqualsAssignment>]     Machine  of string
    | [<EqualsAssignment>]     Frontend of string
    | [<AltCommandLine("-y")>] Yes

    interface IArgParserTemplate with
      member self.Usage =
        match self with
          | Project _   -> "Name of project directory in the workspace"
          | Machine _   -> "Path to the machine config file"
          | Frontend _  -> "Path to the frontend files"
          | Yes         -> "Don't prompt and choose defaults during setup command"
          | Cmd     _   -> "Main Command: either one of setup, start, or help."

  let parser = ArgumentParser.Create<CLIArguments>()

  // ** consoleLoop

  //  _
  // | |    ___   ___  _ __
  // | |   / _ \ / _ \| '_ \
  // | |__| (_) | (_) | |_) |
  // |_____\___/ \___/| .__/ s
  //                  |_|
  let registerExitHandlers (context: IDisco) =
    Console.CancelKeyPress.Add (fun _ ->
      printfn "Disposing context..."
      dispose context
      exit 0)
    System.AppDomain.CurrentDomain.ProcessExit.Add (fun _ -> dispose context)
    System.AppDomain.CurrentDomain.DomainUnload.Add (fun _ -> dispose context)

  // ** vmSetup

  let vmSetup () =
    Thread.CurrentThread.GetApartmentState()
    |> printfn "Using Threading Model: %A"

    let threadCount = System.Environment.ProcessorCount * 2
    ThreadPool.SetMinThreads(threadCount,threadCount)
    |> fun result ->
      printfn "Setting Min. Threads in ThreadPool To %d %s"
        threadCount
        (if result then "Successful" else "Unsuccessful")

  // ** startService

  //  ____  _             _
  // / ___|| |_ __ _ _ __| |_
  // \___ \| __/ _` | '__| __|
  //  ___) | || (_| | |  | |_
  // |____/ \__\__,_|_|   \__|

  let startService
    (machine: DiscoMachine)
    (projectDir: FilePath option)
    (frontend: FilePath option) =
    result {
      let agentRef = ref None
      let post = CommandActions.postCommand agentRef
      let termSupportsColors = Console.isColorTerm()

      do Logger.initialize {
        MachineId = machine.MachineId
        Tier = Tier.Service
        UseColors = termSupportsColors
        Level = LogLevel.Debug
      }
   
      do! Metrics.init machine
    
      use _ = Logger.subscribe Logger.stdout

      let! discoService = Disco.create post {
        Machine = machine
        FrontendPath = frontend
        ProjectPath = projectDir
      }

      agentRef := CommandActions.startAgent machine discoService |> Some

      registerExitHandlers discoService

      do!
        match projectDir with
        | Some projectDir ->
          let name, site =
            name (unwrap projectDir),
            None
          Commands.Command.LoadProject(name, site)
          |> CommandActions.postCommand agentRef
          |> Async.RunSynchronously
          |> Result.map ignore
        | None ->
          Result.succeed ()

      do vmSetup ()

      let result =
        let kont = ref true
        let rec proc kontinue =
          Console.ReadLine() |> ignore
          if !kontinue then
            proc kontinue
        proc kont

      dispose discoService

      return result
    }

  // ** help

  [<Literal>]
  let private header = @"   *   .  *.  .
 * . ____* _ ____ .*____      *
 .  |  _ \(_) ___| / ___|___ .   * .
. * | | | | \___ \| |   / _ \     .
   .| |_| | |___) | |__| (_) |*  *
.*  |____/|_|____/ \____\___/     .
   . Distributed Show Control Daemon © Nsynk GmbH, 2016
*    .*       .
 "

  let help () =
    parser.PrintUsage(header, "disco.exe", true)
    |> flip (printfn "%s\n%s") SubCommand.Doc
    |> Result.succeed

#endif
