namespace Iris.Core

//  _____         _
// |_   _|_ _ ___| | __
//   | |/ _` / __| |/ /
//   | | (_| \__ \   <
//   |_|\__,_|___/_|\_\

type Argument = (string * string)

type Task =
  { Id             : Id
  ; Description    : string
  ; DisplayId      : Id
  ; AudioStream    : string
  ; Arguments      : Argument list
  }
