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
  | [<EqualsAssignment>] Project_Dir  of string
  | [<EqualsAssignment>] Project_Name of string
  |                      Create
  |                      Start
  |                      Reset
  |                      Dump

  interface IArgParserTemplate with
    member self.Usage =
      match self with
        | Project_Dir  _ -> "Project directory to place the config & database in"
        | Project_Name _ -> "Project name when using --create"
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
  let ensureDir b =
    if opts.Contains <@ Project_Dir @> |> not then
      printfn "Error: you must specify a project dir when starting a node"
      exit 3
    b

  let flags =
    ( opts.Contains <@ Create @>
    , opts.Contains <@ Start  @>
    , opts.Contains <@ Reset  @>
    , opts.Contains <@ Dump   @> )

  let valid =
    match flags with
    | (true,false,false,false) -> true
    | (false,true,false,false) -> ensureDir true
    | (false,false,true,false) -> ensureDir true
    | (false,false,false,true) -> ensureDir true
    | _                        -> false

  if not valid then
    printfn "Error: you must specify either *one of* --start/--create/--reset/--dump"
    exit 1

  if opts.Contains <@ Create @> then
    let name = opts.Contains <@ Project_Name @>
    let dir  = opts.Contains <@ Project_Dir @>
    let bind = opts.Contains <@ Bind_Address @>
    let web  = opts.Contains <@ Web_Port @>
    let raft = opts.Contains <@ Raft_Port @>
    let ws   = opts.Contains <@ Ws_Port @>

    if not (name && bind && web && raft && ws) then
      printfn "Error: when creating a new configuration you must specify the following options:"
      printfn "    --project-name: name for the new project"
      printfn "    --project-dir: base directory to store new project in"
      printfn "    --bind-address: ip address to bind raft server to"
      printfn "    --raft-port: port to bind raft server to"
      printfn "    --web-port: port to bind http server to"
      printfn "    --ws-port: port to bind websocket server to"
      exit 1

let parseLogLevel = function
  | "debug" -> Debug
  | "info"  -> Info
  | "warn"  -> Warn
  | _       -> Err

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

let private withTrim (token: string) (str: string) =
  let trimmed = trim str
  if trimmed.StartsWith(token) then
    let substr = trimmed.Substring(token.Length)
    Some <| trim substr
  else None

let (|Exit|_|) str =
  if str = "exit" || str = "quit" then
    Some ()
  else None

let (|Status|_|) (str: string) =
  if trim str = "status" then
    Some ()
  else None

let (|Append|_|)  (str: string) = withTrim "append" str
let (|Join|_|)    (str: string) = withTrim "join" str
let (|AddNode|_|) (str: string) = withTrim "addnode" str
let (|RmNode|_|)  (str: string) = withTrim "rmnode" str

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

let trySetLogLevel (str: string) (context: RaftServer) =
  let config =
    { context.Options.RaftConfig with
        LogLevel = parseLogLevel str }
  context.Options <- updateEngine config context.Options

let trySetInterval i (context: RaftServer) =
  let config = { context.Options.RaftConfig with PeriodicInterval = i }
  context.Options <- updateEngine config context.Options

let tryJoinCluster (hst: string) (context: RaftServer) =
  let parsed =
    match split [| ' ' |] hst with
      | [| ip; port |] -> Some (ip, int port)
      | _            -> None

  match parsed with
    | Some(ip, port) -> context.JoinCluster(ip, port)
    | _ -> printfn "parameters %A could not be parsed" hst

let tryAddNode (hst: string) (context: RaftServer) =
  let parsed =
    match split [| ' ' |] hst with
      | [| id; ip; port |] -> Some (id, ip, int port)
      | _                -> None

  match parsed with
    | Some(id, ip, port) ->
      match context.AddNode(id, ip, port) with
        | Some appended ->
          printfn "Added node: %A in entry %A" id (string appended.Id)
        | _ ->
          printfn "Could not add node %A" id
    | _ ->
      printfn "parameters %A could not be parsed" hst

let tryRmNode (hst: string) (context: RaftServer) =
    match context.RmNode(trim hst) with
      | Some appended ->
        printfn "Removed node: %A in entry %A" hst (string appended.Id)
      | _ ->
        printfn "Could not removed node %A " hst

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
      | LogLevel opt ->
        printfn "loglevel"
        trySetLogLevel opt context
      | Interval   i ->
        printfn "interfvall"
        trySetInterval i context
      | Exit         ->
        printfn "ext[]"
        context.Stop(); kontinue := false
      | Append ety   ->
        printfn "append w"
        match tryAppendEntry context ety with
          | Some response ->
            printfn "Added Entry: %s Index: %A Term: %A"
              (string response.Id)
              response.Index
              response.Term
          | _ -> failwith "an error occurred"
      | Join hst    ->
        printfn "join w"
        tryJoinCluster hst context
      | AddNode hst ->
        printfn "addnode w"
        tryAddNode hst context
      | RmNode hst  ->
        printfn "rmnode"
        tryRmNode  hst context
      | Timeout     ->
        printfn "timeout"
        timeoutRaft context
      | Status      ->
        printfn "%s" <| context.ToString()
      | _           -> printfn "unknown command"
    if !kontinue then
      proc kontinue
  proc kont
