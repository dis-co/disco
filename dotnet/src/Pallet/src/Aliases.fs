namespace Pallet.Core

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
      let stripEquals (str: string) =
        str.Substring(0, str.Length - 2)

      let guid = System.Guid.NewGuid()

      guid.ToByteArray()
      |> System.Convert.ToBase64String
      |> stripEquals
      |> RaftId

type Long   = uint64
type Id     = RaftId
type NodeId = RaftId
type Index  = Long
type Term   = Long
