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
    | Pull of address:string * port:uint16
    | Started
    | Exited of int

  // ** IGitServer

  type IGitServer =
    inherit IDisposable

    abstract Status : ServiceStatus
    abstract Subscribe : (GitEvent -> unit) -> IDisposable
    abstract Start: unit -> Either<IrisError,unit>

  // ** Msg
  type private Msg =
    | Status
    | Exit   of int
    | Log    of string

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<GitEvent>>

  // ** GitAgent

  type private GitAgent = MailboxProcessor<Msg>

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
    Exit proc.ExitCode
    |> agent.Post

  // ** logHandler

  let private logHandler (agent: GitAgent) (data: DataReceivedEventArgs) =
    Log data.Data
    |> agent.Post

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

        let agent = new GitAgent(fun inbox -> implement "me")

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
                  proc.BeginErrorReadLine()
                  proc.BeginErrorReadLine()
                  implement "while loop"
                else
                  implement "else"
              with
                | exn ->
                  implement "with"

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
