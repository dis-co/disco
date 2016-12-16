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
  let leader = Id "TEST_MACHINE" |> Member.create
  // let followers = List.init 4 (fun _ -> Id.Create() |> Node.create)
  // let nodes = leader::followers
  let members = [leader.Id, leader] |> Map
  let cluster =
    { ClusterConfig.Name = "my-cluster"
    ; Members = members
    ; Groups = [||] }

  // let stringBox = Pin.String(Id.Create(), "string", Id.Create(), [||], [|{ Index = 0u; Value = "one" }|])
  // let cue = { Cue.Id=Id.Create(); Name="MyCue"; Pins=[|stringBox|] }

  let project =
    let m = { MachineConfig.create() with MachineId = Id "TEST_MACHINE" }
    let p = Project.create path name m |> Either.get
    let p = Project.updateConfig { p.Config with Cluster = cluster } p
    Project.saveFile path "" signature "Initial project save." p
    |> Either.get
  ()

create @"C:\Users\Alfonso\Documents\GitHub\iris-sample-project"
//Environment.GetEnvironmentVariable "iris-sample-project"
//|> create
