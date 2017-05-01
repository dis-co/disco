namespace Iris.Service

open Argu
open Iris.Core
open System
open System.Threading
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Interfaces
open Iris.Service.CommandLine

[<AutoOpen>]
module Main =
  ////////////////////////////////////////
  //  __  __       _                    //
  // |  \/  | __ _(_)_ __               //
  // | |\/| |/ _` | | '_ \              //
  // | |  | | (_| | | | | |             //
  // |_|  |_|\__,_|_|_| |_|             //
  ////////////////////////////////////////

  let (|Quit|_|) str =
    match str with
    | "quit" -> Some ()
    | _ -> None

  let (|Log|_|) (str: string) =
    if str.StartsWith "log " then
      str.Substring(3) |> Some
    else None

  let (|Append|_|) (str: string) =
    if str.StartsWith "append " then
      str.Substring(7) |> Some
    else None

  [<EntryPoint>]
  let main args =
    use obs = Logger.subscribe Logger.stdout
    MachineConfig.init None |> ignore

    let machine = MachineConfig.get()

    match IrisNG.load args.[0] machine with
    | Right iris ->

      let mutable run = true
      while run do
        match Console.ReadLine() with
        | Quit _  ->
          run <- false
          dispose iris
        | Log str ->
          Logger.create LogLevel.Debug "test" str
          |> IrisEvent.Log
          |> iris.Publish
        | Append str ->
          { Id = Id.Create(); Name = str; Slices = [||] }
          |> AddCue
          |> fun cmd -> (Id.Create(), cmd)
          |> SocketEvent.OnMessage
          |> IrisEvent.Socket
          |> iris.Publish
        | _ -> ()

    | Left error -> printf "error: %A" error

    0


  // [<EntryPoint>]
  // let main args =
  //   let parsed =
  //     try
  //       parser.ParseCommandLine args
  //     with
  //       | exn ->
  //         exn.Message
  //         |> Error.asOther "Main"
  //         |> Error.exitWith

  //   validateOptions parsed

  //   // Init machine config
  //   parsed.TryGetResult <@ Machine @>
  //   |> Option.map (filepath >> Path.getFullPath)
  //   |> MachineConfig.init
  //   |> Error.orExit ignore

  //   Thread.CurrentThread.GetApartmentState()
  //   |> printfn "Using Threading Model: %A"

  //   let threadCount = System.Environment.ProcessorCount * 2
  //   ThreadPool.SetMinThreads(threadCount,threadCount)
  //   |> fun result ->
  //     printfn "Setting Min. Threads in ThreadPool To %d %s"
  //       threadCount
  //       (if result then "Successful" else "Unsuccessful")

  //   let result =
  //     let machine = MachineConfig.get()

  //     let dir =
  //       parsed.TryGetResult <@ Project @>
  //       |> Option.map (fun projectName ->
  //         machine.WorkSpace </> filepath projectName)

  //     let frontend =
  //       parsed.TryGetResult <@ Frontend @>
  //       |> Option.map filepath

  //     Logger.initialize machine.MachineId

  //     match parsed.GetResult <@ Cmd @>, dir with
  //     | Create,            _ -> createProject parsed
  //     | Start,           dir -> startService dir frontend
  //     | Reset,      Some dir -> resetProject dir
  //     | Dump,       Some dir -> dumpDataDir dir
  //     | Add_User,   Some dir -> addUser dir
  //     | Add_Member, Some dir -> addMember dir
  //     | Help,              _ -> help ()
  //     |  _ ->
  //       sprintf "Unexpected command line failure: %A" args
  //       |> Error.asParseError "Main"
  //       |> Either.fail

  //   result |> Error.orExit ignore

  //   Error.exitWith IrisError.OK
