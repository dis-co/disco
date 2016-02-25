namespace Iris.Service

open System.Diagnostics
open System
open System.Threading
open Nessos.FsPickler
open Iris.Core
open Iris.Core.Utils
open Iris.Core.Types
open Iris.Core.Config
open Iris.Service.Types
open Iris.Service.Core
open Iris.Service.Groups
open LibGit2Sharp

module Main =

  type CliCmd =
    | Create of name : string * path : string
    | Load   of path : FilePath
    | Save   of msg  : string
    | Close
    | Set    of variable : string * value : string
    | Info
    | Help
    | Error
    | Quit
    
  let parseLine (str : string) : CliCmd =
    let parsed = str.Split(' ')
    match parsed with
      | [| "create"; name; path |] -> Create(name, path)
      | [| "load";   path       |] -> Load(path)
      | [| "save";   msg        |] -> Save(msg)
      | [| "set";    var;  vl   |] -> Set(var, vl)
      | [| "close";             |] -> Close
      | [| "info";              |] -> Info
      | [| "help";              |] -> Help
      | [| "exit";              |] -> Quit
      | [| "quit";              |] -> Quit
      | _ -> Error

  let help _ = printfn "Iris™, 2016, NSynk GmbH"

  [<EntryPoint>]
  let main argv =

    let cli     = Array.contains "-r" argv || Array.contains "--repl"    argv
    let oracle  = Array.contains "-o" argv || Array.contains "--oracle"  argv
    let verbose = Array.contains "-v" argv || Array.contains "--verbose" argv

    if verbose
    then Environment.SetEnvironmentVariable("IRIS_VERBOSE", "True")

    if oracle
    then
      logger "Main" "Starting Oracle"
      Oracle.Start()
      Oracle.Wait()
    else
      logger "Main" "Starting Iris"
      let Iris = new IrisService()
      Iris.Start()

      if cli
      then
        let mutable run = true
        while run do
          printf "> "
          let cmd = Console.ReadLine()
          match parseLine cmd with
            | Load(path)         -> Iris.LoadProject(path)
            | Save(msg)          -> Iris.SaveProject(msg)
            | Create(name, path) -> Iris.CreateProject(name, path)
            | Close              -> Iris.CloseProject()
            | Set(var, vl)       -> Environment.SetEnvironmentVariable(var, vl)
            | Help               -> help()
            | Info               -> Iris.Dump()
            | Quit               -> run <- false
            | Error              -> printfn "command not recognized"
        (Iris :> IDisposable).Dispose()
      else
        Iris.Wait()
    0
