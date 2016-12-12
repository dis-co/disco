namespace Iris.Core

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

//  _____         _
// |_   _|_ _ ___| | __
//   | |/ _` / __| |/ /
//   | | (_| \__ \   <
//   |_|\__,_|___/_|\_\

type Argument = (string * string)

type Task =
  { Id             : Id
    Description    : string
    DisplayId      : Id
    AudioStream    : string
    Arguments      : Argument array }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let desc = builder.CreateString self.Description
    let disp = builder.CreateString (string self.DisplayId)
    let audio = builder.CreateString self.AudioStream
    let args =
      self.Arguments
      |> Array.map (fun (x,y) -> builder.CreateString(sprintf "%s;%s" x y))
      |> fun offsets -> TaskFB.CreateArgumentsVector(builder,offsets)

    TaskFB.StartTaskFB(builder)
    TaskFB.AddId(builder,id)
    TaskFB.AddDescription(builder,desc)
    TaskFB.AddDisplayId(builder,disp)
    TaskFB.AddAudioStream(builder,audio)
    TaskFB.AddArguments(builder,args)
    TaskFB.EndTaskFB(builder)
