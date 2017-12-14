namespace Disco.Service

#if !DISCO_NODES

// * CommandLine

module CommandLine =

  let private logtag (str: string) = sprintf "CommandLine.%s" str

  // ** Imports

  open Argu
  open Disco.Core
  open Disco.Raft
  open Disco.Service.Persistence
  open Disco.Service.Disco
  open Disco.Service.CommandActions
  open Disco.Service.Interfaces
  open System
  open System.Linq
  open System.Threading
  open System.Collections.Generic
  open System.Text.RegularExpressions

  // ** Command Line Argument Parser

  //     _
  //    / \   _ __ __ _ ___
  //   / _ \ | '__/ _` / __|
  //  / ___ \| | | (_| \__ \
  // /_/   \_\_|  \__, |___/
  //              |___/

  type SubCommand =
    | Help
    | Create
    | Start
    | Reset
    | Dump
    | Add_User
    | Add_Member

    static member Doc
      with get () =
        @"
 ____                                        _        _   _
|  _ \  ___   ___ _   _ _ __ ___   ___ _ __ | |_ __ _| |_(_) ___  _ __
| | | |/ _ \ / __| | | | '_ ` _ \ / _ \ '_ \| __/ _` | __| |/ _ \| '_ \
| |_| | (_) | (__| |_| | | | | | |  __/ | | | || (_| | |_| | (_) | | | |
|____/ \___/ \___|\__,_|_| |_| |_|\___|_| |_|\__\__,_|\__|_|\___/|_| |_|

----------------------------------------------------------------------
| create                                                             |
----------------------------------------------------------------------

  Create a new project in the machine workspace with the name specified by

  --project=project-name : Name of project directory in the workspace

  A folder will be created in the workspace with the project name.

  You must also specify all ports with their respective flags:

  --raft=<uint16> : Port of underlying Raft service
  --git=<uint16>  : Port of `git daemon` service
  --ws=<uint16>   : Port of WebSocket service

  Additionally, you also need to specify the address which all
  services should bind to, using

  --bind=192.168.2.x : Address to bind services to

  Beware that service discovery will not work on loopback interfaces!

----------------------------------------------------------------------
| setup                                                              |
----------------------------------------------------------------------

  Create a new Machine-level configuration file. This sets the current
  machine's global identifier and also specifies the workspace
  directory used by Disco to scan for projects.

  You can specify the parent directory to create configuration in by
  using the machine parameter:

  --machine=/path/to/machine/config : Path to the machine config file

----------------------------------------------------------------------
| start                                                              |
----------------------------------------------------------------------

  Start the Disco daemon with the project specified. You can specify
  the project to start with using

  --project=project-name : Name of project directory in the workspace

----------------------------------------------------------------------
| add-user                                                           |
----------------------------------------------------------------------

  Add a new user to the project. Requires you to specify the project
  name

  --project=project-name : Name of project directory in the workspace

----------------------------------------------------------------------
| add-member                                                         |
----------------------------------------------------------------------

  Add a new cluster member to the project. Requires you to specify the
  project name

  --project=project-name : Name of project directory in the workspace

----------------------------------------------------------------------
| reset                                                              |
----------------------------------------------------------------------

  Reset a project. This is an internal command and might disappear in
  the future.

----------------------------------------------------------------------
| dump                                                               |
----------------------------------------------------------------------

  Dump the current state of the project. Requires you to specify the
  project name

  --project=project-name : Name of project directory in the workspace

----------------------------------------------------------------------
| help                                                               |
----------------------------------------------------------------------

  Show this help message.
"

  type CLIArguments =
    | [<Mandatory;MainCommand;CliPosition(CliPosition.First)>] Cmd of SubCommand

    | [<EqualsAssignment>] Bind         of string
    | [<EqualsAssignment>] Api          of uint16
    | [<EqualsAssignment>] Raft         of uint16
    | [<EqualsAssignment>] Git          of uint16
    | [<EqualsAssignment>] Ws           of uint16
    | [<EqualsAssignment>] Project      of string
    | [<EqualsAssignment>] Machine      of string
    | [<EqualsAssignment>] Frontend     of string
    | [<EqualsAssignment>] Shift_Defaults of uint16

    interface IArgParserTemplate with
      member self.Usage =
        match self with
          | Project _   -> "Name of project directory in the workspace"
          | Machine _   -> "Path to the machine config file"
          | Frontend _  -> "Path to the frontend files"
          | Shift_Defaults _  -> "Shift the default ports and workspaces of machine configuration"
          | Bind    _   -> "Specify a valid IP address."
          | Git     _   -> "Git server port."
          | Ws      _   -> "WebSocket port."
          | Raft    _   -> "Raft server port."
          | Api     _   -> "Api server port."
          | Cmd     _   -> "Either one of setup, create, start, reset, user or dump."

  let parser = ArgumentParser.Create<CLIArguments>()

  // ** validateOptions

  let validateOptions (opts: ParseResults<CLIArguments>) =
    let ensureProject result =
      if opts.Contains <@ Project @> |> not then
        "Missing Startup-Project"
        |> Error.asOther (logtag "validateOptions")
        |> Error.exitWith
      result

    match opts.GetResult <@ Cmd @> with
    | Reset | Dump | Add_User | Add_Member -> ensureProject ()
    | _ -> ()

    if opts.GetResult <@ Cmd @> = Create then
      let project  = opts.Contains <@ Project @>
      let bind = opts.Contains <@ Bind @>
      let raft = opts.Contains <@ Raft @>
      let git  = opts.Contains <@ Git @>
      let ws   = opts.Contains <@ Ws @>

      if not (bind && raft && ws) then
        printfn "Error: when creating a new configuration you must specify the following options:"
        if not project then printfn "    --project=<project name>"
        if not bind    then printfn "    --bind=<binding address>"
        if not git     then printfn "    --git=<git server port>"
        if not raft    then printfn "    --raft=<raft port>"
        if not ws      then printfn "    --ws=<ws port>"
        "CLI options parse error"
        |> Error.asOther (logtag "validateOptions")
        |> Error.exitWith

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  // ** tryCreateProject

  let tryCreateProject force (parameters: IDictionary<string, string>) =
    let tryGet k map (dic: IDictionary<string, string>) =
      match dic.TryGetValue k with
      | true, v ->
        try map v |> Right
        with ex -> Other(logtag "tryCreateProject", ex.Message) |> Left
      | false, _ -> Other(logtag "tryCreateProject", sprintf "Missing %s parameter" k) |> Left

    either {
      let machine = MachineConfig.get()
      let! name = parameters |> tryGet "project" id
      let dir = machine.WorkSpace </> filepath name
      let raftDir = dir </> filepath RAFT_DIRECTORY

      do! match Directory.exists dir, force with
          | true, false  ->
            match Directory.fileSystemEntries(dir).Count() = 0 with
            | true  -> Either.nothing
            | false ->
              printf "%A not empty. I clean first? y/n" dir
              let input = Console.ReadKey()
              match input.Key with
                | ConsoleKey.Y -> rmDir dir
                | _            -> OK |> Either.fail
          | true, true ->
            rmDir dir
          | false, _ -> Either.nothing

      do! mkDir dir
      do! mkDir raftDir

      let! bind = parameters |> tryGet "bind" IpAddress.Parse
      let! git  = parameters |> tryGet "git" uint16
      let! ws   = parameters |> tryGet "ws" uint16
      let! raft = parameters |> tryGet "raft" uint16
      let! api = parameters  |> tryGet "api" uint16

      let mem =
        { Member.create(machine.MachineId) with
            IpAddress = bind
            GitPort   = port git
            WsPort    = port ws
            RaftPort  = port raft
            ApiPort   = port api }

      let! project = buildProject machine name dir raftDir mem

      do! initializeRaft project
    }

  // ** consoleLoop

  //  _
  // | |    ___   ___  _ __
  // | |   / _ \ / _ \| '_ \
  // | |__| (_) | (_) | |_) |
  // |_____\___/ \___/| .__/ s
  //                  |_|
  let registerExitHandlers (context: IDisco) =
    Console.CancelKeyPress.Add (fun _ ->
      printfn "Disposing context..."
      dispose context
      exit 0)
    System.AppDomain.CurrentDomain.ProcessExit.Add (fun _ -> dispose context)
    System.AppDomain.CurrentDomain.DomainUnload.Add (fun _ -> dispose context)

  // ** vmSetup

  let vmSetup () =
    Thread.CurrentThread.GetApartmentState()
    |> printfn "Using Threading Model: %A"

    let threadCount = System.Environment.ProcessorCount * 2
    ThreadPool.SetMinThreads(threadCount,threadCount)
    |> fun result ->
      printfn "Setting Min. Threads in ThreadPool To %d %s"
        threadCount
        (if result then "Successful" else "Unsuccessful")

  // ** startService

  //  ____  _             _
  // / ___|| |_ __ _ _ __| |_
  // \___ \| __/ _` | '__| __|
  //  ___) | || (_| | |  | |_
  // |____/ \__\__,_|_|   \__|

  let startService (projectDir: FilePath option) (frontend: FilePath option) =
    either {
      let agentRef = ref None
      let post = CommandActions.postCommand agentRef
      let machine = MachineConfig.get()

      let termSupportsColors = Console.isColorTerm()

      do Logger.initialize {
        MachineId = machine.MachineId
        Tier = Tier.Service
        UseColors = termSupportsColors
        Level = LogLevel.Debug
      }

      use _ = Logger.subscribe Logger.stdout

      let! discoService = Disco.create post {
        Machine = machine
        FrontendPath = frontend
        ProjectPath = projectDir
      }

      agentRef := CommandActions.startAgent machine discoService |> Some

      registerExitHandlers discoService

      do!
        match projectDir with
        | Some projectDir ->
          let name, site =
            name (unwrap projectDir),
            None
          Commands.Command.LoadProject(name, site)
          |> CommandActions.postCommand agentRef
          |> Async.RunSynchronously
          |> Either.map ignore
        | None ->
          Either.succeed ()

      do vmSetup ()

      let result =
        let kont = ref true
        let rec proc kontinue =
          Console.ReadLine() |> ignore
          if !kontinue then
            proc kontinue
        proc kont

      dispose discoService

      return result
    }

  // ** createProject

  /// ## createProject
  ///
  /// Create a new project given the passed command line options.
  ///
  /// ### Signature:
  /// - parsed: ParseResult<CLIArguments>
  ///
  /// Returns: unit
  let createProject (parsed: ParseResults<CLIArguments>) = either {
      let parameters =
        [ yield "project", parsed.GetResult <@ Project @>
          yield "bind", parsed.GetResult <@ Bind @>
          yield "git", parsed.GetResult <@ Git  @> |> string
          yield "ws", parsed.GetResult <@ Ws  @> |> string
          yield "api", parsed.GetResult <@ Api  @> |> string
          yield "raft", parsed.GetResult <@ Raft  @> |> string ]
        |> dict

      return! tryCreateProject false parameters
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
      let path = datadir </> filepath (PROJECT_FILENAME + ASSET_EXTENSION)
      let raftDir = datadir </> filepath RAFT_DIRECTORY

      let machine = MachineConfig.get()
      let! project = Asset.loadWithMachine path machine

      do! match Directory.exists raftDir with
          | true  -> rmDir raftDir
          | false -> Either.nothing

      do! mkDir raftDir

      let! raft = createRaft project.Config
      let! _ = saveRaft project.Config raft
      return ()
    }

  // ** help

  [<Literal>]
  let private header = @"   *   .  *.  .
 *  ___ ____.*___ ____  .     * .
* .|_ _|  _ \|_ _/ ___|*    .
  * | || |_) || |\___ \  .*  *
    | ||  _ < | | ___) |     .
.* |___|_| \_\___|____/. Automation Framework Daemon © Nsynk GmbH, 2016
*        .*           .* .
 "

  let help () =
    parser.PrintUsage(header, "disco.exe", true)
    |> flip (printfn "%s\n%s") SubCommand.Doc
    |> Either.succeed

  // ** addUser

  let private readPass (field: string) =
    let mutable pass = ""
    while String.length pass = 0 do
      printf "%s: " field
      let mutable last = Unchecked.defaultof<ConsoleKeyInfo>
      while last.Key <> ConsoleKey.Enter do
        last <- Console.ReadKey(true)
        if last.Key <> ConsoleKey.Backspace && last.Key <> ConsoleKey.Enter then
          pass <- sprintf "%s%c" pass last.KeyChar
          Console.Write("*")
        else
          if last.Key = ConsoleKey.Backspace && String.length pass > 0 then
            pass <- String.subString 0 (String.length pass - 1) pass
            Console.Write("\b \b")
      printf "%s" Environment.NewLine
    password pass

  let private readString (field: string) =
    let mutable str = ""
    while String.length str = 0 do
      printf "%s: " field
      str <-
        Console.ReadLine()
        |> String.trim
    str

  let private readPort (field: string) =
    let mutable port = 0us
    while port = 0us do
      let str = readString field
      match UInt16.TryParse(str) with
      | (true, parsed) -> port <- parsed
      | _ -> ()
    port

  let private readIP (field: string) =
    let mutable ip = None
    while Option.isNone ip do
      let str = readString field
      match IpAddress.TryParse str with
      | Right parsed -> ip <- Some parsed
      | Left error -> printfn "%A" error
    Option.get ip

  let private readID (field: string) =
    let mutable id = None
    while Option.isNone id do
      let str = readString field
      match DiscoId.TryParse str with
      | Right parsed -> id <- Some parsed
      | Left _ -> ()
    Option.get id

  let private readEmail (field: string) =
    let pattern = "^.*@.*\..*"
    let mutable email = ""
    while String.length email = 0 do
      let str = readString field
      let m = Regex.Match(str, pattern)
      if m.Success then
        email <- str
    email

  let addUser (datadir: FilePath) =
    either {
      let path = datadir </> filepath (PROJECT_FILENAME + ASSET_EXTENSION)
      let machine = MachineConfig.get()
      let! project = Asset.loadWithMachine path machine

      let username  = readString "UserName"
      let firstname = readString "First Name"
      let lastname  = readString "Last Name"
      let useremail = readEmail  "Email"
      let password1 = readPass   "Enter Password"
      let password2 = readPass   "Re-Enter Password"

      if password1 = password2 then
        let hash, salt = Crypto.hash password1
        let user =
          { Id        = DiscoId.Create()
            UserName  = name username
            FirstName = name firstname
            LastName  = name lastname
            Email     = email useremail
            Password  = hash
            Salt      = salt
            Joined    = DateTime.UtcNow
            Created   = DateTime.UtcNow }
        let! _ = Project.saveAsset user User.Admin project
        return ()
      else
        return!
          "Passwords do not match. Try again Sam."
          |> Error.asOther (logtag "addUser")
          |> Either.fail
    }

  let addMember (datadir: FilePath) =
    either {
      let path = datadir </> filepath (PROJECT_FILENAME + ASSET_EXTENSION)
      let machine = MachineConfig.get()
      let! project = Asset.loadWithMachine path machine

      let id   = readID     "Member ID"
      let hn   = readString "Host Name"
      let ip   = readIP     "IP Address"
      let raft = readPort   "Raft Port"
      let ws   = readPort   "Sockets Port"
      let git  = readPort   "Git Port"

      let mem =
        { Member.create id with
            HostName  = name hn
            IpAddress = ip
            RaftPort  = port raft
            WsPort    = port ws
            GitPort   = port git }

      let! _ =
        DiscoData.saveWithCommit
          datadir
          User.Admin.Signature
          (Project.addMember mem project)

      return ()
    }

#endif