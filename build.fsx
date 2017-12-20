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

// ---------------------------------------------------------------------
// VALUES
// ---------------------------------------------------------------------

// Project Info
let project = "Disco"
let summary = "VVVV Automation Infrastructure"
let description = "Lorem Ipsum Dolor Sit Amet"
let authors = []
let tags = "cool funky special shiny"

// Paths
let baseDir =  __SOURCE_DIRECTORY__ @@ "src" @@ "Disco"
let scriptsDir = __SOURCE_DIRECTORY__ @@ "src" @@ "Scripts"
let userScripts = scriptsDir @@ "User"
let devScripts = scriptsDir @@ "Dev"
let docsDir = __SOURCE_DIRECTORY__ @@ "docs" @@ "tools" @@ "public"
let frontendDir = __SOURCE_DIRECTORY__ @@ "src" @@ "Frontend"

// System
let useNix = Directory.Exists("/nix")
let isUnix = Environment.OSVersion.Platform = PlatformID.Unix
let dotnetcliVersion = "2.0.0"
let mutable dotnetExePath = environVarOrDefault "DOTNET" "dotnet"

let npmPath =
  if File.Exists "/run/current-system/sw/bin/npm"
  then "/run/current-system/sw/bin/npm"
  elif File.Exists "/usr/bin/npm"
  then "/usr/bin/npm"
  elif File.Exists "/usr/local/bin/npm"
  then "/usr/local/bin/npm"
  elif Environment.GetEnvironmentVariable("APPVEYOR") = "True"
  then @"C:\Users\appveyor\AppData\Roamiug\npm\node_modules\npm\bin\npm.cmd"
  else "npm"

let flatcPath : string =
  if Environment.OSVersion.Platform = PlatformID.Unix
  then "flatc"
  else __SOURCE_DIRECTORY__ </> "src/Lib/flatc.exe"

// Read additional information from the release notes document
let release = LoadReleaseNotes "CHANGELOG.md"

//  __  __             _  __           _
// |  \/  | __ _ _ __ (_)/ _| ___  ___| |_
// | |\/| |/ _` | '_ \| | |_ / _ \/ __| __|
// | |  | | (_| | | | | |  _|  __/\__ \ |_
// |_|  |_|\__,_|_| |_|_|_|  \___||___/\__|

let buildFile = baseDir @@ "Disco/Core/Build.fs"
let buildFileTmpl = @"
namespace Disco.Core

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
Disco Version: {0}
Build Id: {1}
Build Number: {2}
Build Version: {3}
Build Time (UTC): {4}
Commit: {5}
Branch: {6}
"







let comment = @"
##  ____  _
## |  _ \(_)___  ___ ___
## | | | | / __|/ __/ _ \
## | |_| | \__ \ (_| (_) |
## |____/|_|___/\___\___/  Automation Toolkit

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

// ---------------------------------------------------------------------
// HELPERS
// ---------------------------------------------------------------------
let installDotnetSdk () =
  match environVarOrNone "DEV_MACHINE" with
  | Some _ -> ()
  | None -> dotnetExePath <- DotNetCli.InstallDotNetSDK dotnetcliVersion

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
      let fi = file |> replaceFirst source "" |> trimSeparator
      let newFile = target @@ fi
      //  logVerbosefn "%s => %s" file newFile
      DirectoryName newFile |> ensureDirectory
      File.Copy(file, newFile, true))
    |> ignore

let maybeFail = function
  | 0    -> ()
  | code -> failwithf "Command failed with exit code %d" code

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
  printfn "CWD: %s" workdir
  ExecProcess (fun info ->
                  info.FileName <- "mono"
                  info.Arguments <- filepath
                  info.UseShellExecute <- true
                  info.WorkingDirectory <- workdir)
              (TimeSpan.FromMinutes 5.0)
  |> maybeFail

let runExec filepath args workdir shell =
  printfn "CWD: %s" workdir
  ExecProcess (fun info ->
                  info.FileName <- filepath
                  info.Arguments <- if String.length args > 0 then args else info.Arguments
                  info.UseShellExecute <- shell
                  info.WorkingDirectory <- workdir)
              TimeSpan.MaxValue
  |> maybeFail

let runExecAndReturn filepath args workdir =
  printfn "CWD: %s" workdir
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

let runTestsOnWindows filepath workdir =
  ExecProcess (fun info ->
                  info.FileName <- (workdir </> filepath)
                  info.UseShellExecute <- false
                  info.WorkingDirectory <- workdir)
              TimeSpan.MaxValue
  |> maybeFail

type BuildConfig =
  | Debug
  | Release

let build config fsproj _ =
  let config = match config with Debug -> "Debug" | Release -> "Release"
  build (setParams config) (baseDir @@ fsproj)

let withoutBuildData (path: string) =
  not (path.Contains("node_modules")) &&
  not (path.Contains("_temporary_compressed_files"))

// ---------------------------------------------------------------------
// ACTIONS
// ---------------------------------------------------------------------

let assemblyInfo () =
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
      | Csproj -> CreateCSharpAssemblyInfo (createPath "cs") attributes)

let generateBuildFile () =
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
  | _ -> ()

let generateManifest () =
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
  | _ -> ()

let copyBinaries () =
  Directory.CreateDirectory "bin/Clients" |> ignore
  SilentCopyDir "bin/Disco"                      (baseDir @@ "bin/Release/Disco")      withoutBuildData
  SilentCopyDir "bin/Clients/vvvv"               (baseDir @@ "../../vvvv")              withoutBuildData
  SilentCopyDir "bin/Clients/vvvv/plugins/nodes" (baseDir @@ "bin/Release/Nodes")      withoutBuildData
  SilentCopyDir "bin/Clients/cli-client"         (baseDir @@ "bin/Release/MockClient") withoutBuildData
  SilentCopyDir "bin/Sdk"                        (baseDir @@ "bin/Release/Sdk")        withoutBuildData

let copyAssets () =
  [ "CHANGELOG.md"] |> List.iter (CopyFile "bin/")
  FileUtils.cp (__SOURCE_DIRECTORY__ @@ "docs/files/test_package.md") "bin/README.md"
  // Frontend
  SilentCopyDir "bin/Disco/www/css" (baseDir @@ "../Frontend/css") withoutBuildData
  SilentCopyDir "bin/Disco/www/js"  (baseDir @@ "../Frontend/js") withoutBuildData
  SilentCopyDir "bin/Disco/www/lib" (baseDir @@ "../Frontend/lib") withoutBuildData
  FileUtils.cp (baseDir @@ "../Frontend/index.html") "bin/Disco/www/"
  FileUtils.cp (baseDir @@ "../Frontend/favicon.ico") "bin/Disco/www/"

let createArchive () =
   let ext = ".zip"
   let nameWithVersion = "Disco-" + release.NugetVersion
   let genericName = "Disco-latest.zip"
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
   File.WriteAllText("Disco.sha256sum",contents)

// TODO: Add another target to run `git clean -xfd`?
let clean () =
  // TODO: Delete also frontend, doc, web files
  CleanDirs ["bin"; "temp"]

  !!"src/**/bin"
  ++ "docs/**/bin"
  |> CleanDirs

  // Don't delete whole `obj` folders to speed up restoration
  !! "src/**/obj/*.nuspec"
  ++ "test/**/obj/*.nuspec"
  |> DeleteFiles

let generateSerialization () =
  printfn "Cleaning up previous"

  DeleteFile (baseDir @@ "Serialization.csproj")
  CleanDirs [ baseDir @@ "Disco/Serialization" ]

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
    !! (baseDir @@ "Disco/Serialization/**/*.cs")
    |> Seq.map (fun p -> "    <Compile Include=\"" + p + "\" />" + Environment.NewLine)
    |> Seq.fold ((+)) ""

  let top = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.top.xml")
  let bot = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.bottom.xml")

  File.WriteAllText((baseDir @@ "Serialization.csproj"), top + files + bot)

  build Debug "Serialization.csproj" ()
  build Release "Serialization.csproj" ()

let buildZeroconf config () =
    build config "../Zeroconf/Mono.Zeroconf/Mono.Zeroconf.csproj" ()
    build config "../Zeroconf/Mono.Zeroconf.Providers.AvahiDBus/Mono.Zeroconf.Providers.AvahiDBus.csproj" ()
    build config "../Zeroconf/Mono.Zeroconf.Providers.Bonjour/Mono.Zeroconf.Providers.Bonjour.csproj" ()

let buildFrontendPlugins () =
  runExec dotnetExePath "build -c Release" (frontendDir @@ "src" @@ "FlatBuffersPlugin") false

let restoreFrontend () =
  runExec dotnetExePath "restore Disco.Frontend.sln" (frontendDir @@ "src") false

let bootStrap () =
  installDotnetSdk ()
  generateSerialization ()
  buildZeroconf Debug ()
  buildZeroconf Release ()
  restoreFrontend ()
  buildFrontendPlugins ()
  runNpmNoErrors "install" __SOURCE_DIRECTORY__ ()

let buildFrontend () =
  runNpmNoErrors "install" __SOURCE_DIRECTORY__ ()
  installDotnetSdk ()
  restoreFrontend ()
  buildFrontendPlugins ()
  runNpm ("run build") __SOURCE_DIRECTORY__ ()

let buildFrontendFast () =
  runNpm "run build" __SOURCE_DIRECTORY__ ()

let buildWebTests () =
  installDotnetSdk ()
  restoreFrontend ()
  buildFrontendPlugins ()
  runNpmNoErrors "install" __SOURCE_DIRECTORY__ ()
  runNpm ("run build-tests") __SOURCE_DIRECTORY__ ()

let buildWebTestsFast () =
  runNpm ("run build-tests") __SOURCE_DIRECTORY__ ()

let runWebTests () =
  // Please leave for Karsten's tests to keep working :)
  if useNix then
    let phantomJsPath = environVarOrDefault "PHANTOMJS_PATH" "phantomjs"
    runExec phantomJsPath "node_modules/mocha-phantomjs-core/mocha-phantomjs-core.js src/Frontend/tests.html tap" __SOURCE_DIRECTORY__ false
  else
    runNpm "test" __SOURCE_DIRECTORY__ ()

///  Working with libgit2 native libraries:
///
///  - see ldd bin/Debug/NativeBinaries/linux/amd64/libgit...so for dependencies
///  - set MONO_LOG_LEVEL=debug for more VM info
///  - ln -s bin/Debug/NativeBinaries bin/Debug/libNativeBinaries
///  - set LD_LIBRARY_PATH=....:/run/current-system/sw/lib/
///
///  now it *should* work. YMMV.
///
///  Good Fix: use a nix-shell environment that exposes LD_LIBRARY_PATH correctly.
let runTests config () =
  let config = match config with Debug -> "Debug" | Release -> "Release"
  let testsDir = baseDir @@ "bin" @@ config @@ "Tests"
  if isUnix
  then runMono "Disco.Tests.exe" testsDir
  else runTestsOnWindows "Disco.Tests.exe" testsDir

// TODO: Watch only handles Fable project but it should watch also markdown files
let generateDocs (watch: bool) () =
  // Build Frontend project to generate XML documentation
  runExec dotnetExePath "restore" (frontendDir @@ "src/Frontend") false
  runExec dotnetExePath "build"   (frontendDir @@ "src/Frontend") false
  // Generate web pages
  runNpmNoErrors "install" (__SOURCE_DIRECTORY__ @@ "docs/tools") ()
  let cmd = if watch then "run start" else "run build"
  runNpmNoErrors cmd (__SOURCE_DIRECTORY__ @@ "docs/tools") ()

let copyDocs () =
  generateDocs false ()
  // Copy them to package
  SilentCopyDir "bin/Docs" docsDir (konst true)

let uploadArtifact () =
  if AppVeyorEnvironment.RepoBranch = "master" || AppVeyorEnvironment.RepoTag then
    let fn =
      if AppVeyorEnvironment.RepoTag then
        let fn' = sprintf "Disco-%s.zip" AppVeyorEnvironment.RepoTagName
        Rename fn' "Disco-latest.zip"
        fn'
      else "Disco-latest.zip"
    let user = Environment.GetEnvironmentVariable "BITBUCKET_USER"
    let pw = Environment.GetEnvironmentVariable "BITBUCKET_PW"
    let url = "https://api.bitbucket.org/2.0/repositories/nsynk/disco/downloads"
    let tpl = @"-s -X POST -u {0}:{1} {2} -F files=@{3}"
    let args = String.Format(tpl, user, pw, url, fn)
    runExec "curl" args __SOURCE_DIRECTORY__ false

// --------------------------------------------------------------------------------------
// TARGETS
// --------------------------------------------------------------------------------------

// Initialization
Target "Clean" clean
Target "Bootstrap" bootStrap
Target "AssemblyInfo" assemblyInfo
Target "GenerateBuildFile" generateBuildFile
Target "GenerateManifest" generateManifest
Target "GenerateSerialization" generateSerialization

// Frontend
Target "BuildFrontendPlugins" buildFrontendPlugins
Target "BuildFrontend" buildFrontend
Target "BuildWebTests" buildWebTests
Target "RunWebTests" runWebTests

// Service
Target "BuildDebugZeroconf"   (buildZeroconf Debug)
Target "BuildDebugCore"       (build Debug "Projects/Core/Core.fsproj")
Target "BuildDebugService"    (build Debug "Projects/Service/Service.fsproj")
Target "BuildDebugNodes"      (build Debug "Projects/Nodes/Nodes.fsproj")
Target "BuildDebugSdk"        (build Debug "Projects/Sdk/Sdk.fsproj")
Target "BuildDebugMockClient" (build Debug "Projects/MockClient/MockClient.fsproj")
Target "BuildDebugRaspi"      (build Debug "Projects/RaspberryPi/RaspberryPi.fsproj")
Target "BuildDebugTests"      (build Debug "Projects/Tests/Tests.fsproj")
Target "RunDebugTests"        (runTests Debug)

Target "BuildReleaseZeroconf"   (buildZeroconf Release)
Target "BuildReleaseCore"       (build Release "Projects/Core/Core.fsproj")
Target "BuildReleaseService"    (build Release "Projects/Service/Service.fsproj")
Target "BuildReleaseNodes"      (build Release "Projects/Nodes/Nodes.fsproj")
Target "BuildReleaseSdk"        (build Release "Projects/Sdk/Sdk.fsproj")
Target "BuildReleaseMockClient" (build Release "Projects/MockClient/MockClient.fsproj")
Target "BuildReleaseRaspi"      (build Release "Projects/RaspberryPi/RaspberryPi.fsproj")
Target "BuildReleaseTests"      (build Release "Projects/Tests/Tests.fsproj")
Target "RunReleaseTests"        (runTests Release)

// TODO: Remove "Fast" targets? (they're called from Makefile)
Target "BuildFrontendFast" buildFrontendFast
Target "BuildTestsFast" (build Debug "Projects/Tests/Tests.fsproj")
Target "RunTestsFast" (fun () ->
  build Debug "Projects/Tests/Tests.fsproj" ()
  runTests Debug ())
Target "BuildWebTestsFast" buildWebTestsFast
Target "RunWebTestsFast" (fun () ->
  buildWebTestsFast ()
  runWebTests ())

// Docs and archiving
Target "GenerateDocs" (generateDocs false)
Target "WatchDocs" (generateDocs true)
Target "CopyDocs" copyDocs
Target "CopyBinaries" copyBinaries
Target "CopyAssets" copyAssets
Target "CreateArchive" createArchive
Target "UploadArtifact" uploadArtifact

Target "Release" (fun () ->
  clean ()
  generateBuildFile ()
  generateSerialization ()
  buildZeroconf Release ()
  build Release "Projects/Sdk/Sdk.fsproj" ()
  build Release "Projects/Nodes/Nodes.fsproj" ()
  build Release "Projects/Service/Service.fsproj" ()
  build Release "Projects/MockClient/MockClient.fsproj" ()
  buildFrontend ()
  copyBinaries ()
  copyAssets ()
  copyDocs ()
  generateManifest ()
  createArchive ()
)

Target "AllTests" (fun () ->
  clean ()
  generateBuildFile ()
  generateSerialization ()
  buildZeroconf Release ()
  // TODO: Use debug mode? (as previously)
  build Release "Projects/Tests/Tests.fsproj" ()
  runTests Release ()
  buildWebTests ()
  runWebTests ()
)

RunTargetOrDefault "Release"
