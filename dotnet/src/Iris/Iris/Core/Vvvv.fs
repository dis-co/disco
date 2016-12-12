namespace Iris.Core

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

// __     __
// \ \   / /_   ____   ____   __
//  \ \ / /\ \ / /\ \ / /\ \ / /
//   \ V /  \ V /  \ V /  \ V /
//    \_/    \_/    \_/    \_/

type VvvvExe =
  { Executable : FilePath
    Version    : Version
    Required   : bool }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let path = builder.CreateString self.Executable
    let version = builder.CreateString self.Version

    VvvvExeFB.StartVvvvExeFB(builder)
    VvvvExeFB.AddExecutable(builder, path)
    VvvvExeFB.AddVersion(builder,version)
    VvvvExeFB.AddRequired(builder, self.Required)
    VvvvExeFB.EndVvvvExeFB(builder)

type VvvvPlugin =
  { Name : Name
    Path : FilePath }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = builder.CreateString self.Name
    let path = builder.CreateString self.Path

    VvvvPluginFB.StartVvvvPluginFB(builder)
    VvvvPluginFB.AddName(builder,name)
    VvvvPluginFB.AddPath(builder,path)
    VvvvPluginFB.EndVvvvPluginFB(builder)
