module Iris.Service.CommandLine

open Argu
open System
open Iris.Core
open Pallet.Core
open Iris.Service.Raft.Server


////////////////////////////////////////
//     _                              //
//    / \   _ __ __ _ ___             //
//   / _ \ | '__/ _` / __|            //
//  / ___ \| | | (_| \__ \            //
// /_/   \_\_|  \__, |___/            //
//              |___/                 //
////////////////////////////////////////

let parser = ArgumentParser.Create<GeneralArgs>()

let validateOptions (opts: ParseResults<GeneralArgs>) =
  let missing = printfn "Error: you must specify %s when joining a cluster"

  if not (opts.Contains <@ Start @>) && not (opts.Contains <@ Join  @>) then
    printfn "Error: you must specify one of --start/--join"
    exit 1

  if opts.Contains <@ Join @> then
    if not <| opts.Contains <@ LeaderIp @> then
      missing "--leader-ip"
      exit 1
    elif not <| opts.Contains <@ LeaderPort @> then
      missing "--leader-port"
      exit 1

let parseOptions args =
  (* Get all mandatory options sorted out and initialize context *)
  try
    let opts = parser.Parse args
    validateOptions opts
    { RaftId           = opts.GetResult    <@ RaftNodeId @> |> trim
    ; IpAddr           = opts.GetResult    <@ Bind       @> |> trim
    ; WebPort          = opts.GetResult    <@ WebPort    @> |> int
    ; RaftPort         = opts.GetResult    <@ RaftPort   @> |> int
    ; Debug            = opts.Contains     <@ Debug      @>
    ; Start            = opts.Contains     <@ Start      @>
    ; LeaderIp         = opts.TryGetResult <@ LeaderIp   @>
    ; LeaderPort       = opts.TryGetResult <@ LeaderPort @>
    ; DataDir          = opts.GetResult    <@ DataDir    @>
    ; PeriodicInterval = 10UL
    ; MaxRetries       = 5u }
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
      UInt64.Parse x |> Some
    with
      | _ -> None
  | _ -> None

let (|Debug|_|) (str: string) =
  let parsed = str.Trim().Split(' ')
  match parsed with
    | [| "debug"; "on" |]    -> Some true
    | [| "debug"; "off" |]   -> Some false
    | [| "debug"; "true" |]  -> Some true
    | [| "debug"; "false" |] -> Some false
    | _ -> None

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
      | Debug opt   -> context.Options <- { context.Options with Debug = opt }
      | Interval  i -> context.Options <- { context.Options with PeriodicInterval = i }
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
