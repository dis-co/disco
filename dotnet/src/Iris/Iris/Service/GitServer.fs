namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities
open Iris.Service.Interfaces

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management
open System.Collections.Concurrent
open System.Text.RegularExpressions
open Microsoft.FSharp.Control
open FSharpx.Functional
open Hopac
open Hopac.Infixes

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

  // ** tag

  let private tag (str: string) = sprintf "GitServer.%s" str

  // ** Listener

  type private Listener = IObservable<GitEvent>

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<Guid,IObserver<GitEvent>>

  // ** GitState

  [<NoComparison;NoEquality>]
  type private GitState =
    { Status        : ServiceStatus
      Process       : Process
      Pid           : int
      SubPid        : int
      BasePath      : FilePath
      Address       : IpAddress
      Port          : Port
      Starter       : AutoResetEvent
      Stopper       : AutoResetEvent
      Subscriptions : Subscriptions
      Disposables   : IDisposable seq }

    interface IDisposable with
      member self.Dispose() =
        for disposable in self.Disposables do
          dispose disposable
        try
          Process.kill self.Pid
        finally
          dispose self.Process
        self.Subscriptions.Clear()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Exit     of int                   // Event from Git, needs no reply
    | Log      of string                // Event from Git, needs no reply either
    | Notify   of GitEvent
    | Stop
    | Stopped
    | Started

    override self.ToString () =
      match self with
      | Stop   _ -> "Stop"
      | Stopped  -> "Stopped"
      | Started  -> "Started"
      | Exit   c -> sprintf "Exit: %d" c
      | Log str  -> sprintf "Log: %s" str
      | Notify e -> sprintf "Notify: %A" e

  // ** GitAgent

  type private GitAgent = MailboxProcessor<Msg>

  // ** createProcess

  let private createProcess (path: FilePath) (addr: IpAddress) (port: Port) =
    let basedir =
      path
      |> unwrap
      |> Path.GetDirectoryName
      |> String.replace '\\' '/'

    let sanepath =
      path
      |> unwrap
      |> String.replace '\\' '/'

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
    agent.Post(Msg.Exit proc.ExitCode)

  // ** logHandler

  let private logHandler (agent: GitAgent) (data: DataReceivedEventArgs) =
    match data.Data with
    | null -> ()
    | _ -> agent.Post(Msg.Log data.Data)

  // ** (|Ready|_|)

  let private (|Ready|_|) (input: string) =
    let m = Regex.Match(input, "\[(?<pid>[0-9]*)\] Ready to rumble")
    if m.Success then
      match Int32.TryParse(m.Groups.[1].Value) with
      | (true, pid) -> Some pid
      | _ -> None
    else
      None

  // ** (|Connection|_|)

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

  // ** (|Fatal|_|)

 // [3672] Could not bind to 127.0.0.1: No such file or directory
 // [3672] unable to allocate any listen sockets for host 127.0.0.1 on port 10001
 // fatal: unable to allocate any listen sockets on port 10001

  let private (|Fatal|_|) (input: string) =
    if input.Contains "fatal" then
      Some input
    else None

  // ** parseLog

  let private parseLog (line: string) =
    match line with
    | Ready pid ->
      Started pid
      |> Either.succeed

    | Connection (pid, ip, prt) ->
      Pull(pid, ip, port prt)
      |> Either.succeed

    | Fatal reason ->
      Failed reason
      |> Either.succeed

    | _ -> Either.fail IrisError.OK      // we don't care about the rest

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

  // ** handleLog

  let private handleLog (state: GitState) (msg: string) (agent: GitAgent) =
    match parseLog msg with
    | Right msg ->
      msg |> Msg.Notify |> agent.Post
      // handle
      match msg with
      | Started pid ->
        Msg.Started |> agent.Post
        { state with
            Status = ServiceStatus.Running
            Pid = state.Process.Id
            SubPid = pid }
      | Failed reason ->
        Msg.Started |> agent.Post
        let error = Error.asGitError (tag "handleLog") reason
        error |> string |> Logger.err (tag "handleLog")
        { state with Status = ServiceStatus.Failed error }
      | _ -> state
    | _ -> state

  // ** handleExit

  let private handleExit (state: GitState) (code: int) (agent: GitAgent) =
    code |> Exited |> Msg.Notify |> agent.Post
    match code with
    | 0 | 143 ->                         // 143 is 128 (process was killed) + 15 (with SIGTERM)
      Msg.Stopped |> agent.Post
      { state with Status = ServiceStatus.Stopped }
    | _ ->
      let error =
        sprintf "Non-zero exit code: %d" code
        |> Error.asGitError (tag "handleExit")
      error |> string |> Logger.err (tag "handleExit")
      Msg.Stopped |> agent.Post
      { state with Status = ServiceStatus.Failed error }

  // ** handleStop

  let private handleStop (state: GitState) =
    Process.kill state.Pid
    { state with Status = ServiceStatus.Stopping }

  // ** handleStopped

  let private handleStopped (state: GitState) =
    state.Stopper.Set() |> ignore
    state

  // ** handleStarted

  let private handleStarted (state: GitState) =
    state.Starter.Set() |> ignore
    state

  // ** handleNotify

  let private handleNotify (state: GitState) ev =
    let subscriptions = state.Subscriptions.ToArray()
    // notify
    for KeyValue(_,subscription) in subscriptions do
      try subscription.OnNext ev
      with
        | exn ->
          exn.Message
          |> sprintf "could not notify subscriber of event: %O"
          |> Logger.err (tag "notify")
    state

  // ** loop

  let private loop (store: IAgentStore<GitState>) (inbox: GitAgent) =
    let rec act () =
      async {
        let! msg = inbox.Receive()
        let state = store.State
        let newstate =
          match msg with
          | Msg.Stop      -> handleStop    state
          | Msg.Stopped   -> handleStopped state
          | Msg.Started   -> handleStarted state
          | Msg.Exit code -> handleExit    state code inbox
          | Msg.Log msg   -> handleLog     state msg inbox
          | Msg.Notify ev -> handleNotify  state ev
        store.Update newstate
        return! act ()
      }
    act ()

  // ** GitServer

  [<RequireQualifiedAccess>]
  module GitServer =

    // *** create

    let create (mem: RaftMember) (path: FilePath) =
      let cts = new CancellationTokenSource()
      let store = AgentStore.create()
      let agent = new GitAgent(loop store, cts.Token)
      agent.Error.Add (sprintf "error on GitServer loop: %O" >> Logger.err (tag "loop"))

      let proc = createProcess path mem.IpAddr mem.GitPort

      let stdoutReader =
        Observable.subscribe (logHandler agent) proc.OutputDataReceived

      let stderrReader =
        Observable.subscribe (logHandler agent) proc.ErrorDataReceived

      let onExitEvent =
        Observable.subscribe (exitHandler proc agent) proc.Exited

      let state = {
          Status        = ServiceStatus.Stopped
          Process       = proc
          Pid           = -1
          SubPid        = -1
          BasePath      = path
          Address       = mem.IpAddr
          Port          = mem.GitPort
          Subscriptions = new Subscriptions()
          Starter       = new AutoResetEvent(false)
          Stopper       = new AutoResetEvent(false)
          Disposables   = [ stdoutReader
                            stderrReader
                            onExitEvent ] }

      store.Update state
      agent.Start()

      { new IGitServer with
          member self.Status
            with get () = store.State.Status

          member self.Pid
            with get () = store.State.Pid

          member self.Subscribe(callback: GitEvent -> unit) =
            let listener = createListener store.State.Subscriptions
            { new IObserver<GitEvent> with
                member self.OnCompleted() = ()
                member self.OnError(error) = ()
                member self.OnNext(value) = callback value }
            |> listener.Subscribe

          member self.Start () = either {
              try
                if proc.Start() then
                  proc.BeginOutputReadLine()
                  proc.BeginErrorReadLine()
                  let started = store.State.Starter.WaitOne(TimeSpan.FromMilliseconds 1000.0)

                  do! if not started then
                        dispose self
                        "Starting of GitServer failed (timeout)"
                        |> Error.asGitError (tag "Start")
                        |> Either.fail
                      else Either.succeed ()

                  match store.State.Status with
                  | ServiceStatus.Failed error ->
                    error |> string |> Logger.err (tag "Start")
                    dispose self
                    return! Either.fail error
                  | _ -> ()
                else
                  dispose self
                  return!
                    proc.ExitCode
                    |> sprintf "Could not start git daemon process: %d"
                    |> Error.asGitError (tag "Start")
                    |> Either.fail
              with
                | exn ->
                  dispose self
                  return!
                    exn.Message
                    |> sprintf "Exception starting git daemon process %s"
                    |> Error.asGitError (tag "Start")
                    |> Either.fail
            }

          member self.Dispose() =
            if not (Service.isDisposed store.State.Status) then
              agent.Post Msg.Stop
              let stopped = store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)
              if not stopped then
                "Timeout disposing GitServer"
                |> Logger.err (tag "Dispose")
              dispose store.State
              cts.Cancel()
              dispose agent
              store.Update { state with Status = ServiceStatus.Disposed }
        }
