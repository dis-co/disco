namespace Iris.Core

open System

//   ____       _     _
//  / ___|_   _(_) __| |
// | |  _| | | | |/ _` |
// | |_| | |_| | | (_| |
//  \____|\__,_|_|\__,_|

type Guid =
  | Guid of string

  with
    override guid.ToString() =
      match guid with | Guid str -> str

    static member Parse (str: string) =
      System.Guid.Parse str |> ignore
      Guid str
  
    static member TryParse (str: string) =
      try 
        System.Guid.Parse str |> ignore
        Some (Guid str)
      with
        | _ -> None
