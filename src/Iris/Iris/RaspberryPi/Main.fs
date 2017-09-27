namespace Iris.RaspberryPi

open Argu
open Iris.Core
open System
open System.IO
open System.Net
open System.Net.Sockets
open Iris.Client
open System.Threading
open Raspberry.IO.GeneralPurpose

[<AutoOpen>]
module Main =

  //   ____ _ _  ___        _   _
  //  / ___| (_)/ _ \ _ __ | |_(_) ___  _ __  ___
  // | |   | | | | | | '_ \| __| |/ _ \| '_ \/ __|
  // | |___| | | |_| | |_) | |_| | (_) | | | \__ \
  //  \____|_|_|\___/| .__/ \__|_|\___/|_| |_|___/
  //                 |_|

  type CliOptions =
    | [<AltCommandLine("-v")>] Verbose
    | [<AltCommandLine("-h")>] Host of string
    | [<AltCommandLine("-p")>] Port of uint16

    interface IArgParserTemplate with
      member self.Usage =
        match self with
        | Verbose -> "be more verbose"
        | Host _  -> "specify the iris services' host to connect to (optional)"
        | Port _  -> "specify the iris services' port to connect on (optional)"

  [<Literal>]
  let private help = @"
    ____                 _  ____ _ _            _
   |  _ \ __ _ ___ _ __ (_)/ ___| (_) ___ _ __ | |_
   | |_) / _` / __| '_ \| | |   | | |/ _ \ '_ \| __|
   |  _ < (_| \__ \ |_) | | |___| | |  __/ | | | |_
   |_| \_\__,_|___/ .__/|_|\____|_|_|\___|_| |_|\__| © Nsynk,  2017
                  |_|"

  let defaultValue = 250 // milliseconds

  //  _   _           _       _         _
  // | | | |_ __   __| | __ _| |_ ___  | |    ___   ___  _ __
  // | | | | '_ \ / _` |/ _` | __/ _ \ | |   / _ \ / _ \| '_ \
  // | |_| | |_) | (_| | (_| | ||  __/ | |__| (_) | (_) | |_) |
  //  \___/| .__/ \__,_|\__,_|\__\___| |_____\___/ \___/| .__/
  //       |_|                                          |_|

  let private updater (led: OutputPinConfiguration)
                      (connection: GpioConnection)
                      (inbox: MailboxProcessor<int>) =
    let mutable timeout = defaultValue
    let rec loop () =
      async {
        let! msg = inbox.Receive()      // get the next message from the actor queue
        timeout <- msg                   // its a potentially new timeout values, so remember it
        connection.Toggle(led)          // toggle the LED state
        do! Async.Sleep(timeout)        // sleep for the specified timeout value
        do inbox.Post(timeout)          // and kick the machine
        return! loop()                  // before recursing
      }
    loop ()

  //  _   _           _       _
  // | | | |_ __   __| | __ _| |_ ___ _ __
  // | | | | '_ \ / _` |/ _` | __/ _ \ '__|
  // | |_| | |_) | (_| | (_| | ||  __/ |
  //  \___/| .__/ \__,_|\__,_|\__\___|_|
  //       |_|

  type IUpdater =
    inherit IDisposable                 // we'll have resources to clean up
    abstract Update: double -> unit      // the update function

  let startUpdater () =
    printfn "IsRaspi: %b" Raspberry.Board.Current.IsRaspberryPi
    let led = ConnectorPin.P1Pin13.Output() // the output pin we'll toggle
    let connection = new GpioConnection(led) // connection with that output
    let cts = new CancellationTokenSource()  // token to cancel the Actor
    let mbp = MailboxProcessor.Start(updater led connection, cts.Token) // an actor
    { new IUpdater with
        member updater.Update value =
          value |> int |> mbp.Post      // post a new timeout value onto the actor cue

        member updater.Dispose() =      // dispose of the actor and the gpio connection
          cts.Cancel()
          connection.Close() }

  let private handleWith (pinid: PinId) (updateWith: double -> unit) = function
    | ClientEvent.Registered ->
      Logger.info "RaspiExample" "successfully registered with server"
    | ClientEvent.UnRegistered ->
      Logger.info "RaspiExample" "successfully registered with server"
    /// | ClientEvent.Update (UpdateSlices(NumberSlices(id, None, slices))) when id = pinid ->
    ///   try updateWith slices.[0] with | _ -> ()
    | _ -> ()


  [<EntryPoint>]
  let main args =
    let parser = ArgumentParser.Create<CliOptions>(helpTextMessage = help)
    let parsed = parser.Parse args

    // create a new unique client id (must be a GUID)
    let clientId = IrisId.Create()

    do Logger.initialize {
      MachineId = clientId
      Tier = Tier.Client
      UseColors = false
      Level = LogLevel.Debug
    }

    let server =
      { Port =
          if parsed.Contains <@ Port @>
          then port (parsed.GetResult <@ Port @>)
          else port Constants.DEFAULT_API_PORT
        IpAddress =
          if parsed.Contains <@ Host @>
          then IPv4Address (parsed.GetResult <@ Host @>)
          else IPv4Address "127.0.0.1" }

    let client =
      { Id = clientId
        ServiceId = IrisId.Create()
        Name = name "Raspi Client"
        Role = Role.Renderer
        Status = ServiceStatus.Starting
        IpAddress = IpAddress.Localhost
        Port = port 0us }

    let pinid = IrisId.Create()
    let groupid = IrisId.Create()

    let client = ApiClient.create server client

    match client.Start() with
    | Right () ->
      let gpio = startUpdater()
      let obs = client.Subscribe (handleWith pinid gpio.Update)

      let info : Pin =
        Pin.Source.string (IrisId.Create()) (name "Board Info") groupid clientId [|
          string Raspberry.Board.Current.Model
          Raspberry.Board.Current.ProcessorName
          string Raspberry.Board.Current.Firmware
        |]

      let pin : Pin =
        Pin.Sink.number pinid (name "Led Frequency") groupid clientId [|
          double defaultValue
        |]

      let group : PinGroup =
        { Id = groupid
          Name = name "Raspi PinGroup"
          ClientId = clientId
          RefersTo = None
          Path = None
          Pins =
            Map.ofList [
              (info.Id, info)
              (pin.Id, pin)
            ] }

      client.AddPinGroup group

      let level =
        if parsed.Contains <@ Verbose @> then
          LogLevel.Debug
        else
          LogLevel.Info

      use obs = Logger.subscribe (Logger.stdoutWith level)

      let mutable run = true
      while run do
        match Console.ReadLine() with
        | "exit" -> run <- false
        | _ -> ()

      dispose client
      exit 0

    | Left error ->
      Console.Error.WriteLine("Encountered error starting client: {0}", Error.toMessage error)
      Console.Error.WriteLine("Aborting.")
      error
      |> Error.toExitCode
      |> exit
