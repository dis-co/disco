#r "../Iris/bin/Debug/Iris/Iris.Serialization.dll"
#r "../Iris/bin/Debug/Iris/SharpYaml.dll"
#r "../Iris/bin/Debug/Iris/LibGit2Sharp.dll"
#r "../Iris/bin/Debug/Iris/iris.exe"

open System
open LibGit2Sharp
open Iris.Core
open Iris.Raft

let private name = "iris-sample-project"
let private signature =
  new Signature("Karsten Gebbert", "karsten@nsynk.de", new DateTimeOffset(DateTime.Now))

let create(path: string) =
  let leader = Id "TEST_MACHINE" |> Node.create
  // let followers = List.init 4 (fun _ -> Id.Create() |> Node.create)
  // let nodes = leader::followers
  let nodes = [leader]
  let cluster =
    { Cluster.Name = "my-cluster"
    ; Nodes = nodes
    ; Groups = [] }

  // let stringBox = IOBox.String(Id.Create(), "string", Id.Create(), [||], [|{ Index = 0u; Value = "one" }|])
  // let cue = { Cue.Id=Id.Create(); Name="MyCue"; IOBoxes=[|stringBox|] }

  let (commit, project) =
    let m = { MachineConfig.create() with MachineId = Id "TEST_MACHINE" }
    let p = { Project.create name m with Path = path }
    Project.updateConfig { p.Config with ClusterConfig = cluster } p
    |> Project.save signature "Initial project save."
    |> Either.get
  ()

Environment.GetEnvironmentVariable "iris-sample-project"
|> create
