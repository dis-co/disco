// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
// Any binary output / copied to bin/projectName/projectName.dll will
// automatically be added as a binary to generate API docs for.
// for binaries output to root bin folder please add the filename only to the
// referenceBinaries list below in order to generate documentation for the binaries.
// (This is the original behaviour of ProjectScaffold prior to multi project support)

let referenceBinaries = [
    "Iris.Core.dll"
  ]

// Web site location for the generated documentation
let website = "/Iris"

let info =
  [ "root", "root"
    "project-description", "Iris.Core"
    "project-author", "Karsten Gebbert, Alfonso Garcia-Caro"
    "project-name", "Iris" ]
// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#I "../../packages/build/FAKE/tools/"
#load "../../packages/build/FSharp.Formatting/FSharp.Formatting.fsx"
#r "NuGet.Core.dll"
#r "FakeLib.dll"

open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "../../bin/Core"
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files      = __SOURCE_DIRECTORY__ @@ "../files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/build/FSharp.Formatting/"
let docTemplate = "template.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRootsAll = new System.Collections.Generic.Dictionary<string, string list>()
layoutRootsAll.Add("en",[ templates; formatting @@ "templates"
                          formatting @@ "templates/reference" ])
subDirectories (directoryInfo templates)
|> Seq.iter (fun d ->
                let name = d.Name
                if name.Length = 2 || name.Length = 3 then
                    layoutRootsAll.Add(
                            name, [templates @@ name
                                   formatting @@ "templates"
                                   formatting @@ "templates/reference" ]))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  ensureDirectory (output @@ "content")
  CopyRecursive (formatting @@ "styles") (output @@ "content") true
    |> Log "Copying styles and scripts: "

let binaries =
    let manuallyAdded =
        referenceBinaries
        |> List.map (fun b -> bin @@ b)

    let conventionBased =
        directoryInfo bin
        |> subDirectories
        |> Array.map (fun d -> d.FullName @@ (sprintf "%s.dll" d.Name))
        |> List.ofArray

    // conventionBased @ manuallyAdded
    manuallyAdded

let libDirs =
    let conventionBasedbinDirs =
        directoryInfo bin
        |> subDirectories
        |> Array.map (fun d -> d.FullName)
        |> List.ofArray

    conventionBasedbinDirs @ [bin]

// Build API reference from XML comments
let buildReference () =
  CleanDir (output @@ "reference")
  // MetadataFormat.Generate
  //   ( binaries, output @@ "reference", layoutRootsAll.["en"],
  //     parameters = ("root", root)::info,
  //     sourceRepo = "",
  //     sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
  //     publicOnly = true,libDirs = libDirs )
  Literate.ProcessScriptFile
    ("../../src/Iris/Iris/Core/Either.fs",
     templates @@ docTemplate,
     output @@ "reference" @@ "Either.html",
     replacements = info)

copyFiles()
buildReference()
