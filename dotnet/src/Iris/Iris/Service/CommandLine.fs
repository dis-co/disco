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
  | [<EqualsAssignment>] BindAddress of string
  | [<EqualsAssignment>] RaftPort    of uint32
  | [<EqualsAssignment>] WebPort     of uint32
  | [<EqualsAssignment>] WsPort      of uint32
  | [<EqualsAssignment>] ProjectDir  of string
  | [<EqualsAssignment>] ProjectName of string
  |                      Create
  |                      Start
  |                      Reset
  |                      Dump

  interface IArgParserTemplate with
    member self.Usage =
      match self with
        | ProjectDir  _ -> "Project directory to place the config & database in"
        | ProjectName _ -> "Project name when using --create"
        | BindAddress _ -> "Specify a valid IP address."
        | WebPort     _ -> "Http server port."
        | WsPort      _ -> "WebSocket port."
        | RaftPort    _ -> "Raft server port (internal)."
        | Create        -> "Create a new configuration (requires --data-dir --bind-address --web-port --raft-port)"
        | Start         -> "Start the server (requires --data-dir)"
        | Reset         -> "Join an existing cluster (requires --data-dir)"
        | Dump          -> "Dump the current state on disk (requires --data-dir)"

let parser = ArgumentParser.Create<CLIArguments>()

let validateOptions (opts: ParseResults<CLIArguments>) =
  let ensureDir b =
    if opts.Contains <@ ProjectDir @> |> not then
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
    let name = opts.Contains <@ ProjectName @>
    let dir  = opts.Contains <@ ProjectDir @>
    let bind = opts.Contains <@ BindAddress @>
    let web  = opts.Contains <@ WebPort @>
    let raft = opts.Contains <@ RaftPort @>
    let ws   = opts.Contains <@ WsPort @>

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
  match ctx.Append (AppEvent(LogMsg(Debug,str))) with
    | Some response ->
      printfn "Added Entry: %s Index: %A Term: %A"
        (string response.Id)
        response.Index
        response.Term
    | _ -> failwith "an error occurred"

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

let private withEmpty (token: string) (str: string) =
  if trim str = token
  then Some ()
  else None

let (|Exit|_|)     str = withEmpty "exit" str
let (|Quit|_|)     str = withEmpty "quit" str
let (|Status|_|)   str = withEmpty "status" str
let (|Periodic|_|) str = withEmpty "step" str
let (|Timeout|_|)  str = withEmpty "timeout" str
let (|Leave|_|)    str = withEmpty "leave" str

let (|Append|_|)  str = withTrim "append" str
let (|Join|_|)    str = withTrim "join" str
let (|AddNode|_|) str = withTrim "addnode" str
let (|RmNode|_|)  str = withTrim "rmnode" str

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

let tryLeaveCluster (context: RaftServer) =
  context.LeaveCluster()

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
      | LogLevel opt -> trySetLogLevel opt context
      | Interval   i -> trySetInterval i context
      | Exit         -> context.Stop(); kontinue := false
      | Periodic     -> context.Periodic()
      | Append ety   -> tryAppendEntry context ety
      | Join hst     -> tryJoinCluster hst context
      | Leave        -> tryLeaveCluster context
      | AddNode hst  -> tryAddNode hst context
      | RmNode hst   -> tryRmNode  hst context
      | Timeout      -> timeoutRaft context
      | Status       -> printfn "%s" <| context.ToString()
      | _            -> printfn "unknown command"
    if !kontinue then
      proc kontinue
  proc kont
