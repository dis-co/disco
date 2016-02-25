namespace Iris.Service

open System.Diagnostics
open System
open System.Threading
open Nessos.FsPickler
open Iris.Core
open Iris.Core.Types
open Iris.Core.Config
open Iris.Service.Types
open Iris.Service.Core
open Iris.Service.Groups
open LibGit2Sharp

module Main =

  let (|Create|Load|Save|Close|Set|Quit|Help|) (str : string) =
    let parsed = str.Split(' ')
    match parsed with
      | [| "create"; name; path |] -> Create(name, path)
      | [| "load";         path |] -> Load(path)
      | [| "save";         msg  |] -> Save(msg)
      | [| "close";             |] -> Close
      | [| "set";    path       |] -> Set(path)
      | [| "exit";              |] -> Quit
      | [| "quit";              |] -> Quit
      | _ -> Help

  let help _ = printfn "Iris™, 2016, NSynk GmbH"

  [<EntryPoint>]
  let main argv =
    printf "starting.."

    let mutable run = true

    let Iris = new IrisService()
    Iris.Start()

    while run do
      printf "> "
      let cmd = Console.ReadLine()
      match cmd with
        | Load(path)         -> Iris.LoadProject(path)
        | Save(msg)          -> Iris.SaveProject(msg)
        | Create(name, path) -> Iris.CreateProject(name, path)
        | Close              -> Iris.CloseProject()
        | Set(path)          -> Environment.SetEnvironmentVariable("IRIS_WORKSPACE", path)
        | Help               -> help()
        | Quit               -> run <- false

    printfn "Disposing now.."

    (Iris :> IDisposable).Dispose()

    0
