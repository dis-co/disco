// #I @"../../packages/build/FAKE/tools/"
// #I @"../../packages/build/FSharp.Compiler.Service/lib/net40"
// #r @"../../packages/build/FAKE/tools/FakeLib.dll"
// open Fake
// open Fake.Git
// open Fake.AssemblyInfoFile
// open Fake.ReleaseNotesHelper
// open Fake.UserInputHelper
// open System
// open System.IO

// // Pattern specifying assemblies to be tested using NUnit
// let solutionFile  = "Pallet.Tests.sln"
// let testAssemblies = "bin/Debug/*Tests*.dll"

// Target "Build" (fun _ ->
//   !! solutionFile
//   |> MSBuildDebug "" "Rebuild"
//   |> ignore)

// Target "RunTests" (fun _ ->
//   !! testAssemblies
//   |> NUnit (fun p ->
//       { p with
//           DisableShadowCopy = true
//           TimeOut = TimeSpan.FromMinutes 20.
//           OutputFile = "TestResults.xml" }))

// RunTargetOrDefault "Build"
// RunTargetOrDefault "RunTests"


#I @"./bin/Debug/"
#r @"./bin/Debug/FsCheck.dll"
#r @"./bin/Debug/FsPickler.dll"
#r @"./bin/Debug/Pallet.dll"
#r @"./bin/Debug/Fuchu.dll"
#r @"./bin/Debug/Pallet.Tests.dll"


open Fuchu
open Pallet.Tests

run palletTests
