// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.NpmHelper
open Fake.ZipHelper
open Fake.FuchuHelper
open Fake.Paket
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
open System
open System.IO
open System.Diagnostics

let konst x _ = x

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
let baseDir = "src/Iris"

let npmPath =
  if File.Exists "/run/current-system/sw/bin/npm" then
    "/run/current-system/sw/bin/npm"
  elif File.Exists "/usr/bin/npm" then
    "/usr/bin/npm"
  elif File.Exists "/usr/local/bin/npm" then
    "/usr/local/bin/npm"
  else // this might work on windows...
    "./packages/Npm.js/tools/npm.cmd"

let flatcPath : string =
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

// Read additional information from the release notes document
let release = LoadReleaseNotes "CHANGELOG.md"

let nativeProjects =
  [ baseDir @@ "Nodes.fsproj"
    baseDir @@ "Service.fsproj"
    baseDir @@ "Tests.fsproj" ]

let jsProjects =
  [ baseDir @@ "Frontend.fsproj"
    baseDir @@ "Worker.fsproj"
    baseDir @@ "Web.Tests.fsproj" ]

// Helper active pattern for project types
let (|Fsproj|Csproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)


let setParams cfg defaults =
  { defaults with
      Verbosity = Some(Quiet)
      Targets = ["Build"]
      Properties = [ "Configuration", cfg ] }

//  ____              _       _
// | __ )  ___   ___ | |_ ___| |_ _ __ __ _ _ __
// |  _ \ / _ \ / _ \| __/ __| __| '__/ _` | '_ \
// | |_) | (_) | (_) | |_\__ \ |_| | | (_| | |_) |
// |____/ \___/ \___/ \__|___/\__|_|  \__,_| .__/
//                                         |_|

Target "Bootstrap"
  (fun _ ->
    Restore(id)                         // restore Paket packages
    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = Install Standard
            WorkingDirectory = "" }) |> ignore

    ExecProcess (fun info ->
                    info.FileName <- "npm"
                    info.Arguments <- "-g install mocha-phantomjs"
                    info.WorkingDirectory <- "$HOME")
                (TimeSpan.FromMinutes 5.0)
    |> ignore)

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

//   ____
//  / ___|___  _ __  _   _
// | |   / _ \| '_ \| | | |
// | |__| (_) | |_) | |_| |
//  \____\___/| .__/ \__, |
//            |_|    |___/

Target "CopyBinaries"
  (fun _ ->
    CopyDir "bin/Iris"  (baseDir @@ "bin/Release/Iris")  (konst true) |> ignore
    CopyDir "bin/Nodes" (baseDir @@ "bin/Release/Nodes") (konst true) |> ignore)

Target "CopyAssets"
  (fun _ ->
    CopyDir "bin/Iris/assets" (baseDir @@ "assets/frontend") (konst true)

    !! (baseDir @@ "bin/*.js")
    |> CopyFiles "bin/Iris/assets/js"
    |> ignore

    !! (baseDir @@ "bin/*.map")
    |> CopyFiles "bin/Iris/assets/js"
    |> ignore)


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
     let nameWithVersion = "Iris-" + release.NugetVersion
     let target = "temp" @@ nameWithVersion

     if Directory.Exists target |> not then
       CreateDir target
     else
       CleanDir target

     CopyDir (target @@ "Iris")  "bin/Iris" (konst true)
     CopyDir (target @@ "Nodes") "bin/Nodes" (konst true)
     let files = !!(target @@ "**")
     CreateZip "temp" (nameWithVersion + ".zip") comment 7 false files
     |> ignore)


//   ____ _
//  / ___| | ___  __ _ _ __
// | |   | |/ _ \/ _` | '_ \
// | |___| |  __/ (_| | | | |
//  \____|_|\___|\__,_|_| |_|

Target "Clean" (fun _ ->
    CleanDirs [
      "bin"
      "temp"
      "docs/output"
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

   let args = "-I " + (baseDir @@ "Schema") + " --csharp " + fbs

   ExecProcess (fun info ->
                  info.FileName  <- flatcPath
                  info.Arguments <- args
                  info.WorkingDirectory <- baseDir)
               (TimeSpan.FromMinutes 5.0)
   |> ignore

   let files =
      !! (baseDir @@ "Iris/Serialization/**/*.cs")
      |> Seq.map (fun p -> "    <Compile Include=\"" + p + "\" />" + Environment.NewLine)
      |> Seq.fold ((+)) ""

   let top = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.top.xml")
   let bot = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.bottom.xml")

   File.WriteAllText((baseDir @@ "Serialization.csproj"), top + files + bot)

   build (setParams "Debug")   (baseDir @@ "Serialization.csproj") |> DoNothing
   build (setParams "Release") (baseDir @@ "Serialization.csproj") |> DoNothing)

//   __
//  / _|___ _____ __ ___   __ _
// | |_/ __|_  / '_ ` _ \ / _` |
// |  _\__ \/ /| | | | | | (_| |
// |_| |___/___|_| |_| |_|\__, |
//                           |_|

Target "FsZMQ"
  (fun _ ->
   build (setParams "Debug")   "src/fszmq/fszmq.fsproj" |> ignore
   build (setParams "Release") "src/fszmq/fszmq.fsproj" |> ignore)

//  ____       _ _      _
// |  _ \ __ _| | | ___| |_
// | |_) / _` | | |/ _ \ __|
// |  __/ (_| | | |  __/ |_
// |_|   \__,_|_|_|\___|\__|

Target "Pallet"
  (fun _ ->
   build (setParams "Debug")   "src/Pallet/Pallet.fsproj" |> ignore
   build (setParams "Release") "src/Pallet/Pallet.fsproj" |> ignore)

Target "PalletTests"
  (fun _ ->
    build (setParams "Debug") "src/Pallet.Tests/Pallet.Tests.fsproj" |> ignore)

Target "RunPalletTests"
  (fun _ ->
    let testsDir = "src/Pallet.Tests"
    ExecProcess (fun info ->
                    info.FileName <- "fsi"
                    info.Arguments <- "run.fsx"
                    info.WorkingDirectory <- testsDir)
                (TimeSpan.FromMinutes 5.0)
   |> ignore)

//  _____                _                 _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| |
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` |
// |  _|| | | (_) | | | | ||  __/ | | | (_| |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| JS!

Target "WatchFrontend" (fun _ ->
    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = (Run "watch-frontend")
            WorkingDirectory = baseDir })
    |> ignore)

Target "BuildFrontend" (fun _ ->
    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = (Run "build-frontend")
            WorkingDirectory = baseDir })
    |> ignore)

Target "BuildFrontendFsProj" (fun _ ->
    build (setParams "Debug") (baseDir @@ "Frontend.fsproj") |> ignore)

Target "BuildWorker" (fun _ ->
    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = (Run "build-worker")
            WorkingDirectory = baseDir })
    |> ignore)

Target "WatchWorker" (fun _ ->
    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = (Run "watch-worker")
            WorkingDirectory = baseDir })
    |> ignore)

Target "BuildWorkerFsProj" (fun _ ->
    build (setParams "Debug") (baseDir @@ "Frontend.fsproj") |> ignore)

//  _____         _
// |_   _|__  ___| |_ ___
//   | |/ _ \/ __| __/ __|
//   | |  __/\__ \ |_\__ \
// JS|_|\___||___/\__|___/

Target "BuildWebTests" (fun _ ->
    let testsDir = baseDir @@ "bin/Debug/Web.Tests"
    let jsDir = testsDir @@ "js"
    let cssDir = testsDir @@ "css"
    let npmMods = "./node_modules"
    let assetsDir = baseDir @@ "/assets/frontend"

    CopyDir testsDir assetsDir (konst true) |> ignore
    CopyFile cssDir (npmMods @@ "/mocha/mocha.css")
    CopyFile jsDir  (npmMods @@ "/mocha/mocha.js")
    CopyFile jsDir  (npmMods @@ "/babel-polyfill/dist/polyfill.js")
    CopyFile jsDir  (npmMods @@ "/virtual-dom/dist/virtual-dom.js")
    CopyFile (jsDir @@ "expect.js") (npmMods @@ "/expect.js/index.js")

    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = (Run "build-tests")
            WorkingDirectory = baseDir })
    |> ignore)

Target "WatchWebTests" (fun _ ->
    Npm(fun p ->
        { p with
            NpmFilePath = npmPath
            Command = (Run "watch-tests")
            WorkingDirectory = baseDir })
    |> ignore)

Target "BuildWebTestsFsProj" (fun _ ->
    build (setParams "Debug") (baseDir @@ "Web.Tests.fsproj") |> ignore)

Target "RunWebTests" (fun _ ->
    ExecProcess (fun info ->
                    info.FileName <- "mocha-phantomjs"
                    info.Arguments <- "-p /home/k/.nix-profile/bin/phantomjs -R dot tests.html"
                    info.WorkingDirectory <- "src/Iris/bin/Debug/Web.Tests")
                (TimeSpan.FromMinutes 5.0)
    |> fun code ->
      match code with
      | 0 -> printfn "ALL GOOD"
      | _ -> exit 1)

//    _   _ _____ _____
//   | \ | | ____|_   _|
//   |  \| |  _|   | |
//  _| |\  | |___  | |
// (_)_| \_|_____| |_|

Target "BuildDebugService"
  (fun _ -> build (setParams "Debug") (baseDir @@ "Service.fsproj") |> ignore)

Target "BuildReleaseService"
  (fun _ -> build (setParams "Release") (baseDir @@ "Service.fsproj") |> ignore)

Target "BuildDebugNodes"
  (fun _ -> build (setParams "Debug") (baseDir @@ "Nodes.fsproj") |> ignore)

Target "BuildReleaseNodes"
  (fun _ -> build (setParams "Release") (baseDir @@ "Nodes.fsproj") |> ignore)

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

Target "BuildTests"
  (fun _ -> build (setParams "Debug") (baseDir @@ "Tests.fsproj") |> ignore)

Target "RunTests"
  (fun _ ->
    let testsDir = baseDir @@ "bin/Debug/Tests"
    ExecProcess (fun info ->
                    info.FileName <- "nix-shell"
                    info.Arguments <- "assets/nix/runtests.nix"
                    info.WorkingDirectory <- testsDir)
                (TimeSpan.FromMinutes 5.0)
    |> ignore)

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

Target "DevServer"
  (fun _ ->
    let info = new ProcessStartInfo("fsi", "DevServer.fsx")
    info.UseShellExecute <- false
    let proc = Process.Start(info)
    proc.WaitForExit()
    printfn "done.")

//  ____
// |  _ \  ___   ___ ___
// | | | |/ _ \ / __/ __|
// | |_| | (_) | (__\__ \
// |____/ \___/ \___|___/

// Target "GenerateReferenceDocs" (fun _ ->
//     if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:REFERENCE"] [] then
//       failwith "generating reference documentation failed"
// )
//
// let generateHelp' fail debug =
//     let args =
//         if debug then ["--define:HELP"]
//         else ["--define:RELEASE"; "--define:HELP"]
//     if executeFSIWithArgs "docs/tools" "generate.fsx" args [] then
//         traceImportant "Help generated"
//     else
//         if fail then
//             failwith "generating help documentation failed"
//         else
//             traceImportant "generating help documentation failed"
//
// let generateHelp fail =
//     generateHelp' fail false
//
// Target "GenerateHelp" (fun _ ->
//     DeleteFile "docs/content/release-notes.md"
//     CopyFile "docs/content/" "CHANGELOG.md"
//     Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"
//
//     DeleteFile "docs/content/license.md"
//     CopyFile "docs/content/" "LICENSE.txt"
//     Rename "docs/content/license.md" "docs/content/LICENSE.txt"
//
//     generateHelp true
// )
//
// Target "GenerateHelpDebug" (fun _ ->
//     DeleteFile "docs/content/release-notes.md"
//     CopyFile "docs/content/" "CHANGELOG.md"
//     Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"
//
//     DeleteFile "docs/content/license.md"
//     CopyFile "docs/content/" "LICENSE.txt"
//     Rename "docs/content/license.md" "docs/content/LICENSE.txt"
//
//     generateHelp' true true
// )
//
// Target "KeepRunning" (fun _ ->
//     use watcher = !! "docs/content/**/*.*" |> WatchChanges (fun changes ->
//          generateHelp false
//     )
//
//     traceImportant "Waiting for help edits. Press any key to stop."
//
//     System.Console.ReadKey() |> ignore
//
//     watcher.Dispose()
// )
//
// Target "GenerateDocs" DoNothing


// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

"Pallet"
==> "PalletTests"
==> "RunPalletTests"

Target "Release" DoNothing

"Clean"
==> "GenerateSerialization"

// Serialization

"GenerateSerialization"
==> "BuildWebTests"

"GenerateSerialization"
==> "BuildFrontend"

"GenerateSerialization"
==> "BuildWorker"

"GenerateSerialization"
==> "BuildReleaseService"

"GenerateSerialization"
==> "BuildReleaseNodes"

// fszmq

"FsZMQ"
==> "BuildReleaseService"

"FsZMQ"
==> "BuildReleaseNodes"

// Pallet

"Pallet"
==> "BuildReleaseService"

"Pallet"
==> "BuildReleaseNodes"

// Tests

"GenerateSerialization"
==> "BuildTests"

"FsZMQ"
==> "BuildTests"

"Pallet"
==> "BuildTests"

"BuildTests"
==> "RunTests"

// ONWARDS!

"BuildReleaseNodes"
==> "BuildReleaseService"
==> "CopyBinaries"

"BuildWorker"
==> "CopyAssets"

"BuildWebTests"
==> "CopyAssets"

"BuildFrontend"
==> "CopyAssets"

"CopyBinaries"
==> "CopyAssets"
==> "CreateArchive"

"CreateArchive"
==> "Release"

"BuildWebTests"
==> "RunWebTests"

//  ____       _                    _    _ _
// |  _ \  ___| |__  _   _  __ _   / \  | | |
// | | | |/ _ \ '_ \| | | |/ _` | / _ \ | | |
// | |_| |  __/ |_) | |_| | (_| |/ ___ \| | |
// |____/ \___|_.__/ \__,_|\__, /_/   \_\_|_|
//                         |___/

Target "DebugAll" DoNothing

"RunWebTests"
==> "DebugAll"

"BuildWorker"
==> "DebugAll"

"BuildFrontend"
==> "DebugAll"

"BuildDebugService"
==> "DebugAll"

"BuildDebugNodes"
==> "DebugAll"

"RunTests"
==> "DebugAll"

"RunPalletTests"
==> "DebugAll"

// "CleanDocs"
//   ==> "GenerateHelp"
//   ==> "GenerateReferenceDocs"
//   ==> "GenerateDocs"
//
// "CleanDocs"
//   ==> "GenerateHelpDebug"
//
// "GenerateHelp"
//   ==> "KeepRunning"

RunTargetOrDefault "Release"
