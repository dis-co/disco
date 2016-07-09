namespace Pallet.Tests

open System.Net
open Fuchu
open Fuchu.Test
open Pallet.Core

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
      let log : Log<unit,unit> = Log.empty
      expect "Should be zero" 0UL Log.length log

  let log_is_non_empty =
    testCase "When create, a log should not be empty" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1UL ())
      |> assume "Should be one"  1UL  Log.length
      |> Log.append (Log.make 1UL ())
      |> assume "Should be two" 2UL Log.length
      |> Log.append (Log.make 1UL ())
      |> assume "Should be two"  3UL  Log.length
      |> ignore

  let log_have_correct_index =
    testCase "When I add an entry, it should have the correct index" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1UL ())
      |> assume "Should have currentIndex 1" 1UL Log.index
      |> assume "Should have currentTerm 1" 1UL Log.term
      |> assume "Should have no lastTerm" None Log.prevTerm
      |> assume "Should have no lastIndex" None Log.prevIndex

      |> Log.append (Log.make 1UL ())

      |> assume "Should have currentIndex 2" 2UL Log.index
      |> assume "Should have currentTerm 1" 1UL Log.term
      |> assume "Should have lastTerm 1" (Some 1UL) Log.prevTerm
      |> assume "Should have lastIndex 1" (Some 1UL) Log.prevIndex
      |> ignore


  let log_get_at_index =
    testCase "When I get an entry by index, it should be equal" <| fun _ ->
      let id1 = RaftId.Create()
      let id2 = RaftId.Create()
      let id3 = RaftId.Create()

      let log =
        Log.empty
        |> Log.append (LogEntry(id1, 0UL, 1UL, (), None))
        |> Log.append (LogEntry(id2, 0UL, 1UL, (), None))
        |> Log.append (LogEntry(id3, 0UL, 1UL, (), None))

      Log.at 1UL log
      |> assume "Should be correct one" id1 (Log.id << Option.get)
      |> ignore

      Log.at 2UL log
      |> assume "Should also be correct one" id2 (Log.id << Option.get)
      |> ignore

      Log.at 3UL log
      |> assume "Should also be correct one" id3 (Log.id << Option.get)
      |> ignore

      expect "Should find none at invalid index" None (Log.at 8UL) log

  let log_find_by_id =
    testCase "When I get an entry by index, it should be equal" <| fun _ ->
      let id1 = RaftId.Create()
      let id2 = RaftId.Create()
      let id3 = RaftId.Create()

      let log =
        Log.empty
        |> Log.append (LogEntry(id1, 0UL, 1UL, (), None))
        |> Log.append (LogEntry(id2, 0UL, 1UL, (), None))
        |> Log.append (LogEntry(id3, 0UL, 1UL, (), None))

      Log.find id1 log
      |> assume "Should be correct one" id1 (Log.id << Option.get)
      |> ignore

      Log.find id2 log
      |> assume "Should also be correct one" id2 (Log.id << Option.get)
      |> ignore

      Log.find id3 log
      |> assume "Should also be correct one" id3 (Log.id << Option.get)
      |> ignore

      Log.find (RaftId.Create()) log
      |> assume "Should find none at invalid index" true Option.isNone
      |> ignore

  let log_depth_test =
    testCase "Should have the correct log depth" <| fun _ ->
      Log.empty
      |> Log.append (Log.make 1UL ())
      |> Log.append (Log.make 1UL ())
      |> Log.append (Log.make 1UL ())
      |> assume "Should have length 3" 3UL Log.length
      |> Log.append (Log.make 1UL ())
      |> Log.append (Log.make 1UL ())
      |> assume "Should have depth 5" 5UL Log.length
      |> ignore

  let log_resFold_short_circuit_test =
    testCase "Should short-circuit when folder fails" <| fun _ ->
      let log =
        Log.empty
        |> Log.append (Log.make 1UL 1)
        |> Log.append (Log.make 1UL 2)
        |> Log.append (Log.make 1UL 3)

      let folder (m: int) (log: LogEntry<int,_>) =
        let value = (Log.data >> Option.get) log
        if value = 2
        then Log.finish (m + 9)
        else Log.next   (m + 2)

      log.Data
      |> Option.get
      |> Log.aggregate folder 0
      |> assume "Should be 11" 11 id // if it does short-circuit, it should not
      |> ignore                       // add more to the result!

  let log_concat_length_test =
    testCase "Should have correct length" <| fun _ ->
      let log =
        Log.empty
        |> Log.append (Log.make 1UL "four")
        |> Log.append (Log.make 1UL "five")
        |> Log.append (Log.make 1UL "six")

      log
      |> Log.append (Log.make 1UL "one")
      |> Log.append (Log.make 1UL "two")
      |> Log.append (Log.make 1UL "three")
      |> assume "Should have length 6" 6UL Log.length
      |> ignore

  let log_concat_monotonicity_test =
    testCase "Should have monotonic index" <| fun _ ->
      let isMonotonic log =
        let __mono (last,ret) _log =
          let i = Log.entryIndex _log
          if ret then (i, i = (last + 1UL)) else (i, ret)
        Log.foldLogR __mono (0UL,true) log

      let log =
        Log.empty
        |> Log.append (Log.make 1UL "four")
        |> Log.append (Log.make 1UL "five")
        |> Log.append (Log.make 1UL "six")

      log
      |> Log.append (Log.make 1UL "one")
      |> Log.append (Log.make 1UL "two")
      |> Log.append (Log.make 1UL "three")
      |> assume "Should be monotonic" true (isMonotonic >> snd)
      |> ignore

  let log_get_entries_until_test =
    testCase "Get all entries until (and including) a given index" <| fun _ ->
      let getData log = Log.map (Log.data >> Option.get) log

      Log.empty
      |> Log.append (Log.make 1UL "one")
      |> Log.append (Log.make 1UL "two")
      |> Log.append (Log.make 1UL "three")
      |> Log.append (Log.make 1UL "four")
      |> Log.append (Log.make 1UL "five")
      |> Log.append (Log.make 1UL "six")
      |> Log.until 4UL
      |> assume "Should have 3 logs" 3UL (Option.get >> Log.depth)
      |> assume "Should have log with these values" ["six"; "five"; "four"] (Option.get >> getData)
      |> ignore

  let log_concat_ensure_no_duplicate_entries =
    testCase "concat ensure no duplicate entires" <| fun _ ->
      let id1 = RaftId.Create()
      let id2 = RaftId.Create()

      let term = 1UL

      let idx1 = 1UL
      let idx2 = 2UL

      let entries =
        LogEntry(id2,idx2,term,"two",
                 Some <| LogEntry(id1,idx1,term,"one",None))

      let log = Log.fromEntries entries

      Log.append entries log
      |> expect "Should be the same" log id

  let log_append_ensure_no_duplicate_entries =
    testCase "append ensure no duplicate entires" <| fun _ ->
      let id1 = RaftId.Create()
      let id2 = RaftId.Create()

      let term = 1UL
      let idx1 = 1UL
      let idx2 = 2UL

      let entries =
        LogEntry(id2,idx2,term,"two",
                 Some <| LogEntry(id1,idx1,term,"one",None))

      let log = Log.fromEntries entries

      Log.append entries log
      |> expect "Should be the same" log id

  let log_concat_ensure_no_duplicate_but_unique_entries =
    testCase "concat ensure no duplicate but unique entries" <| fun _ ->
      let id1 = RaftId.Create()
      let id2 = RaftId.Create()
      let id3 = RaftId.Create()

      let term = 1UL
      let idx1 = 1UL
      let idx2 = 2UL
      let idx3 = 3UL

      let entires =
        LogEntry(id2,idx2,term,"two",
                 Some <| LogEntry(id1,idx1,term,"one",None))

      let log = Log.fromEntries entires

      let newer =
        LogEntry(id3,idx3,term,"three",
                 Some <| LogEntry(id2,idx2,term,"two",
                                  Some <| LogEntry(id1,idx1,term,"one",None)))

      Log.append newer log
      |> assume "Should have length 3" 3UL Log.size
      |> expect "Should have proper id" id3 (Log.entries >> Option.get >> Log.id)


  let log_snapshot_remembers_last_state =
    testCase "snapshot remembers last state" <| fun _ ->
      let term = 8UL
      let data = [ "one"; "two"; "three"; "four" ]

      let nodes =
        [ for n in 0UL .. 5UL do
            yield Node.create (RaftId.Create()) ("Client " + string n) ]
        |> Array.ofList

      let log =
        List.fold (fun l t -> Log.append (Log.make term t) l) Log.empty data

      Log.snapshot nodes "four" log
      |> assume "Should have correct lastTerm" (Some term) Log.lastTerm
      |> expect "Should have correct lastIndex" (Some <| Log.index log) Log.lastIndex

  let log_untilExcluding_should_return_expected_enries =
    testCase "untilExcluding should return expected enries" <| fun _ ->
      let num = 30UL

      [ for n in 1UL .. num do
          yield string n ]
      |> List.fold (fun m s -> Log.append (Log.make 0UL s) m) Log.empty
      |> assume "Should be at correct index" num         Log.size
      |> assume "Should pick correct item"   16UL       (Log.untilExcluding 15UL >> Option.get >> Log.last >> Log.entryIndex)
      |> assume "Should have correct index" (Some "16") (Log.untilExcluding 15UL >> Option.get >> Log.last >> Log.data)
      |> assume "Should have correct index" (Some "15") (Log.until 15UL >> Option.get >> Log.last >> Log.data)
      |> ignore

  let log_append_should_work_with_snapshots_too =
    testCase "append should work with snapshots too" <| fun _ ->
      let log = Log.empty |> Log.append (Snapshot(RaftId.Create(), 0UL, 0UL, 9UL, 1UL, Array.empty, "hello"))
      expect "Log should be size 1" 1UL Log.size log

  let log_firstIndex_should_return_correct_results =
    testCase "firstIndex should return correct results" <| fun _ ->
      let random = System.Random()

      let indices, log =
        let fidxs = ref List.empty

        let combine a b = (a, b)

        let def = LogEntry(RaftId.Create(),0UL,0UL,(),None)

        let folder log (id,term,index) =
          LogEntry(id,index,term,(),Some log)

        [ for term in 1UL .. 4UL do
            let offset = random.Next(1,60)
            for index in uint64(offset) .. uint64(offset + random.Next(10,70)) do
              let (_,t,i) as result = (RaftId.Create(), term, index)
              if index = uint64(offset) then
                fidxs := (t,i) :: !fidxs
              yield result ]
        |> List.fold folder def
        |> Log.fromEntries
        |> combine (!fidxs |> Map.ofList)

      for term in 1UL .. 4UL do
        let fidx = Log.firstIndex term log
        let result = Map.tryFind term indices
        expect "Should be equal" result id fidx

  let log_getn_should_return_right_number_of_entries =
    testCase "getn should return right number of entries" <| fun _ ->
      let n = 20

      let log =
        [ for n in 0 .. (n - 1) do
            yield n ]
        |> List.fold (fun m n -> Log.append (Log.make 0UL n) m) Log.empty

      expect "should have correct depth" (uint64 n) Log.size log

      let get = System.Random().Next(1,n - 1) |> uint64
      expect "should get corrent number" get Log.depth (Log.getn get log |> Option.get)
