(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System
open System.Threading
open System.Diagnostics
open System.Collections.Concurrent
open Expecto
open FsCheck
open Disco.Core
open Disco.Raft
open Disco.Service
open Disco.Net
open Microsoft.FSharp.Control

[<AutoOpen>]
module ActorTests =

  [<Literal>]
  let NUM = 10000

  type Measurement = unit -> unit
  type Measurements = ConcurrentQueue<int64>

  let measurement (measurements:Measurements) =
    let watch = Stopwatch()
    watch.Start()
    fun () ->
      watch.Stop()
      measurements.Enqueue watch.ElapsedMilliseconds

  let measure (measurements:Measurements) (actor:IActor<Measurement>) =
    measurements
    |> measurement
    |> actor.Post

  let asyncLoop _ msg = async { msg() }
  let threadLoop _ msg = msg()

  let medianTime (arr: int64 array) =
    let sum = Array.fold (+) 0L arr
    decimal sum / (decimal arr.Length)

  let analyse tag (measurements:Measurements) =
    measurements.ToArray()
    |> medianTime
    |> printfn "median %s latency in ms: %f" tag

  let test_actor_performance =
    testCase "actor performance" <| fun _ ->
      let asyncMeasurements = Measurements()
      let threadMeasurements = Measurements()

      let asyncActor = AsyncActor.create "async" asyncLoop
      let threadActor = ThreadActor.create "thread" threadLoop

      threadActor.Start()
      asyncActor.Start()

      ignore [ for n in 1 .. NUM -> measure asyncMeasurements asyncActor ]
      while asyncMeasurements.Count <> NUM do Thread.Sleep(1)

      ignore [ for n in 1 .. NUM -> measure threadMeasurements threadActor ]
      while threadMeasurements.Count <> NUM do Thread.Sleep(1)

      analyse "async"  asyncMeasurements
      analyse "thread" threadMeasurements

  let actorTests =
    ftestList "Actor Tests" [
      test_actor_performance
    ]
