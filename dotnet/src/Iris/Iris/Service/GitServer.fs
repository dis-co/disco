namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management
open System.Text.RegularExpressions
open Microsoft.FSharp.Control
open FSharpx.Functional

// * GitServer

//   ____ _ _   ____
//  / ___(_) |_/ ___|  ___ _ ____   _____ _ __
// | |  _| | __\___ \ / _ \ '__\ \ / / _ \ '__|
// | |_| | | |_ ___) |  __/ |   \ V /  __/ |
//  \____|_|\__|____/ \___|_|    \_/ \___|_|

// = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =
//
// EXAMPLE CLONES
//
// [10422] Ready to rumble
// [11331] Connection from 127.0.0.1:43218
// [11331] Extended attributes (21 bytes) exist <host=localhost:6000>
// [11331] Request upload-pack for '/gittest/.git'
// [10422] [11331] Disconnected
// [11921] Connection from 127.0.0.1:43224
// [11921] Extended attributes (21 bytes) exist <host=localhost:6000>
// [11921] Request upload-pack for '/gittest/.git'
// [10422] [11921] Disconnected
//
// = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =

module Git =

  [<Literal>]
  let private tag = "GitServer"

  // ** GitEvent

  type GitEvent =
    | Started of pid:int
    | Exited  of code:int
    | Pull    of pid:int * address:string * port:uint16

  // ** IGitServer

  type IGitServer =
    inherit IDisposable

    abstract Status : ServiceStatus
    abstract Subscribe : (GitEvent -> unit) -> IDisposable
    abstract Start: unit -> Either<IrisError,unit>

  // ** GitState

  type private GitState =
    { Status : ServiceStatus
      Pid    : int
      SubPid : int }

  // ** Msg

  [<RequireQualifiedAccess>]
  type private Msg =
    | Status
    | Pid    of int
    | Exit   of int
    | Log    of string

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | Ok
    | Status of ServiceStatus

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<GitEvent>>

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Reply>

  // ** Message

  type private Message = Msg * ReplyChan

  // ** GitAgent

  type private GitAgent = MailboxProcessor<Message>

  // ** createProcess

  let private createProcess (id: Id) (path: FilePath) (addr: string) (port: uint16) =
    let basedir =
      Path.GetDirectoryName path
      |> String.replace '\\' '/'

    let sanepath = String.replace '\\' '/' path

    sprintf "starting on %s:%d in base path: %A with dir: %A"
      (string addr)
      port
      basedir
      path
    |> Logger.debug id tag

    let args =
      [| "daemon"
      ; "--verbose"
      ; "--strict-paths"
      ; (sprintf "--base-path=%s" basedir)
      ; (if Platform.isUnix then "--reuseaddr" else "")
      ; (addr |> string |> sprintf "--listen=%s")
      ; (sprintf "--port=%d" port)
      ; (sprintf "%s/.git" sanepath) |]
      |> String.join " "

    let proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- args
    proc.EnableRaisingEvents <- true
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    proc

  // ** exitHandler

  /// ## exitHandler
  ///
  /// Handler to monitor the sub-processes. If the `git daemon` process exits for whichever reason
  /// (it might not have been able to start because of erroneous port settings for instance), then
  /// this handler detects this, creates an error from this and ensures control is passed back to
  /// the caller of `Start()`. It also ensures that all resources on the thread will get cleaned up
  /// properly.
  ///
  /// ### Signature:
  /// - proc: Process to monitor (mostly to get ExitCode from)
  ///
  /// Returns: unit
  let private exitHandler (proc: Process) (agent: GitAgent) _ =
    agent.PostAndReply(fun chan -> Msg.Exit proc.ExitCode, chan)
    |> ignore

  // ** logHandler

  let private logHandler (agent: GitAgent) (data: DataReceivedEventArgs) =
    agent.PostAndReply(fun chan -> Msg.Log data.Data, chan)
    |> ignore

  let private (|Ready|_|) (input: string) =
    let m = Regex.Match(input, "\[(?<pid>[0-9]*)\] Ready to rumble")
    if m.Success then
      match Int32.TryParse(m.Groups.[1].Value) with
      | (true, pid) -> Some pid
      | _ -> None
    else
      None

  let private (|Connection|_|) (input: string) =
    let pattern = "\[(?<pid>[0-9]*)\] Connection from (?<ip>[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})\:(?<port>[0-9]*)"
    let m = Regex.Match(input, pattern)
    if m.Success then
      match Int32.TryParse(m.Groups.[1].Value), UInt16.TryParse(m.Groups.[3].Value) with
      | (true,pid), (true,port) ->
        Some(pid, m.Groups.[2].Value, port)
      | _ -> None
    else
      None

  let private parseLog (line: string) =
    match line with
    | Ready pid ->
      Started pid
      |> Either.succeed

    | Connection (pid, ip, port) ->
      Pull(pid, ip, port)
      |> Either.succeed

    | _ ->
      sprintf "Line not relevant or recognized: %A" line
      |> ParseError
      |> Either.fail

  // ** loop

  let private loop (id: Id) (subscriptions: Subscriptions) (inbox: GitAgent) =
    let initial =
      { Status = ServiceStatus.Starting
        Pid = -1
        SubPid = -1 }

    let rec act (state: GitState) =
      async {
        let! (msg, chan) = inbox.Receive()
        let newstate =
          match msg with
          | Msg.Pid pid ->
            { state with Pid = pid }

          | Msg.Status ->
            state.Status
            |> Reply.Status
            |> chan.Reply
            state

          | Msg.Exit code when code = 0 ->
            Reply.Ok
            |> chan.Reply
            { state with Status = ServiceStatus.Stopped }

          | Msg.Exit code ->
            Reply.Ok
            |> chan.Reply
            let error =
              sprintf "Non-zero exit code: %d" code
              |> IrisError.GitError
            { state with Status = ServiceStatus.Failed error }

          | Msg.Log msg ->
            match parseLog msg with
            | Right msg ->
              for subscription in subscriptions do
                subscription.OnNext msg
              match msg with
              | Started pid ->
                { state with SubPid = pid }
              | _ -> state
            | Left error ->
              error
              |> string
              |> Logger.err id tag
              state
        return! act newstate
      }
    act initial

  // ** starting

  let private starting (agent: GitAgent) =
    match agent.PostAndReply(fun chan -> Msg.Status,chan) with
    | Reply.Status status when status = ServiceStatus.Starting -> true
    | _ -> false

  // ** started

  let private running (agent: GitAgent) =
    match agent.PostAndReply(fun chan -> Msg.Status,chan) with
    | Reply.Status status when status = ServiceStatus.Running -> true
    | _ -> false

  // ** GitServer

  [<RequireQualifiedAccess>]
  module GitServer =

    let start (node: RaftNode) (project: IrisProject) =
      match project.Path with
      | Some path ->
        let stdoutToken = new CancellationTokenSource()
        let stderrToken = new CancellationTokenSource()
        let subscriptions = new Subscriptions()

        let listener =
          { new IObservable<GitEvent> with
              member self.Subscribe(obs) =
                lock subscriptions <| fun _ ->
                  subscriptions.Add obs

                { new IDisposable with
                    member self.Dispose () =
                      lock subscriptions <| fun _ ->
                        subscriptions.Remove obs
                        |> ignore } }

        let proc = createProcess node.Id path (string node.IpAddr) node.GitPort

        let agent = new GitAgent(loop node.Id subscriptions)

        let stdoutReader =
          Observable.subscribe (logHandler agent) proc.OutputDataReceived

        let stderrReader =
          Observable.subscribe (logHandler agent) proc.ErrorDataReceived

        let onExitEvent =
          Observable.subscribe (exitHandler proc agent) proc.Exited

        { new IGitServer with
            member self.Status
              with get () = implement "Status"

            member self.Subscribe(callback: GitEvent -> unit) =
              { new IObserver<GitEvent> with
                  member self.OnCompleted() = ()
                  member self.OnError(error) = ()
                  member self.OnNext(value) = callback value }
              |> listener.Subscribe

            member self.Start () =
              try
                if proc.Start() then
                  agent.PostAndReply(fun chan -> Msg.Pid proc.Id, chan)
                  |> ignore
                  proc.BeginErrorReadLine()
                  proc.BeginErrorReadLine()
                  let mutable n = 0
                  while starting agent && n < 1000 do
                    n <- n + 10
                    Thread.Sleep 10
                  if running agent then
                    Right ()
                  else
                    match agent.PostAndReply(fun chan -> Msg.Status,chan) with
                    | Reply.Status status ->
                      status
                      |> string
                      |> GitError
                      |> Either.fail
                    | other ->
                      other
                      |> string
                      |> GitError
                      |> Either.fail
                else
                  "Could not start git daemon process"
                  |> GitError
                  |> Either.fail
              with
                | exn ->
                  exn.Message
                  |> "Exception starting git daemon process %s"
                  |> GitError
                  |> Either.fail

            member self.Dispose() =
              dispose stdoutReader
              dispose stderrReader
              dispose onExitEvent
              try
                Process.kill proc.Id
              finally
                dispose proc
          }
        |> Either.succeed

      | None ->
        ProjectPathError
        |> Either.fail
