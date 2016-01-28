namespace Iris.Tests

open System.IO
open Fuchu
open Iris.Core.Types

module Project =
  let loadSaveTest =
    testCase "Save/Load Project should render equal project values" <|
      fun _ ->
        let name = "test"
        let path = "./tmp"

        Directory.CreateDirectory path |> ignore

        let project = createProject name
        project.Path <- Some(path)
        saveProject project
        let project' = loadProject (path + "/test.iris")

        Assert.Equal("Project should be found", true, Option.isSome project')
        Assert.Equal("Projects should be equal", project, Option.get project')

  [<Tests>]
  let configTests =
    testList "Config tests" [
        loadSaveTest
      ]
