#I @"./bin/Debug/"
#r @"./bin/Debug/SharpYaml.dll"
#r @"./bin/Debug/FsCheck.dll"
#r @"./bin/Debug/FsPickler.dll"
#r @"./bin/Debug/Vsync.dll"
#r @"./bin/Debug/Iris.Core.dll"
#r @"./bin/Debug/FSharp.Configuration.dll"
#r @"./bin/Debug/Fuchu.dll"
#r @"./bin/Debug/Iris.Tests.dll"

open Fuchu
open Iris.Tests.Project

run configTests
