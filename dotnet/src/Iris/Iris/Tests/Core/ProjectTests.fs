namespace Iris.Tests

open System
open System.IO
open System.Linq
open System.Threading
open Fuchu
open Fuchu.Test
open Iris.Core
open LibGit2Sharp

module Project =
  let signature =
    new Signature("Karsten Gebbert", "karsten@nsynk.de", new DateTimeOffset(DateTime.Now))
  //   _                    _    ______
  //  | |    ___   __ _  __| |  / / ___|  __ ___   _____
  //  | |   / _ \ / _` |/ _` | / /\___ \ / _` \ \ / / _ \
  //  | |__| (_) | (_| | (_| |/ /  ___) | (_| |\ V /  __/
  //  |_____\___/ \__,_|\__,_/_/  |____/ \__,_| \_/ \___|ed
  //
  let loadSaveTest =
    testCase "Save/Load Project should render equal project values" <|
      fun _ ->
        let name = "test1"
        let path = Path.Combine(Directory.GetCurrentDirectory(),"tmp", name)

        let (commit, project) =
          { Project.Create name with Path = Some(path) }
          |> save signature "Initial project save."
          |> Option.get

        let result = Project.Load(path + (sprintf "/%s.iris" name))
        expect "Projects should be loaded" true Option.isSome result

        let loaded = Option.get result

        expect "Projects should be structurally equal" true ((=) project) loaded
        expect "Project should have an Id" true ((>) (string project.Id |> String.length)) 0
        expect "Projects should have same Id" true ((=) project.Id) loaded.Id

  //    ____          _                  _             _
  //   / ___|   _ ___| |_ ___  _ __ ___ (_)_______  __| |
  //  | |  | | | / __| __/ _ \| '_ ` _ \| |_  / _ \/ _` |
  //  | |__| |_| \__ \ || (_) | | | | | | |/ /  __/ (_| |
  //   \____\__,_|___/\__\___/|_| |_| |_|_/___\___|\__,_| load/saved
  //
  let testCustomizedCfg =
    testCase "Save/Load of Project with customized configs should render structurally equal values" <|
      fun _ ->
        let name = "test2"
        let path = Path.Combine(Directory.GetCurrentDirectory(),"tmp", name)

        let engineCfg = RaftConfig.Default

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
          { Id        = Guid.Create()
          ; Name      = "Nice Display"
          ; Size      = Rect (1280,1080)
          ; Signals   =
              [{ Size     = Rect (500,500)
               ; Position = Coordinate (0,0)
               };
               { Size     = Rect (800,800)
               ; Position = Coordinate (29, 13)
               }]
          ; RegionMap =
            {
              SrcViewportId = Guid.Create()
              Regions =
                [{ Id             = Guid.Create()
                 ; Name           = "A Cool Region"
                 ; SrcPosition    = Coordinate (0,0)
                 ; SrcSize        = Rect       (50,50)
                 ; OutputPosition = Coordinate (50,50)
                 ; OutputSize     = Rect       (100,100)
                 };
                 { Id             = Guid.Create()
                 ; Name           = "Another Cool Region"
                 ; SrcPosition    = Coordinate (8,67)
                 ; SrcSize        = Rect       (588,5130)
                 ; OutputPosition = Coordinate (10,5300)
                 ; OutputSize     = Rect       (800,900)
                 }]
            }
          }

        let display2 =
          { Id        = Guid.Create()
          ; Name      = "Cool Display"
          ; Size      = Rect (180,12080)
          ; Signals   =
              [{ Size     = Rect (800,200)
               ; Position = Coordinate (3,8)
               };
               { Size     = Rect (1800,8800)
               ; Position = Coordinate (2900, 130)
               }]
          ; RegionMap =
            {
              SrcViewportId = Guid.Create();
              Regions =
                [{ Id             = Guid.Create()
                 ; Name           = "One Region"
                 ; SrcPosition    = Coordinate (0,8)
                 ; SrcSize        = Rect       (50,52)
                 ; OutputPosition = Coordinate (53,50)
                 ; OutputSize     = Rect       (103,800)
                 };
                 { Id             = Guid.Create()
                 ; Name           = "Premium Region"
                 ; SrcPosition    = Coordinate (8333,897)
                 ; SrcSize        = Rect       (83,510)
                 ; OutputPosition = Coordinate (1580,50)
                 ; OutputSize     = Rect       (1800,890)
                 }]
            }
          }

        let viewPort1 =
          { Id             = Guid.Create()
          ; Name           = "One fine viewport"
          ; Position       = Coordinate (22,22)
          ; Size           = Rect       (666,666)
          ; OutputPosition = Coordinate (0,0)
          ; OutputSize     = Rect       (98327,121)
          ; Overlap        = Rect       (0,0)
          ; Description    = "Its better than bad, its good."
          }

        let viewPort2 =
          { Id             = Guid.Create()
          ; Name           = "Another fine viewport"
          ; Position       = Coordinate (82,2)
          ; Size           = Rect       (466,86)
          ; OutputPosition = Coordinate (12310,80)
          ; OutputSize     = Rect       (98,89121)
          ; Overlap        = Rect       (0,33)
          ; Description    = "Its awesome actually"
          }

        let task1 =
          { Id             = Guid.Create()
          ; Description    = "A very important task, indeed."
          ; DisplayId      = Guid.Create()
          ; AudioStream    = "hm"
          ; Arguments      = [("key", "to you heart")]
          }

        let task2 =
          { Id             = Guid.Create()
          ; Description    = "yay, its another task"
          ; DisplayId      = Guid.Create()
          ; AudioStream    = "hoho"
          ; Arguments      = [("mykey", "to my heart")]
          }

        let nodeA =
          { Id       = Guid.Create()
          ; HostName = "moomoo"
          ; Ip       = IpAddress.Parse "182.123.18.2"
          ; Task     = Guid.Create()
          }

        let nodeB =
          { Id       = Guid.Create()
          ; HostName = "taataaa"
          ; Ip       = IpAddress.Parse "118.223.8.12"
          ; Task     = Guid.Create()
          }

        let groupA =
          { Name    = "Group A"
          ; Members = [ Guid.Create() ]
          }

        let groupB =
          { Name    = "Group B"
          ; Members = [ Guid.Create() ]
          }

        let cluster =
          { Name   = "A mighty cool cluster"
          ; Nodes  = [ nodeA;  nodeB  ]
          ; Groups = [ groupA; groupB ]
          }

        let project =
          let prj = Project.Create name
          { prj with
              Path = Some(path)
              Config =
                { prj.Config with
                    RaftConfig    = engineCfg
                    PortConfig    = portCfg
                    VvvvConfig    = vvvvCfg
                    ViewPorts     = [ viewPort1; viewPort2 ]
                    Displays      = [ display1;  display2  ]
                    Tasks         = [ task1;     task2     ]
                    ClusterConfig = cluster } }

        let saved = project.Save(signature, "Initial project save.") |> ignore

        let loaded =
          Project.Load(path + sprintf "/%s.iris" name)
          |> Option.get

        // the only difference will be the automatically assigned timestamp

        expect "Projects should be structurally equal" true ((=) loaded) saved

  //    ____ _ _
  //   / ___(_) |_
  //  | |  _| | __|
  //  | |_| | | |_
  //   \____|_|\__| initialzation
  //
  let saveInitsGit =
    testCase "Saved Project should be a git repository with yaml file." <|
      fun _ ->
        let name = "test3"
        let path = Path.Combine(Directory.GetCurrentDirectory(),"tmp", name)

        if Directory.Exists path
        then Directory.Delete(path, true) |> ignore

        let project =
          { Project.Create name with Path = Some(path) }
          |> save signature "Initial commit."

        let loaded =
          Path.Combine(path, sprintf "%s.iris" name)
          |> Project.Load
          |> Option.get

        expect "Projects should be a folder" true Directory.Exists path
        expect "Projects should be a git repo" true Directory.Exists (path + "/.git")
        expect "Projects should have project yml" true File.Exists (path + "/" + name + ".iris")
        expect "Projects should have repo" true (repository >> Option.isSome) loaded
        expect "Projects should not be dirty" false (repository >> Option.get >> isDirty) loaded
        expect "Projects should have one initial commit" 1 (repository >> Option.get >> commitCount) loaded

  //    ____                          _ _
  //   / ___|___  _ __ ___  _ __ ___ (_) |_ ___
  //  | |   / _ \| '_ ` _ \| '_ ` _ \| | __/ __|
  //  | |__| (_) | | | | | | | | | | | | |_\__ \
  //   \____\___/|_| |_| |_|_| |_| |_|_|\__|___/ per save
  //
  let savesMultipleCommits =
    testCase "Saved Project should be a git repository with yaml file." <|
      fun _ ->
        let name    = "test4"
        let author1 = "karsten"

        let path = Path.Combine(Directory.GetCurrentDirectory(),"tmp", name)

        if Directory.Exists path then
          Directory.Delete(path, true) |> ignore

        let msg1 = "Commit 1"

        let project =
          { Project.Create name with
              Path = Some(path)
              Author = Some(author1) }
          |> save signature msg1

        Path.Combine(path, sprintf "%s.iris" name)
        |> Project.Load
        |> Option.get
        |> (fun loaded ->
            let c = repository loaded |> Option.get |> commits |> elementAt 0
            Assert.Equal("Authors should be equal", true, (Option.get p.Author) = author1)
            Assert.Equal("Project should have one initial commit", true, (Option.get p.Repo).Commits.Count() = 1)
            Assert.Equal("Project should have commit message", true, c.MessageShort = msg1))

        let author2 = "ingolf"

        project.Author <- Some(author2)

        let msg2 = "Commit 2"
        project.Save(signature, msg2) |> ignore

        Path.Combine(path, sprintf "%s.iris" name)
        |> Project.Load
        |> Either.get
        |> (fun p ->
            let c1 = (Option.get p.Repo).Commits.ElementAt(0)
            let c2 = (Option.get p.Repo).Commits.ElementAt(1)
            Assert.Equal("Authors should be equal", true, (Option.get p.Author) = author2)
            Assert.Equal("Projects should two commits", true, (Option.get p.Repo).Commits.Count() = 2)
            Assert.Equal("Project should have current commit message at the start of the log", true, c1.MessageShort = msg2)
            Assert.Equal("Project should have old commit message at 2nd position", true, c2.MessageShort = msg1))

        let author3 = "eno"

        project.Author <- Some(author3)

        let msg3 = "Commit 3"
        project.Save(signature, msg3) |> ignore

        Path.Combine(path, sprintf "%s.iris" name)
        |> Project.Load
        |> Either.get
        |> (fun p ->
            let c1 = (Option.get p.Repo).Commits.ElementAt(0)
            let c2 = (Option.get p.Repo).Commits.ElementAt(1)
            let c3 = (Option.get p.Repo).Commits.ElementAt(2)
            Assert.Equal("Authors should be equal", true, (Option.get p.Author) = author3)
            Assert.Equal("Projects should have three commits", true, (Option.get p.Repo).Commits.Count() = 3)
            Assert.Equal("Project should have current commit message", true, c1.MessageShort = msg3)
            Assert.Equal("Project should have old commit message", true, c2.MessageShort = msg2)
            Assert.Equal("Project should have oldest commit message", true, c3.MessageShort = msg1))

  // For tests async stuff:
  //
  // let testTests =
  //   testCase "making a case" <| (timeout 1000
  //     (fun _ ->
  //       Thread.Sleep(900)
  //       failtest "nop"))

  [<Tests>]
  let configTests =
    testList "Config tests" [
        loadSaveTest
        testCustomizedCfg
        saveInitsGit
        savesMultipleCommits
      ]
