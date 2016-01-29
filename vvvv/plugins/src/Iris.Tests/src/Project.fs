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

        printfn "project:\n%A" project.Author
        printfn "loaded:\n%A"  project'.Author

        Assert.Equal("Projects Name settings should be equal", true,
                     (project.Name = project'.Name))
        Assert.Equal("Projects Author settings should be equal", true,
                     (project.Author = project'.Author))
        Assert.Equal("Projects Path settings should be equal", true,
                     (project.Path = project'.Path))
        Assert.Equal("Projects Audio settings should be equal", true,
                     (project.Audio = project'.Audio))
        Assert.Equal("Projects Vvvv settings should be equal", true,
                     (project.Vvvv = project'.Vvvv))
        Assert.Equal("Projects Engine settings should be equal", true,
                     (project.Engine = project'.Engine))
        Assert.Equal("Projects Timing settings should be equal", true,
                     (project.Timing = project'.Timing))
        Assert.Equal("Projects Port settings should be equal", true,
                     (project.Port = project'.Port))
        Assert.Equal("Projects ViewPorts settings should be equal", true,
                     (project.ViewPorts = project'.ViewPorts))
        Assert.Equal("Projects Displays settings should be equal", true,
                     (project.Displays = project'.Displays))
        Assert.Equal("Projects Cluster settings should be equal", true,
                     (project.Cluster = project'.Cluster))

  [<Tests>]
  let configTests =
    testList "Config tests" [
        //testVsyncConfig
        loadSaveTest
      ]
