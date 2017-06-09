namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities
open Iris.Service.Interfaces

open Suave
open Suave.Http
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Logging
open Suave.Logging.Log
open Suave.CORS
open Suave.Web

open System
open System.IO
open System.Text
open System.Threading
open System.Diagnostics

open System.Collections.Concurrent
open Microsoft.FSharp.Control
open FSharpx.Functional

// * Git

module Git =

  // ** tag

  let private tag (str: string) = sprintf "GitServer.%s" str

  // ** Listener

  type private Listener = IObservable<GitEvent>

  // ** Subscriptions

  type private Subscriptions = Subscriptions<GitEvent>

  // ** createListener

  let private createListener (subscriptions: Subscriptions) =
    let guid = Guid.NewGuid()
    { new Listener with
        member self.Subscribe(obs) =
          subscriptions.TryAdd(guid,obs) |> ignore

          { new IDisposable with
              member self.Dispose () =
                lock subscriptions <| fun _ ->
                  subscriptions.TryRemove(guid)
                  |> ignore } }

  // IMPORTANT CONFIG
  // git config --local receive.denyCurrentBranch updateInstead

  // ** Service

  type private Service =
    | UploadPack
    | ReceivePack

    // *** Parse

    static member Parse = function
      | "upload-pack"
      | "git-upload-pack"  -> UploadPack
      | "receive-pack"
      | "git-receive-pack" -> ReceivePack
      | other -> failwithf "unrecognized service: %s" other

    // *** ToString

    override self.ToString() =
      match self with
      | UploadPack  -> "upload-pack"
      | ReceivePack -> "receive-pack"

  // ** (^^)

  let rec private (^^) (lst: (string * string option) list) name =
    match lst with
    | [] -> None
    | (hdk, v) :: _ when hdk = name -> v
    | _ :: rest -> rest ^^ name

  // ** getAdvertisement

  let private getAdvertisement path (srvc: Service) =
    use proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- (string srvc) + " --stateless-rpc --advertise-refs " + (unwrap path)
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    if proc.Start() then
      let lines = ResizeArray()
      while not proc.StandardOutput.EndOfStream do
        proc.StandardOutput.ReadLine()
        |> lines.Add
      proc.WaitForExit()
      lines.ToArray()
      |> String.join "\n"
    else
      proc.WaitForExit()
      proc.StandardError.ReadToEnd()
      |> failwithf "Error: %s"

  // ** postData

  let private postData path subcriptions srvc (data: byte array) =
    use proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- (string srvc) + " --stateless-rpc " + (unwrap path)
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardInput <- true
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    if proc.Start() then
      // We want to write the bytes unparsed to the processes stdin, so we need to wire up a
      // BinaryWriter to the underlying Stream and write to that.
      use bw = new BinaryWriter(proc.StandardInput.BaseStream)

      bw.Write data
      bw.Flush()
      bw.Close()

      proc.WaitForExit()
      if proc.ExitCode = 0 then
        let mutable bytes = ResizeArray()
        use reader = new BinaryReader(proc.StandardOutput.BaseStream)
        let mutable run = true
        while run do
          try
            reader.ReadByte() |> bytes.Add
          with
            | :? EndOfStreamException -> run <- false
        bytes.ToArray()
      else
        let lines = ResizeArray()
        while not proc.StandardError.EndOfStream do
          proc.StandardError.ReadLine()
          |> lines.Add
        lines.ToArray()
        |> String.join "\n"
        |> failwithf "ERROR: %s"
    else
      proc.WaitForExit()
      proc.StandardError.ReadToEnd()
      |> failwithf "Error: %s"

  // ** makePacketHeader

  let private makePacketHeader (cmd: string) =
    let hexchars = "0123456789abcdef"
    let length = cmd.Length + 4      // 4 hex digits

    let toHex (idx: int) = hexchars.Chars(idx &&& 15)

    let builder = StringBuilder()
    [| toHex (length >>> 12)
       toHex (length >>> 8)
       toHex (length >>> 4)
       toHex  length |]
    |> Array.iter (builder.Append >> ignore)
    string builder

  // ** makePacket

  let private makePacket (cmd: Service) =
    let packet =
      cmd
      |> string
      |> String.format "# service=git-{0}\n"

    let header = makePacketHeader packet
    String.Format("{0}{1}0000", header, packet)

  // ** makeContentType

  let private makeContentType (noun: string) (cmd: Service) =
    cmd
    |> string
    |> String.format ("application/x-git-{0}-" + noun)

  // ** makeHttpHeaders

  let private makeHttpHeaders (contentType: string) =
    setHeader "Cache-Control" "no-cache, no-store, max-age=0, must-revalidate"
    >=> setHeader "If-You-Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "Fri, 01 Jan 1980 00:00:00 GMT"
    >=> setHeader "Content-Type" contentType

  // ** parseService

  let private parseService q = q ^^ "service" |> Option.map Service.Parse

  // ** getData

  let private getData path (cmd: Service) =
    let result = getAdvertisement path cmd

    let headers =
      cmd
      |> makeContentType "advertisement"
      |> makeHttpHeaders

    let body = StringBuilder()

    makePacket cmd |> body.Append |> ignore
    result |> body.Append |> ignore

    headers >=> OK (string body)

  // ** handleGetRequest

  let private handleGetRequest path (req: HttpRequest) =
    match req.query |> parseService with
    | Some cmd -> getData path cmd
    | None -> RequestErrors.FORBIDDEN "missing or malformed git service request"

  // ** handlePostRequest

  let private handlePostRequest path subscriptions (cmd: Service) (req: HttpRequest) =
    let result = postData path subscriptions cmd req.rawForm
    let headers =
      cmd
      |> makeContentType "result"
      |> makeHttpHeaders
    headers >=> ok result

  // ** uploadPack

  let private uploadPack path subscriptions =
    UploadPack
    |> handlePostRequest path subscriptions
    |> request

  // ** receivePack

  let private receivePack path subscriptions =
    ReceivePack
    |> handlePostRequest path subscriptions
    |> request

  // ** get

  let private get path = path |> unwrap |> handleGetRequest |> request

  // ** route

  let private route (name: Name) path =
    let unwrapped:string = unwrap name
    String.Format("/{0}{1}", unwrapped, path)

  // ** makeRoutes

  let private makeRoutes subscriptions name path =
    choose [
        Filters.path (route name "/info/refs") >=> Filters.GET >=> get path
        Filters.POST >=>
          (choose [
            Filters.path (route name "/git-receive-pack") >=> receivePack path subscriptions
            Filters.path (route name "/git-upload-pack" ) >=> uploadPack  path subscriptions ])
        RequestErrors.NOT_FOUND "Requested resource could not be found."
      ]

  // ** makeConfig

  let private makeConfig (ip: IpAddress) port (cts: CancellationTokenSource) =
    let addr = ip.toIPAddress()
    { defaultConfig with
        cancellationToken = cts.Token
        bindings = [ HttpBinding.create HTTP addr port ]
        mimeTypesMap = defaultMimeTypesMap }

  // ** GitServer

  [<RequireQualifiedAccess>]
  module GitServer =

    // *** create

    let create (mem: RaftMember) (project: IrisProject) =
      let mutable status = ServiceStatus.Stopped
      let cts = new CancellationTokenSource()
      let subscriptions = Subscriptions()

      { new IGitServer with
          member self.Status
            with get () = status

          member self.Subscribe(callback: GitEvent -> unit) =
            let listener = createListener subscriptions
            { new IObserver<GitEvent> with
                member self.OnCompleted() = ()
                member self.OnError(error) = ()
                member self.OnNext(value) = callback value }
            |> listener.Subscribe

          member self.Start () = either {
              do! Network.ensureIpAddress mem.IpAddr
              do! Network.ensureAvailability mem.IpAddr mem.GitPort

              status <- ServiceStatus.Starting
              let config = makeConfig mem.IpAddr (unwrap mem.GitPort) cts

              project.Path
              |> makeRoutes subscriptions project.Name
              |> startWebServerAsync config
              |> (fun (_, server) -> Async.Start(server, cts.Token))

              Thread.Sleep(150)

              Observable.notify subscriptions GitEvent.Started
              status <- ServiceStatus.Running
           }

          member self.Dispose() =
            if not (Service.isDisposed status) then
              try
                cts.Cancel()
                cts.Dispose()
              finally
                status <- ServiceStatus.Disposed
        }
