namespace Iris.Tests

open System.IO
open Fuchu
open Iris.Core.Types
open Iris.Core.Types.Config

module Project =
  let testVsyncConfig =
    testCase "Test engine config equality" <|
      fun _ ->
        let vs1 = VsyncConfig.Default
        let vs2 = VsyncConfig.Default

        Assert.Equal("Projects Cluster settings should be equal", true, (vs1 = vs2))
        

  let loadSaveTest =
    testCase "Save/Load Project should render equal project values" <|
      fun _ ->
        let name = "test"
        let path = "./tmp"

        Directory.CreateDirectory path |> ignore

        let project = createProject name
        project.Path <- Some(path)
        saveProject project

        let project' =
          loadProject (path + "/test.iris")
          |> Option.get

        // the only difference will be the automatically assigned timestamp
        project.LastSaved <- project'.LastSaved

        Assert.Equal("Projects should be structurally equal", true, (project = project'))

  [<Tests>]
  let configTests =
    testList "Config tests" [
        //testVsyncConfig
        loadSaveTest
      ]
