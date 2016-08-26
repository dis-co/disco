module Iris.Service.CommandLine

open Argu
open System
open Iris.Core
open Iris.Raft
open Iris.Service.Raft.Server


////////////////////////////////////////
//     _                              //
//    / \   _ __ __ _ ___             //
//   / _ \ | '__/ _` / __|            //
//  / ___ \| | | (_| \__ \            //
// /_/   \_\_|  \__, |___/            //
//              |___/                 //
////////////////////////////////////////

type CLIArguments =
  | [<EqualsAssignment>] Bind_Address of string
  | [<EqualsAssignment>] Raft_Port    of uint32
  | [<EqualsAssignment>] Web_Port     of uint32
  | [<EqualsAssignment>] Ws_Port      of uint32
  | [<EqualsAssignment>] Data_Dir     of string
  |                      Create
  |                      Start
  |                      Reset
  |                      Dump

  interface IArgParserTemplate with
    member self.Usage =
      match self with
        | Data_Dir     _ -> "Temporary directory to place the database in"
        | Bind_Address _ -> "Specify a valid IP address."
        | Web_Port     _ -> "Http server port."
        | Ws_Port      _ -> "WebSocket port."
        | Raft_Port    _ -> "Raft server port (internal)."
        | Create         -> "Create a new configuration (requires --data-dir --bind-address --web-port --raft-port)"
        | Start          -> "Start the server (requires --data-dir)"
        | Reset          -> "Join an existing cluster (requires --data-dir)"
        | Dump           -> "Dump the current state on disk (requires --data-dir)"

let parser = ArgumentParser.Create<CLIArguments>()

let validateOptions (opts: ParseResults<CLIArguments>) =
  let missing = printfn "Error: you must specify %s when joining a cluster"

  // if we are joining a cluster these options must be passed
  if not <| opts.Contains <@ Data_Dir @> then
    missing "--data-dir"
    exit 1

  let flags =
    ( opts.Contains <@ Create @>
    , opts.Contains <@ Start  @>
    , opts.Contains <@ Reset  @>
    , opts.Contains <@ Dump   @> )

  let valid =
    match flags with
    | (true,false,false,false) ->
      let bind = opts.Contains <@ Bind_Address @>
      let web  = opts.Contains <@ Web_Port @>
      let raft = opts.Contains <@ Raft_Port @>
      let ws   = opts.Contains <@ Ws_Port @>
      bind && web && raft && ws
    | (false,true,false,false) -> true
    | (false,false,true,false) -> true
    | (false,false,false,true) -> true
    | _                        -> false

  if not valid then
    printfn "Error: you must specify either *one of* --start/--create/--reset/--dump"
    exit 1

let parseLogLevel = function
  | "debug" -> Debug
  | "info"  -> Info
  | "warn"  -> Warn
  | _       -> Err

let parseOptions args =
  (* Get all mandatory options sorted out and initialize context *)
  try
    let opts = parser.Parse args
    // validateOptions opts

    failwith "implement option parsing"
  with
    | ex ->
      printfn "Error: %s" ex.Message
      exit 1

////////////////////////////////////////
//  ____  _        _                  //
// / ___|| |_ __ _| |_ ___            //
// \___ \| __/ _` | __/ _ \           //
//  ___) | || (_| | ||  __/           //
// |____/ \__\__,_|\__\___| manipulation....
////////////////////////////////////////

let parseHostString (str: string) =
  let trimmed = str.Trim().Split(' ')
  match trimmed with
    | [| id; hostname; hostspec |] as arr ->
      if hostspec.StartsWith("tcp://") then
        match hostspec.Substring(6).Split(':') with
          | [| addr; port |] -> Some (uint32 id, hostname, addr, int port)
          | _ -> None
      else None
    | _ -> None


let tryAppendEntry (ctx: RaftServer) str =
  ctx.Append (AddClient str)

let timeoutRaft (ctx: RaftServer) =
  ctx.ForceTimeout()

/////////////////////////////////////////
//   ____                      _       //
//  / ___|___  _ __  ___  ___ | | ___  //
// | |   / _ \| '_ \/ __|/ _ \| |/ _ \ //
// | |__| (_) | | | \__ \ (_) | |  __/ //
//  \____\___/|_| |_|___/\___/|_|\___| //
/////////////////////////////////////////

let (|Exit|_|) str =
  if str = "exit" || str = "quit" then
    Some ()
  else None

let (|Add|_|) (str: string) =
  if str.StartsWith("add") then
    Some <| str.Substring(4).Trim()
  else None

let (|Remove|_|) (str: string) =
  if str.StartsWith("rm") then
    Some <| str.Substring(3).Trim()
  else None

let (|Nodes|_|) (str: string) =
  if str.Trim() = "nodes" then
    Some ()
  else None

let (|Status|_|) (str: string) =
  if str.Trim() = "status" then
    Some ()
  else None

let (|Append|_|) (str: string) =
  let trimmed = str.Trim()
  if trimmed.StartsWith("append") then
    Some <| trimmed.Substring(6).Trim()
  else None

let (|Interval|_|) (str: string) =
  let trimmed = str.Trim()
  match trimmed.Split(' ') with
  | [| "interval"; x |] ->
    try
      uint8 x |> Some
    with
      | _ -> None
  | _ -> None

let (|LogLevel|_|) (str: string) =
  let parsed = str.Trim().Split(' ')
  match parsed with
    | [| "log"; "debug" |] -> Some "debug"
    | [| "log"; "info" |]  -> Some "info"
    | [| "log"; "warn" |]  -> Some "warn"
    | [| "log"; "err" |]   -> Some "err"
    | _                  -> None

let (|Periodic|_|) (str: string) =
  let trimmed = str.Trim()
  if trimmed = "step" then
    Some ()
  else None

let (|Timeout|_|) (str: string) =
  let trimmed = str.Trim()
  if trimmed = "timeout" then
    Some ()
  else None

////////////////////////////////////////
//  _                                 //
// | |    ___   ___  _ __             //
// | |   / _ \ / _ \| '_ \            //
// | |__| (_) | (_) | |_) |           //
// |_____\___/ \___/| .__/            //
//                  |_|               //
////////////////////////////////////////

let consoleLoop (context: RaftServer) =
  let kont = ref true
  let rec proc kontinue =
    printf "~> "
    let input = Console.ReadLine()
    match input with
      | LogLevel opt   ->
        let config = { context.Options.RaftConfig with LogLevel = parseLogLevel opt }
        context.Options <- updateEngine context.Options config
      | Interval  i ->
        let config = { context.Options.RaftConfig with PeriodicInterval = i }
        context.Options <- updateEngine context.Options config
      | Exit        -> context.Stop(); kontinue := false
      | Periodic    -> context.Periodic()
      | Nodes       -> Map.iter (fun _ a -> printfn "Node: %A" a) context.State.Peers
      | Append ety  ->
        match tryAppendEntry context ety with
          | Some response ->
            printfn "Added Entry: %s Index: %A Term: %A"
              (string response.Id)
              response.Index
              response.Term
          | _ -> failwith "an error occurred"
      | Timeout     -> timeoutRaft context
      | Status      -> printfn "%s" <| context.ToString()
      | _           -> printfn "unknown command"
    if !kontinue then
      proc kontinue
  proc kont
