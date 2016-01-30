namespace Iris.Tests

open System.IO
open Fuchu
open Iris.Core.Types
open Iris.Core.Types.Config

module Project =
  //   _                    _    ______                  
  //  | |    ___   __ _  __| |  / / ___|  __ ___   _____ 
  //  | |   / _ \ / _` |/ _` | / /\___ \ / _` \ \ / / _ \
  //  | |__| (_) | (_| | (_| |/ /  ___) | (_| |\ V /  __/
  //  |_____\___/ \__,_|\__,_/_/  |____/ \__,_| \_/ \___|ed
  //                                                     
  let loadSaveTest =
    testCase "Save/Load Project should render equal project values" <|
      fun _ ->
        let name = "test"
        let path = "./tmp"

        let project = createProject name
        project.Path <- Some(path)
        saveProject project

        let project' =
          loadProject (path + "/test.iris")
          |> Option.get

        // the only difference will be the automatically assigned timestamp
        project.LastSaved <- project'.LastSaved

        Assert.Equal("Projects should be structurally equal", true, (project = project'))

  //    ____          _                  _             _ 
  //   / ___|   _ ___| |_ ___  _ __ ___ (_)_______  __| |
  //  | |  | | | / __| __/ _ \| '_ ` _ \| |_  / _ \/ _` |
  //  | |__| |_| \__ \ || (_) | | | | | | |/ /  __/ (_| |
  //   \____\__,_|___/\__\___/|_| |_| |_|_/___\___|\__,_| load/saved
  //                                                     
  let testCustomizedCfg =
    testCase "Save/Load of Project with customized configs should render structurally equal values" <|
      fun _ ->
        let name = "test"
        let path = "./tmp"

        let engineCfg = 
          { VsyncConfig.Default with
              Hosts      = Some(["localhost"; "otherhost"]);
              InfiniBand = Some true;
              TTL        = Some(0u);
              LogDir     = Some("otherplace")
            }

        let vvvvCfg =
          { VvvvConfig.Default with
              Executables =
                [{ Executable = "/pth/to/nowhere"
                 ; Version    = "0.0.0.0.0.0.1"
                 ; Required   = true
                 };
                 { Executable = "/antoher/path"
                 ; Version    = "1.2.34.4"
                 ; Required   = false
                 }]
            }

        let portCfg =
          { PortConfig.Default with
              WebSocket = 666u;
            }

        let display1 =
          { Id        = "02fa2b"
          ; Name      = "Nice Display"
          ; Size      = (1280,1080)
          ; Signals   =
              [{ Size     = (500,500)
               ; Position = (0,0)
               };
               { Size     = (800,800)
               ; Position = (29, 13)
               }]
          ; RegionMap =
            {
              SrcViewportId = "3fad6b";
              Regions =
                [{ Id             = "f90121"
                 ; Name           = "A Cool Region"
                 ; SrcPosition    = (0,0)
                 ; SrcSize        = (50,50)
                 ; OutputPosition = (50,50)
                 ; OutputSize     = (100,100)
                 };
                 { Id             = "alskdjflaskd"
                 ; Name           = "Another Cool Region"
                 ; SrcPosition    = (8,67)
                 ; SrcSize        = (588,5130)
                 ; OutputPosition = (10,5300)
                 ; OutputSize     = (800,900)
                 }]
            }
          }

        let display2 =
          { Id        = "209f2"
          ; Name      = "Cool Display"
          ; Size      = (180,12080)
          ; Signals   =
              [{ Size     = (800,200)
               ; Position = (3,8)
               };
               { Size     = (1800,8800)
               ; Position = (2900, 130)
               }]
          ; RegionMap =
            {
              SrcViewportId = "i1203r";
              Regions =
                [{ Id             = "1al0b2312"
                 ; Name           = "One Region"
                 ; SrcPosition    = (0,8)
                 ; SrcSize        = (50,52)
                 ; OutputPosition = (53,50)
                 ; OutputSize     = (103,800)
                 };
                 { Id             = "rrrr1331"
                 ; Name           = "Premium Region"
                 ; SrcPosition    = (8333,897)
                 ; SrcSize        = (83,510)
                 ; OutputPosition = (1580,50)
                 ; OutputSize     = (1800,890)
                 }]
            }
          }

        let viewPort1 =
          { Id             = "23f09a8f"
          ; Name           = "One fine viewport"
          ; Position       = (22,22)
          ; Size           = (666,666)
          ; OutputPosition = (0,0)
          ; OutputSize     = (98327,121)
          ; Overlap        = (0,0)
          ; Description    = "Its better than bad, its good."
          }

        let viewPort2 =
          { Id             = "akjsdlfksj"
          ; Name           = "Another fine viewport"
          ; Position       = (82,2)
          ; Size           = (466,86)
          ; OutputPosition = (12310,80)
          ; OutputSize     = (98,89121)
          ; Overlap        = (0,33)
          ; Description    = "Its awesome actually"
          }

        let task1 =
          { Id             = "9213f22"
          ; Description    = "A very important task, indeed."
          ; DisplayId      = "02fa2b"
          ; AudioStream    = "hm"
          ; Arguments      = [("key", "to you heart")]
          }

        let task2 =
          { Id             = "a929132"
          ; Description    = "yay, its another task"
          ; DisplayId      = "02fa2b"
          ; AudioStream    = "hoho"
          ; Arguments      = [("mykey", "to my heart")]
          }

        let nodeA =
          { Id       = "10f23r1"
          ; HostName = "moomoo"
          ; Ip       = "182.123.18.2"
          ; Task     = "9213f22"
          }

        let nodeB =
          { Id       = "af2hmse"
          ; HostName = "taataaa"
          ; Ip       = "118.223.8.12"
          ; Task     = "9213f22"
          }

        let groupA =
          { Name    = "Group A"
          ; Members = [ "10f23r1" ]
          }

        let groupB =
          { Name    = "Group B"
          ; Members = [ "af2hmse" ]
          }

        let cluster =
          { Name   = "A mighty cool cluster"
          ; Nodes  = [ nodeA;  nodeB  ]
          ; Groups = [ groupA; groupB ]
          }

        let project = createProject name
        project.Path <- Some(path)

        project.Config <-
          { project.Config with
              Engine    = engineCfg
              Port      = portCfg
              Vvvv      = vvvvCfg
              ViewPorts = [ viewPort1; viewPort2 ]
              Displays  = [ display1;  display2  ]
              Tasks     = [ task1;     task2     ]
              Cluster   = cluster
          }

        saveProject project

        let project' =
          loadProject (path + "/test.iris")
          |> Option.get

        // the only difference will be the automatically assigned timestamp
        project.LastSaved <- project'.LastSaved

        Assert.Equal("Projects should be structurally equal", true, (project = project'))

  //    ____ _ _   
  //   / ___(_) |_ 
  //  | |  _| | __|
  //  | |_| | | |_ 
  //   \____|_|\__| initialzation
  //               
  let createInitsGit =
    testCase "Saved Project should be a git repository with yaml file." <|
      fun _ ->
        let name = "test"
        let path = "./tmp"

        if Directory.Exists path
        then Directory.Delete(path, true) |> ignore

        let project = createProject name
        project.Path <- Some(path)
        saveProject project

        Assert.Equal("Projects should be a folder", true, Directory.Exists path)
        Assert.Equal("Projects should be a git repo", true, Directory.Exists    (path + "/.git"))
        Assert.Equal("Projects should have project yml", true, File.Exists (path + "/" + name + ".iris"))
        Assert.Equal("Projects should not be dirty", true, false)
        Assert.Equal("Projects should have one initial commit", true, false)

  
  [<Tests>]
  let configTests =
    testList "Config tests" [
        loadSaveTest
        testCustomizedCfg
        createInitsGit
      ]
