namespace Iris.Service

// * CommandLine
module CommandLine =

  // ** Imports
  open Argu
  open Iris.Core
  open Iris.Raft
  open Iris.Service.Persistence
  open Iris.Service.Iris
  open Iris.Service.Raft
  open System
  open System.IO
  open System.Linq
  open System.Text.RegularExpressions

  // ** Command Line Argument Parser

  //     _
  //    / \   _ __ __ _ ___
  //   / _ \ | '__/ _` / __|
  //  / ___ \| | | (_| \__ \
  // /_/   \_\_|  \__, |___/
  //              |___/

  type SubCommand =
    | Setup
    | Create
    | Start
    | Reset
    | Dump
    | User

  type CLIArguments =
    | [<Mandatory;MainCommand;CliPosition(CliPosition.First)>] Cmd of SubCommand

    | [<AltCommandLine("-i")>]        Interactive
    | [<AltCommandLine("--no-http")>] NoHttp

    | [<EqualsAssignment>] Bind         of string
    | [<EqualsAssignment>] Raft         of uint16
    | [<EqualsAssignment>] Web          of uint16
    | [<EqualsAssignment>] Git          of uint16
    | [<EqualsAssignment>] Ws           of uint16
    | [<EqualsAssignment>] Name         of string
    | [<EqualsAssignment>] Dir          of string

    interface IArgParserTemplate with
      member self.Usage =
        match self with
          | Interactive -> "Start daemon in interactive mode"
          | NoHttp      -> "Do not start http server (default: http will be started)"
          | Dir     _   -> "Project directory to place the config & database in"
          | Name    _   -> "Project name when using <create>"
          | Bind    _   -> "Specify a valid IP address."
          | Web     _   -> "Http server port."
          | Git     _   -> "Git server port."
          | Ws      _   -> "WebSocket port."
          | Raft    _   -> "Raft server port (internal)."
          | Cmd     _   -> "Either one of setup, create, start, reset, user or dump"

  let parser = ArgumentParser.Create<CLIArguments>()

  // ** validateOptions

  let validateOptions (opts: ParseResults<CLIArguments>) =
    let ensureDir result =
      if opts.Contains <@ Dir @> |> not then
        Error.exitWith MissingStartupDir
      result

    let valid =
      match opts.GetResult <@ Cmd @> with
      | Create -> true
      | Setup  -> true
      | Start  -> ensureDir true
      | Reset  -> ensureDir true
      | Dump   -> ensureDir true
      | User   -> ensureDir true

    if opts.GetResult <@ Cmd @> = Create then
      let name = opts.Contains <@ Name @>
      let dir  = opts.Contains <@ Dir @>
      let bind = opts.Contains <@ Bind @>
      let web  = opts.Contains <@ Web @>
      let raft = opts.Contains <@ Raft @>
      let git  = opts.Contains <@ Git @>
      let ws   = opts.Contains <@ Ws @>

      if not (name && bind && web && raft && ws) then
        printfn "Error: when creating a new configuration you must specify the following options:"
        if not name then printfn "    --name=<name>"
        if not dir  then printfn "    --dir=<directory>"
        if not bind then printfn "    --bind=<binding address>"
        if not web  then printfn "    --web=<web interface port>"
        if not git  then printfn "    --git=<git server port>"
        if not raft then printfn "    --raft=<raft port>"
        if not ws   then printfn "    --ws=<ws port>"
        Error.exitWith CliParseError

  // ** Utilities

  let private withTrim (token: string) (str: string) =
    let trimmed = String.trim str
    if trimmed.StartsWith(token) then
      let substr = trimmed.Substring(token.Length)
      Some <| String.trim substr
    else None

  let private withEmpty (token: string) (str: string) =
    if String.trim str = token
    then Some ()
    else None

  let private handleError (error: IrisError) =
    printfn "Encountered error during ForceTimeout operation: %A" error

  let parseHostString (str: string) =
    let trimmed = str.Trim().Split(' ')
    match trimmed with
      | [| id; hostname; hostspec |] as arr ->
        if hostspec.StartsWith("tcp://") then
          match hostspec.Substring(6).Split(':') with
            | [| addr; port |] -> Some (uint32 id, hostname, addr, int port)
            | _ -> None
        else None
      | _ -> None

  // ** tryAppendEntry

  let tryAppendEntry (ctx: IIrisServer) str =
    warn "CLI AppendEntry currently not supported"

  // ** timeoutRaft

  let tryForceElection (ctx: IIrisServer) =
    either {
      do! ctx.ForceElection()
    }
    |> Either.mapError handleError
    |> ignore

  // ** Command Parsers

  let (|Exit|_|)     str = withEmpty "exit" str
  let (|Quit|_|)     str = withEmpty "quit" str
  let (|Status|_|)   str = withEmpty "status" str
  let (|Periodic|_|) str = withEmpty "step" str
  let (|Timeout|_|)  str = withEmpty "timeout" str
  let (|Leave|_|)    str = withEmpty "leave" str

  let (|Append|_|)  str = withTrim "append" str
  let (|Join|_|)    str = withTrim "join" str
  let (|AddNode|_|) str = withTrim "addnode" str
  let (|RmNode|_|)  str = withTrim "rmnode" str

  let (|Interval|_|) (str: string) =
    let trimmed = str.Trim()
    match trimmed.Split(' ') with
    | [| "interval"; x |] ->
      try
        uint8 x |> Some
      with
        | _ -> None
    | _ -> None

  let (|LogLevel|_|) (str: string) =
    let parsed = str.Trim().Split(' ')
    match parsed with
      | [| "log"; "debug" |] -> Some "debug"
      | [| "log"; "info" |]  -> Some "info"
      | [| "log"; "warn" |]  -> Some "warn"
      | [| "log"; "err" |]   -> Some "err"
      | _                  -> None

  let (|JoinParams|_|) (str: string) =
    let pattern = "(?<ip>[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}):(?<port>[0-9]{1,5})"
    let m = Regex.Match(str, pattern)
    if m.Success then
      match IpAddress.TryParse m.Groups.[1].Value, UInt16.TryParse m.Groups.[2].Value with
      | Right ip, (true, port) -> Some (ip, port)
      | _ -> None
    else None

  let (|AddNodeParams|_|) (str: string) =
    let pattern =
      [| "id:(?<id>.*)"
      ; "hn:(?<hn>.*)"
      ; "ip:(?<ip>[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})"
      ; "port:(?<port>[0-9]{1,5})"
      ; "web:(?<web>[0-9]{1,5})"
      ; "ws:(?<ws>[0-9]{1,5})"
      ; "git:(?<git>[0-9]{1,5})" |]
      |> String.join " "
    let m = Regex.Match(str, pattern)
    if m.Success then
      let id = Id m.Groups.[1].Value
      let hn =  m.Groups.[2].Value
      let ip = IpAddress.TryParse m.Groups.[3].Value
      let port = UInt16.TryParse m.Groups.[4].Value
      let web = UInt16.TryParse m.Groups.[5].Value
      let ws = UInt16.TryParse m.Groups.[6].Value
      let git = UInt16.TryParse m.Groups.[7].Value
      match ip, port, web, ws, git with
      | Right ip, (true,port), (true,web), (true,ws), (true,git) ->
        { Node.create id with
            HostName = hn
            IpAddr   = ip
            Port     = port
            WebPort  = web
            WsPort   = ws
            GitPort  = git }
        |> Some
      | _ -> None
    else None

  // ** trySetLogLevel

  let trySetLogLevel (context: IIrisServer) (str: string) =
    either {
      let! config = context.Config
      let updated =
        { config.RaftConfig with
            LogLevel = LogLevel.Parse str }
      do! context.SetConfig (Config.updateEngine updated config)
    }
    |> Either.mapError handleError
    |> ignore

  // ** trySetInterval

  let trySetInterval (context: IIrisServer) i =
    either {
      let! config = context.Config
      let updated =
        { config.RaftConfig with PeriodicInterval = i }
      do! context.SetConfig (Config.updateEngine updated config)
    }
    |> Either.mapError handleError
    |> ignore

  // ** tryJoinCluster

  let tryJoinCluster (context: IIrisServer) (hst: string) =
    match hst with
      | JoinParams (ip, port) ->
        either {
          do! context.JoinCluster ip port
        }
        |> Either.mapError handleError
        |> ignore
      | _ ->
        sprintf "parameters %A could not be parsed" hst
        |> Other
        |> handleError

  // ** tryLeaveCluster

  let tryLeaveCluster (context: IIrisServer) =
    either {
      do! context.LeaveCluster()
    }
    |> Either.mapError handleError
    |> ignore

  // ** tryAddNode

  let tryAddNode (context: IIrisServer) (hst: string) =
    match hst with
      | AddNodeParams node ->
        either {
          let! appended = context.AddNode node
          printfn "Added node: %A in entry %A" id (string appended.Id)
          return ()
        }
        |> Either.mapError handleError
        |> ignore
      | _ ->
        sprintf "parameters %A could not be parsed" hst
        |> Other
        |> handleError

  // ** tryRmNode

  let tryRmNode (context: IIrisServer) (hst: string) =
    either {
      let! appended = context.RmNode (Id (String.trim hst))
      printfn "Removed node: %A in entry %A" id (string appended.Id)
      return ()
    }
    |> Either.mapError handleError
    |> ignore

  // ** tryPeriodic

  let tryPeriodic (context: IIrisServer) =
    either {
      do! context.Periodic()
    }
    |> Either.mapError handleError
    |> ignore

  // ** tryGetStatus

  let tryGetStatus (context: IIrisServer) =
    either {
      let! status = context.Status
      printfn "IrisService Status: %A" status
      return ()
    }
    |> Either.mapError handleError
    |> ignore

  // ** consoleLoop

  //  _
  // | |    ___   ___  _ __
  // | |   / _ \ / _ \| '_ \
  // | |__| (_) | (_) | |_) |
  // |_____\___/ \___/| .__/ s
  //                  |_|

  let registerExitHandlers (context: IIrisServer) =
    Console.CancelKeyPress.Add (fun _ ->
      printfn "Disposing context..."
      dispose context
      exit 0)
    System.AppDomain.CurrentDomain.ProcessExit.Add (fun _ -> dispose context)
    System.AppDomain.CurrentDomain.DomainUnload.Add (fun _ -> dispose context)

  let interactiveLoop (context: IIrisServer) : unit =
    printfn "Welcome to the Raft REPL. Type help to see all commands."
    let kont = ref true
    let rec proc kontinue =
      printf "λ "
      let input = Console.ReadLine()
      match input with
        | LogLevel opt -> trySetLogLevel   context opt
        | Interval   i -> trySetInterval   context i
        | Exit         -> dispose          context; kontinue := false
        | Periodic     -> tryPeriodic      context
        | Append ety   -> tryAppendEntry   context ety
        | Join hst     -> tryJoinCluster   context hst
        | Leave        -> tryLeaveCluster  context
        | AddNode hst  -> tryAddNode       context hst
        | RmNode hst   -> tryRmNode        context hst
        | Timeout      -> tryForceElection context
        | Status       -> tryGetStatus     context
        | _            -> printfn "unknown command"
      if !kontinue then
        proc kontinue
    proc kont

  let silentLoop () =
    let kont = ref true
    let rec proc kontinue =
      Console.ReadLine() |> ignore
      if !kontinue then
        proc kontinue
    proc kont

  // ** ensureMachineConfig

  let ensureMachineConfig () =
    if not (File.Exists MachineConfig.defaultPath) then
      MachineConfig.create ()
      |> MachineConfig.save None
      |> Error.orExit id

  // ** buildNode

  //  _   _           _
  // | \ | | ___   __| | ___
  // |  \| |/ _ \ / _` |/ _ \
  // | |\  | (_) | (_| |  __/
  // |_| \_|\___/ \__,_|\___|

  let buildNode (parsed: ParseResults<CLIArguments>) (id: Id) =
    { Node.create(id) with
        IpAddr  = parsed.GetResult <@ Bind @> |> IpAddress.Parse
        GitPort = parsed.GetResult <@ Git  @>
        WsPort  = parsed.GetResult <@ Ws   @>
        WebPort = parsed.GetResult <@ Web  @>
        Port    = parsed.GetResult <@ Raft @> }

  // ** startService

  //  ____  _             _
  // / ___|| |_ __ _ _ __| |_
  // \___ \| __/ _` | '__| __|
  //  ___) | || (_| | |  | |_
  // |____/ \__\__,_|_|   \__|

  let startService (web: bool) (interactive: bool) (projectdir: FilePath) : Either<IrisError, unit> =
    ensureMachineConfig ()

    let projFile = Path.GetFullPath(projectdir) </> PROJECT_FILENAME + ASSET_EXTENSION

    if File.Exists projFile |> not then
      projectdir
      |> ProjectNotFound
      |> Either.fail
    else
      either {
        let! machine = MachineConfig.load None
        #if FRONTEND_DEV
        let machine = { machine with MachineId = Id "TEST_MACHINE" }
        #endif
        let! server = IrisService.create machine web
        use obs = Logger.subscribe Logger.stdout

        registerExitHandlers server

        do! server.Load projFile

        let result =
          if interactive then
            interactiveLoop server
          else
            silentLoop ()

        dispose server

        return result
      }

  // ** createProject

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  /// ## buildProject
  ///
  /// Create a new IrisProject data structure with given parameters.
  ///
  /// ### Signature:
  /// - name: Name of the Project
  /// - path: destination path of the Project
  /// - raftDir: Raft data directory
  /// - node: self Node (built from Node Id env var)
  ///
  /// Returns: IrisProject
  let buildProject (machine: IrisMachine) (name: string) (path: FilePath) (raftDir: FilePath) (node: RaftNode) =
    Project.create name machine
    |> Project.updatePath path
    |> Project.updateDataDir raftDir
    |> Project.addMember node

  /// ## initializeRaft
  ///
  /// Given the user (usually the admin user) and Project value, initialize the Raft intermediate
  /// state in the data directory and commit the result to git.
  ///
  /// ### Signature:
  /// - user: User to commit as
  /// - project: IrisProject to initialize
  ///
  /// Returns: unit
  let initializeRaft (user: User) (project: IrisProject) = either {
      let! raft = createRaft project.Config
      let! result = saveRaft project.Config raft
      let! (commit, saved) = Project.save user.Signature "project created" project

      project.Path
      |> printfn "project initialized in %A and committed @ %s" commit.Sha
    }

  /// ## createProject
  ///
  /// Create a new project given the passed command line options.
  ///
  /// ### Signature:
  /// - parsed: ParseResult<CLIArguments>
  ///
  /// Returns: unit
  let createProject (parsed: ParseResults<CLIArguments>) = either {
      ensureMachineConfig ()

      let! machine = MachineConfig.load None

      let me = User.Admin
      let baseDir = parsed.GetResult <@ Dir @>
      let name = parsed.GetResult <@ Name @>
      let dir = baseDir </> name
      let raftDir = Path.GetFullPath(dir) </> RAFT_DIRECTORY

      do! match Directory.Exists dir with
          | true  ->
            match Directory.EnumerateFileSystemEntries(dir).Count() = 0 with
            | true  -> Either.nothing
            | false ->
              printf "%A not empty. I clean first? y/n" dir
              let input = Console.ReadKey()
              match input.Key with
                | ConsoleKey.Y -> rmDir dir
                | _            -> OK |> Either.fail
          | false -> Either.nothing

      do! mkDir dir
      do! mkDir raftDir

      let node = buildNode parsed machine.MachineId
      let project = buildProject machine dir name raftDir node

      do! initializeRaft me project
    }

  // ** resetProject

  //  ____                _
  // |  _ \ ___  ___  ___| |_
  // | |_) / _ \/ __|/ _ \ __|
  // |  _ <  __/\__ \  __/ |_
  // |_| \_\___||___/\___|\__|

  /// ## resetProject
  ///
  /// Reset a Project at given path to initial state.
  ///
  /// ### Signature:
  /// - datadir: FilePath to Project directory
  ///
  /// Returns: unit
  let resetProject (datadir: FilePath) = either {
      let path = datadir </> PROJECT_FILENAME + ASSET_EXTENSION
      let raftDir = datadir </> RAFT_DIRECTORY

      let! machine = MachineConfig.load None
      let! project = Project.load path machine

      do! match Directory.Exists raftDir with
          | true  -> rmDir raftDir
          | false -> Either.nothing

      do! mkDir raftDir

      let! raft = createRaft project.Config
      let! result = saveRaft project.Config raft
      return ()
    }

  // ** dumpDataDir

  //  ____
  // |  _ \ _   _ _ __ ___  _ __
  // | | | | | | | '_ ` _ \| '_ \
  // | |_| | |_| | | | | | | |_) |
  // |____/ \__,_|_| |_| |_| .__/
  //                       |_|

  let dumpDataDir (datadir: FilePath) =
    implement "dumpDataDir"


  let setup (location: FilePath option) =
    let create (path: FilePath) =
      try
        if File.Exists path then
          printfn "Machine configuration already present. Contents:"
          File.ReadAllText path
          |> String.indent 4
          |> printfn "%s"
          printf "Should I overwrite this? (y/n) "
          let key = Console.ReadKey()
          printfn "\nAnswer: %c" key.KeyChar
          match key.KeyChar with
          | 'y' | 'Y' ->
            MachineConfig.create ()
            |> MachineConfig.save (Some path)
            |> Error.orExit id
            printfn "Created Machine new config in %A" path
            |> Either.succeed
          | _ ->
            printfn "Not making any changes. Bye."
            |> Either.succeed
        else
          MachineConfig.create ()
          |> MachineConfig.save (Some path)
          |> Error.orExit id
          printfn "Created Machine new config in %A:" path
          |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Other
          |> Either.fail

    match location with
    | Some path ->
      path </> MACHINECONFIG_NAME + ASSET_EXTENSION
      |> create
    | None ->
      create MachineConfig.defaultPath
