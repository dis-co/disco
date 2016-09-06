namespace Iris.Tests.Raft

open System.Net
open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Raft

[<AutoOpen>]
module Scenarios =
  type LogDataType = string

  type Sender =
    { Inbox  : List<Msg>
    ; Outbox : List<Msg>
    }

  and Msg =
    | RequestVote           of sender:NodeId * req:VoteRequest
    | RequestVoteResponse   of sender:NodeId * vote:VoteResponse
    | AppendEntries         of sender:NodeId * ae:AppendEntries
    | AppendEntriesResponse of sender:NodeId * ar:AppendResponse

  let create =
    { Inbox = List.empty<Msg>
    ; Outbox = List.empty<Msg>
    }

  ////////////////////////////////////////////////////////////////////////////
  //   ___  ____  ____  _____ ____    ____  _____ ___  ____  _     _____    //
  //  / _ \|  _ \|  _ \| ____|  _ \  |  _ \| ____/ _ \|  _ \| |   | ____|   //
  // | | | | |_) | | | |  _| | |_) | | |_) |  _|| | | | |_) | |   |  _|     //
  // | |_| |  _ <| |_| | |___|  _ <  |  __/| |__| |_| |  __/| |___| |___ _  //
  //  \___/|_| \_\____/|_____|_| \_\ |_|   |_____\___/|_|   |_____|_____( ) //
  //                                                                    |/  //
  //   ___  ____  ____  _____ ____                                          //
  //  / _ \|  _ \|  _ \| ____|  _ \                                         //
  // | | | | |_) | | | |  _| | |_) |     messages are not being delivered   //
  // | |_| |  _ <| |_| | |___|  _ <      correctly                          //
  //  \___/|_| \_\____/|_____|_| \_\                                        //
  ////////////////////////////////////////////////////////////////////////////



  let __logg str = ignore str // printfn "%s" str

  let __append_msgs (peers: Map<NodeId,Sender ref>) (sid:NodeId) (rid:NodeId) msg =
    let sender = Map.find sid peers
    let receiver = Map.find rid peers
    sender   := { !sender   with Outbox = msg :: (!sender).Outbox  }
    receiver := { !receiver with Inbox  = msg :: (!receiver).Inbox }

  let testRequestVote (peers: Map<NodeId,Sender ref>) (sender:RaftNode) (receiver:RaftNode) req =
    __logg <| sprintf "testRequestVote [sender: %A] [receiver: %A]" sender.Id receiver.Id
    RequestVote(sender.Id,req)
    |> __append_msgs peers sender.Id receiver.Id
    None

  let testRequestVoteResponse (peers: Map<NodeId,Sender ref>) (sender:RaftNode) (receiver:RaftNode) resp =
    __logg <| sprintf "testRequestVoteResponse [sender: %A] [receiver: %A]" sender.Id receiver.Id
    RequestVoteResponse(sender.Id,resp)
    |> __append_msgs peers sender.Id receiver.Id

  let testAppendEntries (peers: Map<NodeId,Sender ref>) (sender:RaftNode) (receiver:RaftNode) ae =
    __logg <| sprintf "testAppendEntries"
    AppendEntries(sender.Id,ae)
    |> __append_msgs peers sender.Id receiver.Id
    None

  let testAppendEntriesResponse (peers: Map<NodeId,Sender ref>) (sender:RaftNode) (receiver:RaftNode) resp =
    __logg <| sprintf "testAppendEntriesResponse"
    AppendEntriesResponse(sender.Id,resp)
    |> __append_msgs peers sender.Id receiver.Id

  let testInstallSnapshot (peers: Map<NodeId,Sender ref>) (sender: RaftNode) (receiver: RaftNode) is =
    __logg <| sprintf "testInstallSnapshot"
    None

  let pollMsgs peers =
    let _process msg =
      raft {
        match msg with
          | RequestVote(sid,req) ->
            let! peer = getNodeM sid
            if Option.isSome peer
            then
              let sender = Option.get peer
              __logg <| sprintf "    process request vote %A" req
              let! response = receiveVoteRequest sid req

              let! raft' = get
              let receiver = self raft'
              if not response.Granted then
                __logg <| sprintf "(1) result was not granted"
                let updated = { response with Term = raft'.CurrentTerm }
                let msg = RequestVoteResponse(receiver.Id, updated)
                __append_msgs peers receiver.Id sender.Id msg
              else
                let msg = RequestVoteResponse(receiver.Id, response)
                __append_msgs peers receiver.Id sender.Id msg
          | RequestVoteResponse(sid,resp) ->
            __logg "    process request vote response"
            do! receiveVoteResponse sid resp
          | AppendEntries(sid,ae) ->
            __logg "process appendentries"
            let! response = receiveAppendEntries (Some sid) ae
            if not response.Success then
              __logg "(2) result was Fail"
            let! raft' = get
            let sender = self raft'
            let! peer = getNodeM sid
            match peer with
              | Some receiver ->
                let msg = AppendEntriesResponse(sender.Id, response)
                __append_msgs peers sender.Id receiver.Id msg
              | _ ->
                __logg "peer unknown"
          | AppendEntriesResponse (sid,ar) ->
            __logg "process appendentries response"
            do! receiveAppendEntriesResponse sid ar
      }

    raft {
      let! raft' = get
      let receiver = Map.find raft'.Node.Id peers

      let inbox = (!receiver).Inbox
      let outbox = (!receiver).Inbox

      receiver :=
        { !receiver with
            Inbox = List.empty
            Outbox = List.empty }

      for msg in inbox do
        do! _process msg
    }

  let numMsgs peer =
    List.length (!peer).Inbox

  let totalMsgs result _ peer =
    result + numMsgs peer

  let anyMsg result nid peer  =
    if result then result else numMsgs peer > 0

  let anyMsgs (peers: Map<NodeId,Sender ref>) =
    Map.fold (anyMsg) false peers

  // Do 50 iterations maximum. If unsure, turn up value.
  let config = { FsCheck.Config.Default with MaxTest = 50 }

  let scenario_leader_appears =
    testPropertyWithConfig config "leader appears" <| fun _ ->
      let numPeers = 3UL

      let ids =
        [| for n in 0UL .. (numPeers - 1UL) do
            yield Id.Create() |]

      let senders =
        [| for n in 0UL .. (numPeers - 1UL) do
            yield (ids.[int n], ref create) |]
        |> Map.ofArray

      let servers =
        [| for n in 0UL .. (numPeers - 1UL) do
            let peers =
              [| for pid in 0UL .. (numPeers - 1UL) do
                  let nid = ids.[int pid]
                  yield Node.create nid |]

            let callbacks =
              { SendRequestVote     = testRequestVote     senders peers.[int n]
              ; SendAppendEntries   = testAppendEntries   senders peers.[int n]
              ; SendInstallSnapshot = testInstallSnapshot senders peers.[int n]
              ; PersistSnapshot     = ignore
              ; PrepareSnapshot     = konst Log.empty
              ; RetrieveSnapshot    = konst None
              ; ApplyLog            = ignore
              ; NodeAdded           = ignore
              ; NodeUpdated         = ignore
              ; NodeRemoved         = ignore
              ; Configured          = ignore
              ; StateChanged        = fun _ -> ignore
              ; PersistVote         = ignore
              ; PersistTerm         = ignore
              ; PersistLog          = ignore
              ; DeleteLog           = ignore
              ; LogMsg              = fun _ _ -> ignore
              } :> IRaftCallbacks

            let raft =
              createRaft peers.[int n]
              |> setElectionTimeout 500UL
              |> addNodes peers

            yield (raft,callbacks) |]

      // First node starts election.
      periodic 1000UL
      |> evalRaft  (fst servers.[0]) (snd servers.[0])
      |> fun result ->
        Array.set servers 0 (result, snd servers.[0])

      expect "Should be candidate now" Candidate getState (fst servers.[0])

      for j in 0..19  do
        Map.fold totalMsgs 0 senders
        |> sprintf "Processing messages [iteration %d] [num msgs %d]" j
        |> __logg

        while anyMsgs senders do
          for idx in 0UL .. (numPeers - 1UL) do
            let srv = servers.[int idx]
            __logg <| sprintf "[raft: %d] [state: %A]" idx (fst srv).State

            pollMsgs senders
            |> evalRaft (fst srv) (snd srv)
            |> fun r -> Array.set servers (int idx) (r, snd srv)

        __logg "Running periodic"
        for n in 0UL .. (numPeers - 1UL) do
          let srv = servers.[int n]
          periodic 100UL
          |> evalRaft (fst srv) (snd srv)
          |> fun r -> Array.set servers (int n) (r, snd srv)

      let __fldr result raft =
        if isLeader raft then result + 1 else result

      let leaders = Array.map fst servers |> Array.fold __fldr 0
      Assert.Equal("System should have have one leader", 1, leaders)
