namespace Iris.Core

open System
open System.Text.RegularExpressions
open Fable.Core

//   ____       _     _
//  / ___|_   _(_) __| |
// | |  _| | | | |/ _` |
// | |_| | |_| | | (_| |
//  \____|\__,_|_|\__,_|

[<Erase>]
type Guid =
  | Guid of string

  with
    override guid.ToString() =
      match guid with | Guid str -> str

    static member Parse (str: string) = Guid str

    static member TryParse (str: string) = Guid str |> Some

    /// ## Create
    ///
    /// Create a new Guid.
    ///
    /// ### Signature:
    /// - unit: .
    ///
    /// Returns: Guid
    static member Create () =
      let sanitize (str: string) =
        Regex.Replace(str, "[\+|\/|\=]","").ToLower()

      let guid = System.Guid.NewGuid()
      guid.ToByteArray()
      |> Convert.ToBase64String
      |> sanitize
      |> Guid
