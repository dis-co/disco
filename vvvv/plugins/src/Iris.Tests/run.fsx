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
open Iris.Tests.Raft

(*
   Working with libgit2 native libraries:
   
   - see ldd bin/Debug/NativeBinaries/linux/amd64/libgit...so for dependencies
   - set MONO_LOG_LEVEL=debug for more VM info
   - ln -s bin/Debug/NativeBinaries bin/Debug/libNativeBinaries
   - set LD_LIBRARY_PATH=....:/run/current-system/sw/lib/ 

   now it *should* work. YMMV.

   Good Fix: use a nix-shell environment that exposes LD_LIBRARY_PATH correctly.
*)

//run configTests
run raftTests
