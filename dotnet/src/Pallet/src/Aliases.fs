namespace Pallet.Core

//   ____       _     _
//  / ___|_   _(_) __| |
// | |  _| | | | |/ _` |
// | |_| | |_| | | (_| |
//  \____|\__,_|_|\__,_|

type Guid = Guid of string
  with
    static member Create () =
      let stripEquals (str: string) =
        str.Substring(0, str.Length - 2)
      let guid = System.Guid.NewGuid()
      guid.ToByteArray()
      |> System.Convert.ToBase64String
      |> stripEquals
      |> Guid

type Long   = uint64
type Id     = Guid
type NodeId = Guid
type Index  = Long
type Term   = Long
