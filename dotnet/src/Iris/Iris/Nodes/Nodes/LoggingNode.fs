namespace VVVV.Nodes

open System
open System.Collections.Concurrent
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes
open FSharp.Reflection

//  _
// | |    ___   __ _  __ _  ___ _ __
// | |   / _ \ / _` |/ _` |/ _ \ '__|
// | |__| (_) | (_| | (_| |  __/ |
// |_____\___/ \__, |\__, |\___|_|
//             |___/ |___/

type private sa = string array

[<PluginInfo(Name="Logger", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type LoggingNode() =

  [<DefaultValue>]
  [<Output("Call Site")>]
  val mutable OutCallSite: ISpread<string>

  [<DefaultValue>]
  [<Output("LogLevel")>]
  val mutable OutLogLevel: ISpread<string>

  [<DefaultValue>]
  [<Output("Log")>]
  val mutable OutLog: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  let mutable initialized = false
  let mutable obs = Unchecked.defaultof<IDisposable>
  let logs = new ConcurrentQueue<LogEvent>();

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if not initialized then
        obs <- Logger.subscribe logs.Enqueue
        initialized <- true

      if logs.IsEmpty then
        self.OutCallSite.SliceCount <- 1
        self.OutLogLevel.SliceCount <- 1
        self.OutLog.SliceCount <- 1

        self.OutCallSite.AssignFrom [| |]
        self.OutLogLevel.AssignFrom [| |]
        self.OutLog.AssignFrom [| |]
        self.OutUpdate.[0] <- false
      else
        let current = logs.ToArray()
        let len = Array.length current

        self.OutCallSite.SliceCount <- len
        self.OutLogLevel.SliceCount <- len
        self.OutLog.SliceCount <- len

        let _, sites, levels, msgs =
          Array.fold
            (fun (i, (s: sa), (l: sa), (m: sa)) (log: LogEvent) ->
              s.[i] <- log.Tag
              l.[i] <- string log.LogLevel
              m.[i] <- log.Message
              (i + 1, s, l, m))
            (0, Array.zeroCreate len
              , Array.zeroCreate len
              , Array.zeroCreate len)
            current

        self.OutCallSite.AssignFrom sites
        self.OutLogLevel.AssignFrom levels
        self.OutLog.AssignFrom msgs
        self.OutUpdate.[0] <- true

        for _ in 0 .. len - 1 do
          match logs.TryDequeue() with
          | true,  _ -> ()
          | false, _ -> logs.TryDequeue() |> ignore // retry failed attempt

  interface IDisposable with
    member self.Dispose() =
      dispose obs
