namespace Disco.Core

module Metrics =

  open System
  open System.Collections.Generic
  open InfluxDB.Collector

  type private MetricsAgent = MailboxProcessor<string * obj>

  let mutable private agent: MetricsAgent option = None  

  let private createCollector (config: DiscoMachine) =
    let conf = CollectorConfiguration() 
    let url = sprintf "http://%O:%O" config.MetricsHost config.MetricsPort
    url
    |> sprintf "Collecting metrics at url: %A"
    |> Logger.debug "Metrics" 
    conf
      .Tag.With("host", unwrap config.HostName)
      .Tag.With("host_id", string config.MachineId)
      .Batch.AtInterval(TimeSpan.FromSeconds(1.))
      .WriteTo.InfluxDB(url, config.MetricsDb)
      .CreateCollector()

  let init (config: DiscoMachine): Either<DiscoError,unit> =
    try 
      if config.CollectMetrics then
        match agent with 
        | Some _ -> Either.nothing
        | None -> 
          let mbp = MailboxProcessor.Start(fun inbox -> 
            let collector = createCollector config
            let rec loop () =
              async {
                let! (name,value) = inbox.Receive()
                do collector.Increment "iterations"
                let values = Dictionary<string,obj>()
                do values.Add(name, value)
                do collector.Write(name, values)
                return! loop()
              }
            loop())
          mbp.Error.Add(printfn "unhandled error on actor loop: %O" )
          agent <- Some mbp
          Either.nothing
      else Either.nothing
    with exn ->
      exn.Message
      |> Error.asIOError "Metrics.init"
      |> Either.fail
      
  let collect (name: string) (value: obj) : unit =
    match agent with
    | Some agent -> (name, value) |> agent.Post
    | None -> ()