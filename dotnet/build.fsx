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
            WorkingDirectory = "" }))

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
//  ___      _
// |_ _|_ __(_)___
//  | || '__| / __|
//  | || |  | \__ \
// |___|_|  |_|___/ Automation Toolkit
// NsynK GmbH, 2016
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

// Target "CreateArchive" (fun _ ->
//     [   "", !! "bin/Iris/**"
//             ++ "bin/Nodes/**"
//     ]
//     |> ZipOfIncludes (sprintf @"tests.%s.zip" "hahahahah")
// )

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
      !! (baseDir @@ "Schema/*.fbs")
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
      !! (baseDir @@ "Iris/Serialization/*.cs")
      |> Seq.map (fun p -> "    <Compile Include=\"" + p + "\" />" + Environment.NewLine)
      |> Seq.fold ((+)) "" 
   
   let top = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.top.xml")
   let bot = File.ReadAllText (baseDir @@ "assets/csproj/Serialization.bottom.xml")
   
   File.WriteAllText((baseDir @@ "Serialization.csproj"), top + files + bot)

   MSBuildDebug "" "Build" [ baseDir @@ "Serialization.csproj" ]
   |> ignore

   MSBuildRelease "" "Build" [ baseDir @@ "Serialization.csproj" ]
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

Target "BuildWebTests" (fun _ ->
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

//    _   _ _____ _____
//   | \ | | ____|_   _|
//   |  \| |  _|   | |
//  _| |\  | |___  | |
// (_)_| \_|_____| |_|

Target "BuildDebugService"
  (fun _ ->
    MSBuildDebug "" "Rebuild" [ baseDir @@ "Service.fsproj" ]
    // |> BuildFrontEnd 
    |> ignore)

Target "BuildReleaseService"
  (fun _ ->
    MSBuildRelease "" "Rebuild" [ baseDir @@ "Service.fsproj" ]
    // |> BuildFrontEnd 
    |> ignore)

Target "BuildDebugNodes"
  (fun _ ->
    MSBuildDebug "" "Rebuild" [ baseDir @@ "Nodes.fsproj" ]
    // |> BuildFrontEnd 
    |> ignore)

Target "BuildReleaseNodes"
  (fun _ ->
    MSBuildRelease "" "Rebuild" [ baseDir @@ "Nodes.fsproj" ]
    // |> BuildFrontEnd 
    |> ignore)

//  _____         _
// |_   _|__  ___| |_ ___
//   | |/ _ \/ __| __/ __|
//   | |  __/\__ \ |_\__ \
//   |_|\___||___/\__|___/

Target "RunTests"
  (fun _ ->
    MSBuildDebug "" "Rebuild" [ baseDir @@ "Tests.fsproj" ] |> ignore
    failwith "FIX THE TESTS ON NIXOS DUDE")

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

Target "Release" DoNothing

"Clean"
==> "GenerateSerialization"
==> "BuildFrontend"
==> "BuildWorker"
==> "BuildWebTests"
==> "BuildReleaseService"
==> "BuildReleaseNodes"
==> "CopyBinaries"
==> "CopyAssets"
==> "CreateArchive"
==> "Release"

Target "All" DoNothing

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
    
RunTargetOrDefault "All"
