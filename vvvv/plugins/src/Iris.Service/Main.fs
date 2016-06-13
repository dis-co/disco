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
open LibGit2Sharp

module Main =

  type CliCmd =
    | Create of name : string * path : string
    | Load   of path : FilePath
    | Save   of id'  : string * msg  : string
    | Close  of id'  : string
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
      | [| "save";   id;   msg  |] -> Save(id, msg)
      | [| "set";    var;  vl   |] -> Set(var, vl)
      | [| "close";  id;        |] -> Close(id)
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
            | Load(path) ->
              // match Iris.LoadProject(path) with
              //   | Success p -> printfn "Loaded %s: %s" p.Name <| p.Id.ToString()
              //   | Fail err -> printfn "Error loading: %s" err
              printfn "fix loading"

            | Save(pid,msg) ->
              // match Iris.SaveProject(Guid.Parse(pid),msg) with
              //   | Success c -> printfn "Saved! Commit Sha: %s" c.Sha
              //   | Fail err -> printfn "Error saving: %s" err
              printfn "fix saving"

            | Create(name, path) ->
              // match  Iris.CreateProject(name, path) with
              //   | Success p -> printfn "Created Project: %s" <| p.Id.ToString()
              //   | Fail err -> printfn "Could not create project: %s" err
              printfn "fix creating"

            | Close(pid) ->
              // match Iris.CloseProject(Guid.Parse(pid)) with
              //   | Success p -> printfn "Closed project %s." p.Name
              //   | Fail err -> printfn "Could not close project: %s" err
              printfn "fix closing"

            | Set(var, vl) -> Environment.SetEnvironmentVariable(var, vl)

            | Help  -> help()
            | Info  -> Iris.Dump()
            | Quit  -> run <- false
            | Error -> printfn "command not recognized"
        (Iris :> IDisposable).Dispose()
      else
        Iris.Wait()
    0
