// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "System.IO.Compression.FileSystem"
#r @"packages/build/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.ZipHelper
open Fake.Paket
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
open Fake.AppVeyor
open System
open System.IO
open System.Diagnostics
open System.Text

module DotNet =
  type ComparisonResult = Smaller | Same | Bigger

  let foldi f init (xs: 'T seq) =
      let mutable i = -1
      (init, xs) ||> Seq.fold (fun state x ->
          i <- i + 1
          f i state x)

  let compareVersions (expected: string) (actual: string) =
      if actual = "*" // Wildcard for custom fable-core builds
      then Same
      else
          let expected = expected.Split('.', '-')
          let actual = actual.Split('.', '-')
          (Same, expected) ||> foldi (fun i comp expectedPart ->
              match comp with
              | Bigger -> Bigger
              | Same when actual.Length <= i -> Smaller
              | Same ->
                  let actualPart = actual.[i]
                  match Int32.TryParse(expectedPart), Int32.TryParse(actualPart) with
                  // TODO: Don't allow bigger for major version?
                  | (true, expectedPart), (true, actualPart) ->
                      if actualPart > expectedPart
                      then Bigger
                      elif actualPart = expectedPart
                      then Same
                      else Smaller
                  | _ ->
                      if actualPart = expectedPart
                      then Same
                      else Smaller
              | Smaller -> Smaller)

  let dotnetcliVersion = "1.0.1"
  let mutable dotnetExePath = environVarOrDefault "DOTNET" "dotnet"

  let installDotnetSdk () =
    let dotnetSDKPath = FullName "./dotnetsdk"

    let correctVersionInstalled =
        try
            let processResult =
                ExecProcessAndReturnMessages (fun info ->
                info.FileName <- dotnetExePath
                info.WorkingDirectory <- Environment.CurrentDirectory
                info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

            let installedVersion = processResult.Messages |> separated ""
            match compareVersions dotnetcliVersion installedVersion with
            | Same | Bigger -> true
            | Smaller -> false
        with
        | _ -> false

    if correctVersionInstalled then
        tracefn "dotnetcli %s already installed" dotnetcliVersion
    else
        CleanDir dotnetSDKPath
        let archiveFileName =
            if isWindows then
                sprintf "dotnet-dev-win-x64.%s.zip" dotnetcliVersion
            elif isLinux then
                sprintf "dotnet-dev-ubuntu-x64.%s.tar.gz" dotnetcliVersion
            else
                sprintf "dotnet-dev-osx-x64.%s.tar.gz" dotnetcliVersion
        let downloadPath =
                sprintf "https://dotnetcli.azureedge.net/dotnet/Sdk/%s/%s" dotnetcliVersion archiveFileName
        let localPath = Path.Combine(dotnetSDKPath, archiveFileName)

        tracefn "Installing '%s' to '%s'" downloadPath localPath

        use webclient = new Net.WebClient()
        webclient.DownloadFile(downloadPath, localPath)

        if not isWindows then
            let assertExitCodeZero x =
                if x = 0 then () else
                failwithf "Command failed with exit code %i" x

            Shell.Exec("tar", sprintf """-xvf "%s" -C "%s" """ localPath dotnetSDKPath)
            |> assertExitCodeZero
        else
            System.IO.Compression.ZipFile.ExtractToDirectory(localPath, dotnetSDKPath)

        tracefn "dotnet cli path - %s" dotnetSDKPath
        System.IO.Directory.EnumerateFiles dotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s" path)
        System.IO.Directory.EnumerateDirectories dotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s%c" path System.IO.Path.DirectorySeparatorChar)

        dotnetExePath <- dotnetSDKPath </> (if isWindows then "dotnet.exe" else "dotnet")


let konst x _ = x

/// Copies a directory recursively without any output.
/// If the target directory does not exist, it will be created.
/// ## Parameters
///
///  - `target` - The target directory.
///  - `source` - The source directory.
///  - `filterFile` - A file filter predicate.
let SilentCopyDir target source filterFile =
    CreateDir target
    Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)
    |> Seq.filter filterFile
    |> Seq.iter (fun file ->
           let fi =
               file
               |> replaceFirst source ""
               |> trimSeparator

           let newFile = target @@ fi
          //  logVerbosefn "%s => %s" file newFile
           DirectoryName newFile |> ensureDirectory
           File.Copy(file, newFile, true))
    |> ignore

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Iris"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "VVVV Automation Infrastructure"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = "Lorem Ipsum Dolor Sit Amet"

// List of author names (for NuGet package)
let authors = [ "Karsten Gebbert <karsten@nsynk.de>" ]

// Tags for your project (for NuGet package)
let tags = "cool funky special shiny"

// File system information
let solutionFile  = "Iris.sln"

// Project code base directory
let baseDir =  __SOURCE_DIRECTORY__ @@ "src" @@ "Iris"

// scripts
let scriptsDir = __SOURCE_DIRECTORY__ @@ "src" @@ "Scripts"
let userScripts = scriptsDir @@ "User"
let devScripts = scriptsDir @@ "Dev"

let docsDir = __SOURCE_DIRECTORY__ @@ "docs" @@ "src"

let useNix = Directory.Exists("/nix")

let isUnix = Environment.OSVersion.Platform = PlatformID.Unix

let maybeFail = function
  | 0    -> ()
  | code -> failwithf "Command failed with exit code %d" code

let npmPath =
  if File.Exists "/run/current-system/sw/bin/npm" then
    "/run/current-system/sw/bin/npm"
  elif File.Exists "/usr/bin/npm" then
    "/usr/bin/npm"
  elif File.Exists "/usr/local/bin/npm" then
    "/usr/local/bin/npm"
  elif Environment.GetEnvironmentVariable("APPVEYOR_CI_BUILD") = "true" then
    @"C:\Users\appveyor\AppData\Roamiug\npm\node_modules\npm\bin\npm.cmd"
  else
    "npm"

let flatcPath : string =
  if Environment.OSVersion.Platform = PlatformID.Unix then
    let info = new ProcessStartInfo("which","flatc")
    info.StandardOutputEncoding <- System.Text.Encoding.UTF8
    info.RedirectStandardOutput <- true
    info.UseShellExecute        <- false
    info.CreateNoWindow         <- true
    use proc = Process.Start info
    proc.WaitForExit()
    match proc.ExitCode with
      | 0 when not proc.StandardOutput.EndOfStream ->
        proc.StandardOutput.ReadLine()
      | _ -> failwith "flatc was not found. Please install FlatBuffers first"
  else "flatc.exe"

// Read additional information from the release notes document
let release = LoadReleaseNotes "CHANGELOG.md"

let nativeProjects =
  [ baseDir @@ "Nodes.fsproj"
    baseDir @@ "Service.fsproj"
    baseDir @@ "Tests.fsproj" ]

let jsProjects =
  [ baseDir @@ "Frontend.fsproj"
    baseDir @@ "Web.Tests.fsproj" ]

// Helper active pattern for project types
let (|Fsproj|Csproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | _                           ->
      failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

let setParams cfg defaults =
  { defaults with
      Verbosity = Some(Quiet)
      Targets = ["Build"]
      Properties = [ "Configuration", cfg ] }

let runMono filepath workdir =
  ExecProcess (fun info ->
                  info.FileName <- "mono"
                  info.Arguments <- filepath
                  info.UseShellExecute <- true
                  info.WorkingDirectory <- workdir)
              (TimeSpan.FromMinutes 5.0)
  |> maybeFail

let runExec filepath args workdir shell =
  ExecProcess (fun info ->
                  info.FileName <- filepath
                  info.Arguments <- if String.length args > 0 then args else info.Arguments
                  info.UseShellExecute <- shell
                  info.WorkingDirectory <- workdir)
              TimeSpan.MaxValue
  |> maybeFail

let runExecAndReturn filepath args workdir =
  ExecProcessAndReturnMessages (fun info ->
    info.FileName <- filepath
    info.Arguments <- if String.length args > 0 then args else info.Arguments
    info.UseShellExecute <- false
    info.WorkingDirectory <- workdir) TimeSpan.MaxValue
  |> fun res -> res.Messages |> String.concat "\n"

let runNet cmd workingDir _ =
  match Environment.OSVersion.Platform with
  | PlatformID.Unix -> runMono cmd workingDir
  | _ ->
    let filepath, args =
      match cmd.IndexOf(' ') with
      | -1 -> cmd, ""
      | i -> cmd.Substring(0,i), cmd.Substring(i+1)
    runExec filepath args workingDir false

/// npm warnings when installing may cause AppVeyor build fail, so they must be swallowed
let runNpmNoErrors cmd workdir _ =
  let npm, cmd =
    match Environment.OSVersion.Platform with
      | PlatformID.Unix ->  "npm", cmd
      | _ -> "cmd", ("/C " + "npm" + " " + cmd)
  runExecAndReturn npm cmd workdir
  |> printfn "%s"

let runNpm cmd workdir _ =
  let npm, cmd =
    match Environment.OSVersion.Platform with
      | PlatformID.Unix ->  "npm", cmd
      | _ -> "cmd", ("/C " + "npm" + " " + cmd)
  // npm warnings may cause AppVeyor build fail, so they must be swallowed
  runExec npm cmd workdir false

let runNode cmd workdir _ =
  let node, cmd =
    match Environment.OSVersion.Platform with
      | PlatformID.Unix ->  "npm", cmd
      | _ -> "cmd", ("/C " + "node" + " " + cmd)
  runExec node cmd workdir false

let runFable fableconfigdir extraArgs _ =
  runNpm ("run fable -- " + fableconfigdir + " " + extraArgs) __SOURCE_DIRECTORY__ ()
  // Run Fable's dev version
  // runNode ("../../Fable/build/fable " + fableconfigdir + " " + extraArgs + " --verbose") __SOURCE_DIRECTORY__ ()

let runTestsOnWindows filepath workdir =
  let arch =
    if Environment.Is64BitOperatingSystem
    then "amd64"
    else "i386"

  printfn "Copy %s to %s" (baseDir @@ arch @@ "libzmq.dll") workdir
  CopyFile workdir (baseDir @@ arch @@ "libzmq.dll")

  printfn "Copy %s to %s" (baseDir @@ arch @@ "libzmq.dll") workdir
  CopyFile workdir (baseDir @@ arch @@ "libsodium.dll")

  ExecProcess (fun info ->
                  info.FileName <- (workdir </> filepath)
                  info.UseShellExecute <- false
                  info.WorkingDirectory <- workdir)
              TimeSpan.MaxValue
  |> maybeFail

let buildDebug fsproj _ =
  build (setParams "Debug") (baseDir @@ fsproj)

let buildRelease fsproj _ =
  build (setParams "Release") (baseDir @@ fsproj)

let frontendDir = __SOURCE_DIRECTORY__ @@ "src" @@ "Frontend"

//  ____              _       _
// | __ )  ___   ___ | |_ ___| |_ _ __ __ _ _ __
// |  _ \ / _ \ / _ \| __/ __| __| '__/ _` | '_ \
// | |_) | (_) | (_) | |_\__ \ |_| | | (_| | |_) |
// |____/ \___/ \___/ \__|___/\__|_|  \__,_| .__/
//                                         |_|

Target "Bootstrap" (fun _ ->
  Restore(id)                              // restore Paket packages
  runNpmNoErrors "install" frontendDir ()
  runExec DotNet.dotnetExePath "restore" frontendDir false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "Core.Frontend") false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "Frontend") false
)

//     _                           _     _       ___        __
//    / \   ___ ___  ___ _ __ ___ | |__ | |_   _|_ _|_ __  / _| ___
//   / _ \ / __/ __|/ _ \ '_ ` _ \| '_ \| | | | || || '_ \| |_ / _ \
//  / ___ \\__ \__ \  __/ | | | | | |_) | | |_| || || | | |  _| (_) |
// /_/   \_\___/___/\___|_| |_| |_|_.__/|_|\__, |___|_| |_|_|  \___/
//                                         |___/

Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    let assemblyFileName (fn: string) (suffix: string) =
      fn.Substring(0,fn.Length - 7) + "Info." + suffix

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        let createPath suffix =
          let filename = assemblyFileName (projFileName.Substring(folderName.Length)) suffix
          (folderName @@ "AssemblyInfo") @@ filename

        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (createPath "fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo (createPath "cs") attributes))

//  __  __             _  __           _
// |  \/  | __ _ _ __ (_)/ _| ___  ___| |_
// | |\/| |/ _` | '_ \| | |_ / _ \/ __| __|
// | |  | | (_| | | | | |  _|  __/\__ \ |_
// |_|  |_|\__,_|_| |_|_|_|  \___||___/\__|

let buildFile = baseDir @@ "Iris/Core/Build.fs"
let buildFileTmpl = @"
namespace Iris.Core

module Build =

  [<Literal>]
  let VERSION = ""{0}""

  [<Literal>]
  let BUILD_ID = ""{1}""

  [<Literal>]
  let BUILD_NUMBER = ""{2}""

  [<Literal>]
  let BUILD_VERSION = ""{3}""

  [<Literal>]
  let BUILD_TIME_UTC = ""{4}""

  [<Literal>]
  let COMMIT = ""{5}""

  [<Literal>]
  let BRANCH = ""{6}""
"

let manifestFile = __SOURCE_DIRECTORY__ @@ "bin/MANIFEST"
let manifestTmpl = @"
Iris Version: {0}
Build Id: {1}
Build Number: {2}
Build Version: {3}
Build Time (UTC): {4}
Commit: {5}
Branch: {6}
"

Target "GenerateBuildFile" (
  fun () ->
    match Environment.GetEnvironmentVariable "APPVEYOR" with
    | "True" ->
      let time = DateTime.Now.ToUniversalTime().ToString()
      String.Format(buildFileTmpl,
        release.AssemblyVersion,
        AppVeyorEnvironment.BuildId,
        AppVeyorEnvironment.BuildNumber,
        AppVeyorEnvironment.BuildVersion,
        time,
        AppVeyorEnvironment.RepoCommit,
        AppVeyorEnvironment.RepoBranch)
      |> fun src -> File.WriteAllText(buildFile, src)
    | _ -> ())

Target "GenerateManifest" (
  fun () ->
    match Environment.GetEnvironmentVariable "APPVEYOR" with
    | "True" ->
      let time = DateTime.Now.ToUniversalTime().ToString()
      String.Format(manifestTmpl,
        release.AssemblyVersion,
        AppVeyorEnvironment.BuildId,
        AppVeyorEnvironment.BuildNumber,
        AppVeyorEnvironment.BuildVersion,
        time,
        AppVeyorEnvironment.RepoCommit,
        AppVeyorEnvironment.RepoBranch)
      |> fun src -> File.WriteAllText(manifestFile, src)
    | _ -> ())

//   ____
//  / ___|___  _ __  _   _
// | |   / _ \| '_ \| | | |
// | |__| (_) | |_) | |_| |
//  \____\___/| .__/ \__, |
//            |_|    |___/

let withoutNodeModules (path: string) =
  path.Contains("node_modules") |> not

Target "CopyBinaries"
  (fun _ ->
    // SilentCopyDir "bin/Core"  (baseDir @@ "bin/Release/Core")  withoutNodeModules
    SilentCopyDir "bin/Iris"  (baseDir @@ "bin/Release/Iris")  withoutNodeModules
    SilentCopyDir "bin/Nodes" (baseDir @@ "bin/Release/Nodes") withoutNodeModules
    SilentCopyDir "bin/MockClient" (baseDir @@ "bin/Release/MockClient") withoutNodeModules
  )

Target "CopyAssets"
  (fun _ ->
    [ "CHANGELOG.md"
    // ; userScripts @@ "runiris.sh"
    ; userScripts @@ "createproject.cmd"
    ; userScripts @@ "runiris.cmd"
    ; userScripts @@ "mockclient.cmd"
    ] |> List.iter (CopyFile "bin/")
    FileUtils.cp (docsDir @@ "md/04_test_package.md") "bin/README.md"
    // Frontend
    SilentCopyDir "bin/Frontend/img" (baseDir @@ "../Frontend/img") withoutNodeModules
    SilentCopyDir "bin/Frontend/js"  (baseDir @@ "../Frontend/js") withoutNodeModules
    SilentCopyDir "bin/Frontend/lib" (baseDir @@ "../Frontend/lib") withoutNodeModules
    FileUtils.cp (baseDir @@ "../Frontend/index.html") "bin/Frontend/"
    FileUtils.cp (baseDir @@ "../Frontend/favicon.ico") "bin/Frontend/"
  )

Target "CopyDocs"
  (fun _ ->
    SilentCopyDir "bin/Docs" docsDir (konst true))

//     _             _     _
//    / \   _ __ ___| |__ (_)_   _____
//   / _ \ | '__/ __| '_ \| \ \ / / _ \
//  / ___ \| | | (__| | | | |\ V /  __/
// /_/   \_\_|  \___|_| |_|_| \_/ \___|

let comment = @"
##  ___      _
## |_ _|_ __(_)___
##  | || '__| / __|
##  | || |  | \__ \
## |___|_|  |_|___/ Automation Toolkit
## NsynK GmbH, 2016

Let us do something, while we have the chance! It is not every day that we are
needed. Not indeed that we personally are needed. Others would meet the case
equally well, if not better. To all mankind they were addressed, those cries for
help still ringing in our ears! But at this place, at this moment of time, all
mankind is us, whether we like it or not. Let us make the most of it, before it
is too late! Let us represent worthily for one the foul brood to which a cruel
fate consigned us! What do you say? It is true that when with folded arms we
weigh the pros and cons we are no less a credit to our species. The tiger bounds
to the help of his congeners without the least reflexion, or else he slinks away
into the depths of the thickets. But that is not the question. What are we doing
here, that is the question. And we are blessed in this, that we happen to know
the answer. Yes, in the immense confusion one thing alone is clear. We are
waiting for Godot to come --

"

Target "CreateArchive"
  (fun _ ->
     let ext = ".zip"
     let nameWithVersion = "Iris-" + release.NugetVersion
     let genericName = "Iris-latest.zip"
     let filename = nameWithVersion + ext
     let folder = "temp" @@ nameWithVersion

     if Directory.Exists folder then
       FileUtils.rm_rf folder

     CopyDir folder "bin/" (konst true)

     let files = !!(folder @@ "**")
     CreateZip "temp" filename comment 7 false files
     CopyFile genericName filename
     let checksum = Checksum.CalculateFileHash(filename).ToLowerInvariant()
     let contents = sprintf "%s  %s\n%s  %s" checksum filename checksum genericName
     File.WriteAllText("Iris.sha256sum",contents))


//   ____ _
//  / ___| | ___  __ _ _ __
// | |   | |/ _ \/ _` | '_ \
// | |___| |  __/ (_| | | | |
//  \____|_|\___|\__,_|_| |_|

Target "Clean" (fun _ ->
    CleanDirs [
      "bin"
      "temp"
      "src/Iris/bin"
      "src/Iris/obj"
    ])

//  ____            _       _ _          _   _
// / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
// \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
//  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
// |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|

Target "GenerateSerialization"
  (fun _ ->
   printfn "Cleaning up previous"

   DeleteFile (baseDir @@ "Serialization.csproj")
   CleanDirs [ baseDir @@ "Iris/Serialization" ]

   let fbs =
      !! (baseDir @@ "Schema/**/*.fbs")
      |> Seq.map (fun p -> " " + p)
      |> Seq.fold ((+)) ""

   // CSHARP
   let args = "-I " + (baseDir @@ "Schema") + " --csharp " + fbs
   runExec flatcPath args baseDir false

   // JAVASCRIPT
   let args = "-I " + (baseDir @@ "Schema") + " -o " + (baseDir @@ "../Frontend/js") + " --js " + fbs
   runExec flatcPath args baseDir false

   let files =
      !! (baseDir @@ "Iris/Serialization/**/*.cs")
      |> Seq.map (fun p -> "    <Compile Include=\"" + p + "\" />" + Environment.NewLine)
      |> Seq.fold ((+)) ""

   let top = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.top.xml")
   let bot = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.bottom.xml")

   File.WriteAllText((baseDir @@ "Serialization.csproj"), top + files + bot)

   buildDebug "Serialization.csproj" ())

//  _____                               __
// |__  /___ _ __ ___   ___ ___  _ __  / _|
//   / // _ \ '__/ _ \ / __/ _ \| '_ \| |_
//  / /|  __/ | | (_) | (_| (_) | | | |  _|
// /____\___|_|  \___/ \___\___/|_| |_|_|

Target "BuildDebugZeroconf"
  (fun _ ->
    build (setParams "Debug") "src/Zeroconf/Mono.Zeroconf/Mono.Zeroconf.csproj"
    build (setParams "Debug") "src/Zeroconf/Mono.Zeroconf.Providers.AvahiDBus/Mono.Zeroconf.Providers.AvahiDBus.csproj"
    build (setParams "Debug") "src/Zeroconf/Mono.Zeroconf.Providers.Bonjour/Mono.Zeroconf.Providers.Bonjour.csproj"
    build (setParams "Debug") "src/Zeroconf/MZClient/MZClient.csproj")

Target "BuildReleaseZeroconf"
  (fun _ ->
    build (setParams "Debug") "src/Zeroconf/Mono.Zeroconf/Mono.Zeroconf.csproj"
    build (setParams "Debug") "src/Zeroconf/Mono.Zeroconf.Providers.AvahiDBus/Mono.Zeroconf.Providers.AvahiDBus.csproj"
    build (setParams "Debug") "src/Zeroconf/Mono.Zeroconf.Providers.Bonjour/Mono.Zeroconf.Providers.Bonjour.csproj")

//  _____                _                 _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| |
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` |
// |  _|| | | (_) | | | | ||  __/ | | | (_| |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| JS!


Target "BuildFrontend" (fun () ->
  DotNet.installDotnetSdk ()
  runNpmNoErrors "install" frontendDir ()
  runExec DotNet.dotnetExePath "restore" frontendDir false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "Core.Frontend") false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "Frontend") false
  runExec DotNet.dotnetExePath "build -c Release" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "fable npm-run build-worker" frontendDir false
  runExec DotNet.dotnetExePath "fable npm-run build" frontendDir false
)

Target "BuildFrontendFast" (fun () ->
  runExec DotNet.dotnetExePath "build -c Release" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "fable npm-run build-worker" frontendDir false
  runExec DotNet.dotnetExePath "fable npm-run build" frontendDir false
)


//  _____         _
// |_   _|__  ___| |_ ___
//   | |/ _ \/ __| __/ __|
//   | |  __/\__ \ |_\__ \
// JS|_|\___||___/\__|___/

Target "BuildWebTests" (fun _ ->
  DotNet.installDotnetSdk ()
  runNpmNoErrors "install" frontendDir ()
  runExec DotNet.dotnetExePath "restore" frontendDir false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "restore" (frontendDir @@ "fable" @@ "Tests.Frontend") false
  runExec DotNet.dotnetExePath "build -c Release" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "fable npm-run build-test" frontendDir false
)

Target "BuildWebTestsFast" (fun _ ->
  runExec DotNet.dotnetExePath "build -c Release" (frontendDir @@ "fable" @@ "plugins") false
  runExec DotNet.dotnetExePath "fable npm-run build-test" frontendDir false
)

let runWebTests = (fun _ ->
  // Please leave for Karsten's tests to keep working :)
  if useNix then
    let phantomJsPath = environVarOrDefault "PHANTOMJS_PATH" "phantomjs"
    runExec phantomJsPath "src/Frontend/node_modules/mocha-phantomjs-core/mocha-phantomjs-core.js src/Frontend/tests.html tap" __SOURCE_DIRECTORY__ false
  else
    runNpm "test" frontendDir ()
)

Target "RunWebTests" runWebTests
Target "RunWebTestsFast" runWebTests

//    _   _ _____ _____
//   | \ | | ____|_   _|
//   |  \| |  _|   | |
//  _| |\  | |___  | |
// (_)_| \_|_____| |_|

Target "BuildDebugCore" (buildDebug "Projects/Core/Core.fsproj")

Target "BuildReleaseCore" (buildRelease "Projects/Core/Core.fsproj")

Target "BuildDebugService" (fun () ->
  buildDebug "Projects/Service/Service.fsproj" ()
  // let assetsTargetDir = (baseDir @@ "bin" @@ "Debug" @@ "Iris" @@ "assets")
  // FileUtils.cp_r (baseDir @@ "assets/frontend") assetsTargetDir
  // runNpmNoErrors "install" assetsTargetDir ()
)

Target "BuildReleaseService" (fun () ->
  buildRelease "Projects/Service/Service.fsproj" ()
  // let targetDir = (baseDir @@ "bin/Release/Iris/assets")
  // FileUtils.cp_r (baseDir @@ "assets/frontend") targetDir
  // runNpmNoErrors "install" targetDir ()
)

Target "BuildDebugNodes" (buildDebug "Projects/Nodes/Nodes.fsproj")

Target "BuildReleaseNodes" (buildRelease "Projects/Nodes/Nodes.fsproj")

Target "BuildDebugMockClient" (buildDebug "Projects/MockClient/MockClient.fsproj")

Target "BuildReleaseMockClient" (buildRelease "Projects/MockClient/MockClient.fsproj")

//  _____         _
// |_   _|__  ___| |_ ___
//   | |/ _ \/ __| __/ __|
//   | |  __/\__ \ |_\__ \
//   |_|\___||___/\__|___/

(*
   Working with libgit2 native libraries:

   - see ldd bin/Debug/NativeBinaries/linux/amd64/libgit...so for dependencies
   - set MONO_LOG_LEVEL=debug for more VM info
   - ln -s bin/Debug/NativeBinaries bin/Debug/libNativeBinaries
   - set LD_LIBRARY_PATH=....:/run/current-system/sw/lib/

   now it *should* work. YMMV.

   Good Fix: use a nix-shell environment that exposes LD_LIBRARY_PATH correctly.
*)

Target "BuildTests" (buildDebug "Projects/Tests/Tests.fsproj")
Target "BuildTestsFast" (buildDebug "Projects/Tests/Tests.fsproj")

let runTests = (fun _ ->
  let testsDir = baseDir @@ "bin" @@ "Debug" @@ "Tests"

  if isUnix then
    runMono "Iris.Tests.exe" testsDir
  else
    runTestsOnWindows "Iris.Tests.exe" testsDir)

Target "RunTests" runTests
Target "RunTestsFast" runTests

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

Target "DevServer"
  (fun _ ->
    let info = new ProcessStartInfo("fsi", devScripts @@ "DevServer.fsx")
    info.UseShellExecute <- false
    let proc = Process.Start(info)
    proc.WaitForExit()
    printfn "done.")

//  ____
// |  _ \  ___   ___ ___
// | | | |/ _ \ / __/ __|
// | |_| | (_) | (__\__ \
// |____/ \___/ \___|___/

Target "CleanDocs" (fun _ ->
    CleanDir ("docs" @@ "output"))

Target "GenerateReferenceDocs"
  (fun _ ->
    let result =
      executeFSIWithArgs
        "docs/tools"
        "generate.fsx"
        ["--define:RELEASE"; "--define:REFERENCE"]
        []

    if not result then
      failwith "generating reference documentation failed")

let generateHelp' fail debug =
  let args =
    if debug then ["--define:HELP"]
    else ["--define:RELEASE"; "--define:HELP"]
  if executeFSIWithArgs "docs/tools" "generate.fsx" args [] then
    traceImportant "Help generated"
  else
    if fail then
      failwith "generating help documentation failed"
    else
      traceImportant "generating help documentation failed"

let generateHelp fail =
  generateHelp' fail false

Target "GenerateHelp" (fun _ ->
  CopyFile "docs/src/" "CHANGELOG.md"
  CopyFile "docs/src/" "LICENSE.txt"
  generateHelp true)

Target "KeepRunning" (fun _ ->
  use watcher = !! "docs/src/**/*.*" |> WatchChanges (fun changes -> generateHelp false)
  traceImportant "Waiting for help edits. Press any key to stop."
  System.Console.ReadKey() |> ignore)

Target "GenerateDocs" DoNothing

//  _   _       _                 _
// | | | |_ __ | | ___   __ _  __| |
// | | | | '_ \| |/ _ \ / _` |/ _` |
// | |_| | |_) | | (_) | (_| | (_| |
//  \___/| .__/|_|\___/ \__,_|\__,_|
//       |_|

Target "UploadArtifact" (fun () ->
  if AppVeyorEnvironment.RepoBranch = "master" || AppVeyorEnvironment.RepoTag then
    let fn =
      if AppVeyorEnvironment.RepoTag then
        let fn' = sprintf "Iris-%s.zip" AppVeyorEnvironment.RepoTagName
        Rename fn' "Iris-latest.zip"
        fn'
      else "Iris-latest.zip"
    let user = Environment.GetEnvironmentVariable "BITBUCKET_USER"
    let pw = Environment.GetEnvironmentVariable "BITBUCKET_PW"
    let url = "https://api.bitbucket.org/2.0/repositories/nsynk/iris/downloads"
    let tpl = @"-s -X POST -u {0}:{1} {2} -F files=@{3}"
    let args = String.Format(tpl, user, pw, url, fn)
    runExec "curl" args __SOURCE_DIRECTORY__ false
)

//  ____       _                 ____             _
// |  _ \  ___| |__  _   _  __ _|  _ \  ___   ___| | _____ _ __
// | | | |/ _ \ '_ \| | | |/ _` | | | |/ _ \ / __| |/ / _ \ '__|
// | |_| |  __/ |_) | |_| | (_| | |_| | (_) | (__|   <  __/ |
// |____/ \___|_.__/ \__,_|\__, |____/ \___/ \___|_|\_\___|_|
//                         |___/

let getCommitHash() =
  let log = runExecAndReturn "git" "log -n1 --oneline" baseDir
  log.Substring(0, log.IndexOf(" "))

let dockerCreateImage hash workingDir =
  let cmd = sprintf "build --label iris --tag iris:%s ." hash
  runExec "docker" cmd workingDir false

Target "DockerCreateBaseImage" (fun () ->
  runExec "docker" "build --label iris --tag iris:base ../Docker/iris_base" baseDir false
)

Target "DockerRunTests" (fun () ->
  let testsDir = "src/Iris/bin/Debug/Tests"
  FileUtils.cp_r "src/Docker/iris/" testsDir
  let hash = getCommitHash()
  dockerCreateImage hash testsDir
  let irisNodeId = Guid.NewGuid().ToString()
  let img = runExecAndReturn "docker" ("images -q iris:" + hash) baseDir
  let runCmd = sprintf "run -i --rm -e IRIS_NODE_ID=%s -e COMMAND=tests %s" irisNodeId img
  runExec "docker" runCmd  baseDir false
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "Release" DoNothing

"Clean"
==> "GenerateSerialization"

// Serialization

"GenerateBuildFile"
==> "GenerateSerialization"

"GenerateSerialization"
==> "BuildWebTests"

"GenerateSerialization"
==> "BuildFrontend"

"GenerateSerialization"
==> "BuildReleaseService"

"GenerateSerialization"
==> "BuildReleaseNodes"

"GenerateSerialization"
==> "BuildDebugMockClient"

// Zeroconf

"BuildReleaseZeroconf"
==> "BuildReleaseService"

"BuildReleaseZeroconf"
==> "BuildReleaseCore"

"BuildReleaseZeroconf"
==> "BuildDebugMockClient"

// Tests

"GenerateSerialization"
==> "BuildTests"

"BuildTests"
==> "RunTests"

"BuildTestsFast"
==> "RunTestsFast"

// ONWARDS!

"BuildReleaseNodes"
==> "BuildReleaseService"
==> "BuildFrontend"
// ==> "BuildReleaseCore"
==> "BuildReleaseMockClient"
==> "CopyBinaries"

// "BuildWebTests"
// ==> "CopyAssets"

"CopyBinaries"
==> "CopyAssets"
==> "CopyDocs"
==> "GenerateManifest"
==> "CreateArchive"

"CreateArchive"
==> "Release"

"BuildWebTests"
==> "RunWebTests"

"BuildWebTestsFast"
==> "RunWebTestsFast"

"BuildTests"
==> "DockerRunTests"

// let startDockerCmd() =
//   let project, irisNodeId, image = "foo", "foo", "foo"
//   sprintf """run -p 7000:7000 -i --rm -v %s:/project \
//     -e IRIS_NODE_ID=%s -e COMMAND=start %s""" project irisNodeId image

// Target "CreateDocker" (fun () ->
//   FileUtils.cp_r "src/Docker/iris/" "src/Iris/bin/Debug/Iris"
//   runExec "docker" (createDockerCmd()) baseDir false
// )

// Target "StartDockerImage" (fun () ->
//   runExec "docker" (startDockerCmd()) baseDir false
// )

Target "DebugDocs" DoNothing

"GenerateDocs"
==> "DebugDocs"

//  ____       _                    _    _ _
// |  _ \  ___| |__  _   _  __ _   / \  | | |
// | | | |/ _ \ '_ \| | | |/ _` | / _ \ | | |
// | |_| |  __/ |_) | |_| | (_| |/ ___ \| | |
// |____/ \___|_.__/ \__,_|\__, /_/   \_\_|_|
//                         |___/

Target "DebugAll" DoNothing

"BuildFrontend"
==> "DebugAll"

"BuildDebugService"
==> "DebugAll"

"BuildDebugNodes"
==> "DebugAll"

//
// "CleanDocs"
//   ==> "GenerateHelpDebug"
//
// "GenerateHelp"
//   ==> "KeepRunning"

Target "AllTests" DoNothing

"RunTests"
==> "AllTests"

"RunWebTests"
==> "AllTests"

RunTargetOrDefault "Release"
