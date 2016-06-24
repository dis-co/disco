namespace Iris.Service

open System
open System.Security.Cryptography
open System.Net
open System.Threading
open System.Diagnostics

open Iris.Core
open Iris.Core.Utils
open Iris.Service.Types
open Iris.Service.Core
open LibGit2Sharp

open Argu
open Pallet.Core

[<AutoOpen>]
module Main =

  [<EntryPoint>]
  let main argv =

    0

  ///////////////////////////////////////////
  //  _                     _ _            //
  // | |__   __ _ _ __   __| | | ___ _ __  //
  // | '_ \ / _` | '_ \ / _` | |/ _ \ '__| //
  // | | | | (_| | | | | (_| | |  __/ |    //
  // |_| |_|\__,_|_| |_|\__,_|_|\___|_|    //
  ///////////////////////////////////////////

  let handleMessage (ctx: AppContext) msg : RaftMsg =
    match msg with
      | RequestVote (sender,req) -> ctx.ReceiveVoteRequest sender req
      | RequestVoteResponse (sender,rep) -> ctx.ReceiveVoteResponse sender rep
      | AppendEntries (sender,ae) -> ctx.ReceiveAppendEntries sender ae
      | AppendEntriesResponse (sender,ar) -> ctx.ReceiveAppendResponse sender ar
      | HandShake node ->
        if isLeader ctx.State then
          ctx.AddNode node
          //MembershipResponse(Hello, None)
          EmptyResponse
        else
          match currentLeader ctx.State with
            | Some nid -> 
              let node = getNode nid ctx.State
              // MembershipResponse(Redirect, node)
              EmptyResponse
            | _ -> 
              // MembershipResponse(Redirect, None)
              EmptyResponse

      // ------------------------------------------------------------
      | HandWaive node ->
        if isLeader ctx.State then
          ctx.RemoveNode node
          //MembershipResponse(ByeBye, None)
          EmptyResponse
        else
          match currentLeader ctx.State with
            | Some nid -> 
              let node = getNode nid ctx.State
              // MembershipResponse(Redirect, node)
              EmptyResponse
            | _ -> 
              // MembershipResponse(Redirect, None)
              EmptyResponse

      // ------------------------------------------------------------
      | InstallSnapshot (sender, snapshot) -> //
        ctx.Log (sprintf "[InstallSnapshot RPC] installing")
        ctx.InstallSnapshot sender snapshot

      | InstallSnapshotResponse (sender, snapshot) ->
        ctx.Log (sprintf "[InstallSnapshot RPC] done")
        EmptyResponse

      // // ------------------------------------------------------------
      // | MembershipResponse (Redirect, None) -> 
      //   ctx.Log (sprintf "[HandShake] failed. No known leader.")
      //   exit 1

      // // ------------------------------------------------------------
      // | MembershipResponse (Redirect, Some node) -> 
      //   ctx.Log (sprintf "[HandShake] redirect to %A" node)
      //   EmptyResponse

      // ------------------------------------------------------------
      // | MembershipResponse (Hello, _) -> 
      //   ctx.Log (sprintf "[HandShake] all good ")
      //   EmptyResponse

      // | MembershipResponse (ByeBye, _) -> 
      //   ctx.Log (sprintf "[HandShake] bye bye ")
      //   EmptyResponse

      // ------------------------------------------------------------
      | ErrorResponse err ->
        ctx.Log (sprintf "[error] %A" err)
        EmptyResponse

      // ------------------------------------------------------------
      | EmptyResponse -> EmptyResponse


  //////////////////////////////////////////////////////////
  //                                 _                    //
  //  ___  ___ _ ____   _____ _ __  | | ___   ___  _ __   //
  // / __|/ _ \ '__\ \ / / _ \ '__| | |/ _ \ / _ \| '_ \  //
  // \__ \  __/ |   \ V /  __/ |    | | (_) | (_) | |_) | //
  // |___/\___|_|    \_/ \___|_|    |_|\___/ \___/| .__/  //
  //                                              |_|     //
  //////////////////////////////////////////////////////////

  let serverLoop (context: AppContext) server =
    let rec proc () =
      async {
        let mutable raw = new Message ()
        Message.recv &raw server |> ignore

        let msg : Msg =
          Message.data raw
          |> context.Coder.UnPickle

        let result = handleMessage context msg

        match result with
          | EmptyResponse -> ()         // should not really happen
          | _ as thing ->
            thing                       // go for it
            |> context.Coder.Pickle
            |> send server

        return! proc ()
      }

    Async.Start(proc ())

  ///////////////////////////////////////////
  //  ____           _           _ _       //
  // |  _ \ ___ _ __(_) ___   __| (_) ___  //
  // | |_) / _ \ '__| |/ _ \ / _` | |/ __| //
  // |  __/  __/ |  | | (_) | (_| | | (__  //
  // |_|   \___|_|  |_|\___/ \__,_|_|\___| //
  ///////////////////////////////////////////

  let timeout = 10u

  let periodicLoop (context: AppContext) =
    let rec proc () =
      async {
          Thread.Sleep(int timeout) // sleep for 100ms
          context.Periodic timeout  // kick the machine
          return! proc ()           // recurse
        }

    Async.Start(proc())

  ////////////////////////////////////////
  //  __  __       _                    //
  // |  \/  | __ _(_)_ __               //
  // | |\/| |/ _` | | '_ \              //
  // | |  | | (_| | | | | |             //
  // |_|  |_|\__,_|_|_| |_|             //
  ////////////////////////////////////////
  [<EntryPoint>]
  let main args =

    // 1. Initialise the application context from the supplied options
    let options = parseOptions args 
    use context = new AppContext(options)

    // 2. Start the server loop.
    use server = rep context.Context
    context.State
    |> self
    |> Node.getData
    |> formatUri
    |> bind server

    // 3. Enter the server loop to begin listening for incoming messages
    serverLoop context server

    // 4.Initialize and start Raft'ing :)
    context.Start()

    // 5. Start the periodic loop.
    periodicLoop context

    // 6. Start the console input loop.
    printfn "Welcome to the Raft REPL. Type help to see all commands."
    consoleLoop context

    0
