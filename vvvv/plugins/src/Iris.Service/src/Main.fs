namespace Iris.Service

open System.Diagnostics
open System
open System.Threading
open Nessos.FsPickler
open Iris.Core.Types
open Iris.Core.Config
open Iris.Service.Types
open Iris.Service.Core
open LibGit2Sharp
open Vsync

module Main =

  let (|Create|Load|Save|Start|Stop|Help|) (str : string) =
    let parsed = str.Split(' ')
    match parsed with
      | [| "create"; name; path |] -> Create(name, path)
      | [| "load";         path |] -> Load(path)
      | [| "save";         msg  |] -> Save(msg)
      | [| "start";             |] -> Start
      | [| "stop";              |] -> Stop
      | _ -> Help

  [<EntryPoint>]
  let main argv =
    printf "starting.."

    let options =
      { VsyncConfig.Default with
          UnicastOnly = Some(true);
          Hosts = Some([ "localhost" ]) }

    options.Apply()
    
    // VsyncSystem.Start()

    let mutable daemon : Git.Daemon option = None
    let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))
    let Context = new Context()
    Context.Signature <- Some(signature)

    printfn "..ready."

    while true do
      printf "> "
      let cmd = Console.ReadLine()
      match cmd with
        | Load(path)         -> Context.LoadProject(path)
        | Save(msg)          -> Context.SaveProject(msg)
        | Create(name, path) -> Context.CreateProject(name, path)
        | Start ->
          if Option.isSome Context.Project
          then
            match daemon with
              | Some(d) -> if d.Running()
                           then printfn "already running"
                           else d.Start()
              | None ->
                match Context.Project with
                  | Some(p) ->
                    match p.Path with
                      | Some path -> 
                         let d = new Git.Daemon(path)
                         d.Start()
                         daemon <- Some(d)
                      | _ -> printfn "project has no path"
                  | None -> printfn "no project yet"
        | Stop -> match daemon with
                    | Some(d) -> d.Stop()
                    | None -> printfn "no daemon running"
        | Help -> printfn "help requested.."

    // VsyncSystem.WaitForever()

    0
