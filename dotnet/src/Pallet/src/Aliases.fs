namespace Pallet.Core

open System.Text.RegularExpressions

//  ____        __ _   ___    _
// |  _ \ __ _ / _| |_|_ _|__| |
// | |_) / _` | |_| __|| |/ _` |
// |  _ < (_| |  _| |_ | | (_| |
// |_| \_\__,_|_|  \__|___\__,_|

type RaftId = RaftId of string
  with
    override self.ToString() =
      match self with | RaftId str -> str

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
      |> System.Convert.ToBase64String
      |> sanitize
      |> RaftId

type Long   = uint64
type Id     = RaftId
type NodeId = RaftId
type Index  = Long
type Term   = Long
