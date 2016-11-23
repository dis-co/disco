namespace Iris.Service

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
// open Iris.Service.Persistence

module Util =
  open System.Collections.Concurrent

  let authorizedUsers =
    ConcurrentDictionary<string, User>()
    
  let users = [
    { Id        = Id.Create()
    ; UserName  = "alfonso"
    ; FirstName = "Alfonso"
    ; LastName  = "García-Caro"
    ; Email     = "alfonso.garcia-caro@nsynk.de"
    ; Password  = "1234"
    ; Joined    = DateTime.Now
    ; Created   = DateTime.Now }
  ]

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

type MockService(?project: IrisProject ref) =
  // let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

  let config, state =
    let leader = Id "MOCKUP" |> Node.create
    let followers = List.init 4 (fun _ -> Id.Create() |> Node.create)
    let nodes = leader::followers
    let cluster =
      { Cluster.Name = "my-cluster"
      ; Nodes = nodes
      ; Groups = [] }
    let config =
      Config.create("mock-config")
      |> Config.updateCluster cluster
    let state =
      let nodes = nodes |> Seq.map (fun x -> x.Id, x) |> Map
      { State.Empty with Nodes = nodes; Users = Util.users |> Seq.map (fun u -> u.Id, u) |> Map }
    config, state
  let store : Store = new Store(state)

  // let kontext = new ZContext()

  // let raftserver = new RaftServer((!project).Config, kontext)

  let wsserver   = new WsServer() //(!project).Config)
  // let httpserver = new AssetServer() //(!project).Config)

  let setup _ =
    // WEBSOCKET
    wsserver.OnOpen <- fun (id: Id) ->
      wsserver.Send id (DataSnapshot store.State)
      // let msg =
      //   match raftserver.Append(AddSession session) with
      //   | Right entry -> Debug, (sprintf "Added session to Raft log with id: %A" entry.Id)
      //   | Left  error -> Err, string error
      // wsserver.Broadcast (LogMsg msg)

    wsserver.OnClose <- fun id ->
      // TODO: Remove session
//      store.Dispatch(RemoveSession session)
      ()
      // match Map.tryFind sessionid store.State.Sessions with
      // | Some session ->
      //   let msg =
      //     match raftserver.Append(RemoveSession session) with
      //     | Right entry -> Debug, (sprintf "Remove session added to Raft log with id: %A" entry.Id)
      //     | Left error  -> Err, string error
      //   wsserver.Broadcast (LogMsg msg)
      // | _ ->
      //   let msg = Err, "Session not found. Something spooky is going on"
      //   wsserver.Broadcast (LogMsg msg)

    wsserver.OnError <- fun id ->
      ()
      // match Map.tryFind sessionid store.State.Sessions with
      // | Some session ->
      //   let msg =
      //     match raftserver.Append(RemoveSession session) with
      //     | Right entry -> Debug, (sprintf "Remove session added to Raft log with id: %A" entry.Id)
      //     | Left error  -> Err, string error
      //   wsserver.Broadcast (LogMsg msg)
      // | _ ->
      //   let msg = Err, "Session not found. Something spooky is going on"
      //   wsserver.Broadcast (LogMsg msg)

    wsserver.OnMessage <- fun id command ->
      match command with
      | AddSession session ->
        match wsserver.BuildSession(id, session) with
        | Left err -> Error.exitWith err
        | Right session -> AddSession session

      | UpdateSession session when session.Status.StatusType = Login ->
        let username, password =
          // TODO: Validate format
          let info = session.Status.Payload.Split('\n')
          info.[0], info.[1]

        store.State.Users
        |> Map.tryPick (fun _ u -> if u.UserName = username then Some u else None)
        |> function
          | Some user when user.Password = password ->
            { session with Status = { StatusType = Authorized; Payload = string user.Id } }
          | _ ->
            { session with Status = { StatusType = Unathorized; Payload = "" } }
        |> UpdateSession
        
      | command -> command
      |> store.Dispatch

      wsserver.Broadcast (DataSnapshot store.State)
      // let msg =
      //   match raftserver.Append(command) with
      //   | Right entry -> Debug, (sprintf "Entry added to Raft log with id: %A" entry.Id)
      //   | Left error  -> Err, string error
      // wsserver.Broadcast (LogMsg msg)

    // RAFTSERVER
    // raftserver.OnConfigured <-
    //   Array.map (fun (node: RaftNode) -> string node.Id)
    //   >> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
    //   >> (fun str -> LogMsg(Iris.Core.LogLevel.Debug, str))
    //   >> wsserver.Broadcast

    // raftserver.OnLogMsg <- fun _ msg ->
    //   wsserver.Broadcast(LogMsg(Iris.Core.LogLevel.Debug, msg))

    // raftserver.OnNodeAdded   <- fun node ->
    //   warn "NODE NEEDS TO BE ADDED TO PROJECT NOW"
    //   AddNode node
    //   |> wsserver.Broadcast

    // raftserver.OnNodeUpdated <- fun node ->
    //   warn "NODE NEEDS TO BE UPDATED IN PROJECT NOW"
    //   UpdateNode node
    //   |> wsserver.Broadcast

    // raftserver.OnNodeRemoved <- fun node ->
    //   warn "NODE NEEDS TO BE REMOVED FROM PROJECT NOW"
    //   RemoveNode node
    //   |> wsserver.Broadcast

    // raftserver.OnApplyLog <- fun sm ->
    //   store.Dispatch sm
    //   wsserver.Broadcast sm
    //   if raftserver.IsLeader then
    //     persistEntry !project sm |> ignore
    //   else
    //     updateRepo !project

  do setup ()

  // member self.Raft
  //   with get () : RaftServer = raftserver

  //  ___       _             __
  // |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  ___
  //  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \/ __|
  //  | || | | | ||  __/ |  |  _| (_| | (_|  __/\__ \
  // |___|_| |_|\__\___|_|  |_|  \__,_|\___\___||___/
  //
  interface IDisposable with
    member self.Dispose() =
      self.Stop()

  //  _     _  __       ____           _
  // | |   (_)/ _| ___ / ___|   _  ___| | ___
  // | |   | | |_ / _ \ |  | | | |/ __| |/ _ \
  // | |___| |  _|  __/ |__| |_| | (__| |  __/
  // |_____|_|_|  \___|\____\__, |\___|_|\___|
  //                        |___/
  member self.Start(?web: bool) =
    // if defaultArg web true then
    //   httpserver.Start()
    wsserver.Start()
    // raftserver.Start()

  member self.Start() =
    // httpserver.Start()
    wsserver.Start()
    // raftserver.Start()

  member self.Stop() =
    // dispose raftserver
    dispose wsserver
    // dispose httpserver
    // dispose kontext

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  // member self.SaveProject(id, msg) =
  //   match saveProject id signature msg !state with
  //     | Success (commit, newstate) ->
  //       state := newstate
  //       Either.succeed commit
  //     | Fail err -> Either.fail err

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  // member self.CreateProject(name, path) =
  //   createProject name path signature !state
  //     >>= fun (project, state') ->
  //       // add and start the process groups for this project
  //       let result =
  //         project.Name
  //           >>= startProcess
  //           >>= (addProcess project.Id >> succeed)

  //       match result with
  //         | Success _ -> state := state'
  //                        self.Ctrl.Load(project.Id, project.Name)
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err
  //   printfn "hm"

  //   ____ _
  //  / ___| | ___  ___  ___
  // | |   | |/ _ \/ __|/ _ \
  // | |___| | (_) \__ \  __/
  //  \____|_|\___/|___/\___|

  // member self.CloseProject(pid) =
  //   findProject pid !state
  //     >>= fun project ->
  //       combine project (closeProject pid !state)
  //     >>= fun (project, state') ->
  //       // remove and stop process groups
  //       let result = removeProcess project.Id >>= stopProcess
  //       match result with
  //         | Success _ -> state := state'                    // save global state
  //                        self.Ctrl.Close(pid, project.Name) // notify everybody
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err

  //   printfn "fm"

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  // member self.LoadProject(path : FilePath) =
  //   loadProject path !state
  //     >>= fun (project, state') ->
  //       // add and start the process groups for this project
  //       let result =
  //         ProjectProcess.Create project
  //           >>= startProcess
  //           >>= (addProcess project.Id >> succeed)

  //       match result with
  //         | Success _ -> self.Ctrl.Load(project.Id, project.Name)
  //                        state := state'
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err
  //   printfn "oh"
