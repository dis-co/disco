namespace Iris.Core


type StateMachine =
  | Open
  | Save
  | Create
  | Close
  | AddClient
  | UpdateClient
  | RemoveClient
  | DataSnapshot
