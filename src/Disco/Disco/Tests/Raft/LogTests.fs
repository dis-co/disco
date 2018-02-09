(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests.Raft

open System.Net
open Expecto
open Disco.Raft
open Disco.Core

[<AutoOpen>]
module Log =

  ////////////////////////////////////////
  //  _                                 //
  // | |    ___   __ _                  //
  // | |   / _ \ / _` |                 //
  // | |__| (_) | (_| |                 //
  // |_____\___/ \__, |                 //
  //             |___/                  //
  ////////////////////////////////////////

  let log_new_log_is_empty =
    testCase "When create, a log should be empty" <| fun _ ->
      let log: Log = Log.empty
      expect "Should be zero" 0 Log.length log

  let log_is_non_empty =
    testCase "When create, a log should not be empty" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should be one"  1  Log.length
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should be two" 2 Log.length
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should be three" 3 Log.length
      |> ignore

  let log_have_correct_index =
    testCase "When I add an entry, it should have the correct index" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should have currentIndex 1" 1<index> Log.index
      |> assume "Should have currentTerm 1" 1<term> Log.term
      |> assume "Should have no lastTerm" None Log.prevTerm
      |> assume "Should have no lastIndex" None Log.prevIndex

      |> Log.append (Log.make 1<term> defSM)

      |> assume "Should have currentIndex 2" 2<index> Log.index
      |> assume "Should have currentTerm 1" 1<term> Log.term
      |> assume "Should have lastTerm 1" (Some 1<term>) Log.prevTerm
      |> assume "Should have lastIndex 1" (Some 1<index>) Log.prevIndex
      |> ignore


  let log_get_at_index =
    testCase "When I get an entry by index, it should be equal" <| fun _ ->
      let id1 = DiscoId.Create()
      let id2 = DiscoId.Create()
      let id3 = DiscoId.Create()

      let log =
        Log.empty
        |> Log.append (LogEntry(id1, 0<index>, 1<term>, defSM, None))
        |> Log.append (LogEntry(id2, 0<index>, 1<term>, defSM, None))
        |> Log.append (LogEntry(id3, 0<index>, 1<term>, defSM, None))

      Log.at 1<index> log
      |> assume "Should be correct one" id1 (LogEntry.id << Option.get)
      |> ignore

      Log.at 2<index> log
      |> assume "Should also be correct one" id2 (LogEntry.id << Option.get)
      |> ignore

      Log.at 3<index> log
      |> assume "Should also be correct one" id3 (LogEntry.id << Option.get)
      |> ignore

      expect "Should find none at invalid index" None (Log.at 8<index>) log

  let log_find_by_id =
    testCase "When I get an entry by index, it should be equal" <| fun _ ->
      let id1 = DiscoId.Create()
      let id2 = DiscoId.Create()
      let id3 = DiscoId.Create()

      let log =
        Log.empty
        |> Log.append (LogEntry(id1, 0<index>, 1<term>, defSM, None))
        |> Log.append (LogEntry(id2, 0<index>, 1<term>, defSM, None))
        |> Log.append (LogEntry(id3, 0<index>, 1<term>, defSM, None))

      Log.find id1 log
      |> assume "Should be correct one" id1 (LogEntry.id << Option.get)
      |> ignore

      Log.find id2 log
      |> assume "Should also be correct one" id2 (LogEntry.id << Option.get)
      |> ignore

      Log.find id3 log
      |> assume "Should also be correct one" id3 (LogEntry.id << Option.get)
      |> ignore

      Log.find (DiscoId.Create()) log
      |> assume "Should find none at invalid index" true Option.isNone
      |> ignore

  let log_depth_test =
    testCase "Should have the correct log depth" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should have length 3" 3 Log.length
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should have depth 5" 5 Log.length
      |> ignore

  let log_resFold_short_circuit_test =
    testCase "Should short-circuit when folder fails" <| fun _ ->
      let sm = AddCue { Id = DiscoId.Create(); Name = name "Wonderful"; Slices = [| |] }
      let log =
        Log.empty
        |> Log.append (Log.make 1<term> defSM)
        |> Log.append (Log.make 1<term> sm)
        |> Log.append (Log.make 1<term> defSM)

      let folder (m: int) (log: LogEntry) : Continue<int> =
        let value = (LogEntry.data >> Option.get) log
        if value = sm
        then LogEntry.finish (m + 9)
        else LogEntry.next   (m + 2)

      log.Data
      |> Option.get
      |> LogEntry.aggregate folder 0
      |> assume "Should be 11" 11 id // if it does short-circuit, it should not
      |> ignore                     // add more to the result!

  let log_concat_length_test =
    testCase "Should have correct length" <| fun _ ->
      let log =
        Log.empty
        |> Log.append (Log.make 1<term> defSM)
        |> Log.append (Log.make 1<term> defSM)
        |> Log.append (Log.make 1<term> defSM)

      log
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should have length 6" 6 Log.length
      |> ignore

  let log_concat_monotonicity_test =
    testCase "Should have monotonic index" <| fun _ ->
      let isMonotonic log =
        let __mono (last,ret) _log =
          let i = LogEntry.index _log
          if ret then (i, i = (last + 1<index>)) else (i, ret)
        Log.foldLogR __mono (0<index>,true) log

      let log =
        Log.empty
        |> Log.append (Log.make 1<term> defSM)
        |> Log.append (Log.make 1<term> defSM)
        |> Log.append (Log.make 1<term> defSM)

      log
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> Log.append (Log.make 1<term> defSM)
      |> assume "Should be monotonic" true (isMonotonic >> snd)
      |> ignore

  let log_get_entries_until_test =
    testCase "Get all entries until (and including) a given index" <| fun _ ->
      let cues : Cue array =
        [| "one"; "two"; "three"; "four"; "five"; "six" |]
        |> Array.map (fun name' -> { Id = DiscoId.Create(); Name = name name'; Slices = [| |] })

      let getData log =
        LogEntry.map
          (fun entry ->
            match LogEntry.data entry with
            | Some data -> data
            | None      -> failwith "oooops")
          log

      Log.empty
      |> Log.append (Log.make 1<term> (AddCue cues.[0]))
      |> Log.append (Log.make 1<term> (AddCue cues.[1]))
      |> Log.append (Log.make 1<term> (AddCue cues.[2]))
      |> Log.append (Log.make 1<term> (AddCue cues.[3]))
      |> Log.append (Log.make 1<term> (AddCue cues.[4]))
      |> Log.append (Log.make 1<term> (AddCue cues.[5]))
      |> Log.until 4<index>
      |> assume "Should have 3 logs" 3 (Option.get >> LogEntry.depth)
      |> assume "Should have log with these values" [AddCue cues.[5]; AddCue cues.[4]; AddCue cues.[3]] (Option.get >> getData)
      |> ignore

  let log_concat_ensure_no_duplicate_entries =
    testCase "concat ensure no duplicate entires" <| fun _ ->
      let id1 = DiscoId.Create()
      let id2 = DiscoId.Create()

      let term = 1<term>

      let idx1 = 1<index>
      let idx2 = 2<index>

      let entries =
        LogEntry(id2,idx2,term,DataSnapshot (State.Empty),
                 Some <| LogEntry(id1,idx1,term,DataSnapshot(State.Empty),None))

      let log = Log.fromEntries entries

      Log.append entries log
      |> expect "Should be the same" log id

  let log_append_ensure_no_duplicate_entries =
    testCase "append ensure no duplicate entires" <| fun _ ->
      let id1 = DiscoId.Create()
      let id2 = DiscoId.Create()

      let term = 1<term>
      let idx1 = 1<index>
      let idx2 = 2<index>

      let entries =
        LogEntry(id2,idx2,term,DataSnapshot(State.Empty),
                 Some <| LogEntry(id1,idx1,term,DataSnapshot(State.Empty),None))

      let log = Log.fromEntries entries

      Log.append entries log
      |> expect "Should be the same" log id

  let log_concat_ensure_no_duplicate_but_unique_entries =
    testCase "concat ensure no duplicate but unique entries" <| fun _ ->
      let id1 = DiscoId.Create()
      let id2 = DiscoId.Create()
      let id3 = DiscoId.Create()

      let term = 1<term>
      let idx1 = 1<index>
      let idx2 = 2<index>
      let idx3 = 3<index>

      let entires =
        LogEntry(id2,idx2,term,DataSnapshot(State.Empty),
                 Some <| LogEntry(id1,idx1,term,DataSnapshot(State.Empty),None))

      let log = Log.fromEntries entires

      let newer =
        LogEntry(id3,idx3,term,DataSnapshot(State.Empty),
                 Some <| LogEntry(id2,idx2,term,DataSnapshot(State.Empty),
                                  Some <| LogEntry(id1,idx1,term,DataSnapshot(State.Empty),None)))

      Log.append newer log
      |> assume "Should have length 3" 3 Log.length
      |> expect "Should have proper id" id3 (Log.entries >> Option.get >> LogEntry.id)


  let log_snapshot_remembers_last_state =
    testCase "snapshot remembers last state" <| fun _ ->
      let term = 8<term>
      let data =
        [ for i in 0 .. 3 do
            yield DataSnapshot(State.Empty) ]

      let mems =
        [ for n in 0u .. 5u do
            yield Member.create (DiscoId.Create()) ]
        |> Array.ofList

      let log =
        List.fold (fun l t -> Log.append (Log.make term t) l) Log.empty data

      Log.snapshot mems (DataSnapshot(State.Empty)) log
      |> assume "Should have correct lastTerm" (Some term) Log.lastTerm
      |> expect "Should have correct lastIndex" (Some <| Log.index log) Log.lastIndex

  let log_untilExcluding_should_return_expected_enries =
    testCase "untilExcluding should return expected enries" <| fun _ ->
      let num = 30
      let id = DiscoId.Create()
      [ for n in 1 .. num do
          yield AddCue {
            Id = id
            Name = name (string n)
            Slices = Array.empty
          } ]
      |> List.fold (fun m s -> Log.append (Log.make 0<term> s) m) Log.empty
      |> assume "Should be at correct index" num Log.length
      |> assume "Should pick correct item"  16<index> (Log.untilExcluding 15<index> >> Option.get >> LogEntry.last >> LogEntry.index)
      |> assume "Should have correct index" (AddCue { Id = id; Name = name "16"; Slices = [| |] } |> Some) (Log.untilExcluding 15<index> >> Option.get >> LogEntry.last >> LogEntry.data)
      |> assume "Should have correct index" (AddCue { Id = id; Name = name "15"; Slices = [| |] } |> Some) (Log.until 15<index> >> Option.get >> LogEntry.last >> LogEntry.data)
      |> ignore

  let log_append_should_work_with_snapshots_too =
    testCase "append should work with snapshots too" <| fun _ ->
      let log =
        Log.empty
        |> Log.append (Snapshot(DiscoId.Create(), 0<index>, 0<term>, 9<index>, 1<term>, Array.empty, DataSnapshot(State.Empty)))

      expect "Log should be size 1" 1 Log.length log

  let log_firstIndex_should_return_correct_results =
    testCase "firstIndex should return correct results" <| fun _ ->
      let random = System.Random()

      let indices, log =
        let fidxs = ref List.empty

        let combine a b = (a, b)

        let def = LogEntry(DiscoId.Create(),0<index>,0<term>,defSM,None)

        let folder log (id,term,index) =
          LogEntry(id,index,term,defSM,Some log)

        [ for trm in 1 .. 4 do
            let offset = random.Next(1,60)
            for idx in offset .. offset + random.Next(10,70) do
              let (_,t,i) as result = (DiscoId.Create(), 1<term> * trm, 1<index> * idx)
              if idx = offset then
                fidxs := (t,i) :: !fidxs
              yield result ]
        |> List.fold folder def
        |> Log.fromEntries
        |> combine (!fidxs |> Map.ofList)

      for trm in 1 .. 4 do
        let fidx = Log.firstIndex (1<term> * trm) log
        let result = Map.tryFind (1<term> * trm) indices
        expect "Should be equal" result id fidx

  let log_getn_should_return_right_number_of_entries =
    testCase "getn should return right number of entries" <| fun _ ->
      let n = 20

      let log =
        [ for n in 0 .. (n - 1) do
            yield DataSnapshot(State.Empty) ]
        |> List.fold (fun m n -> Log.append (Log.make 0<term> n) m) Log.empty

      expect "should have correct depth" n Log.length log

      let getter = System.Random().Next(1,n - 1)
      expect "should get corrent number" getter LogEntry.depth (Log.getn getter log |> Option.get)
