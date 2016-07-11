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

    static member Parse (str: string) = Guid str

    static member TryParse (str: string) = Guid str |> Some

    /// ## Create
    ///
    /// Create a new globally unique identifie
    ///
    /// ### Signature:
    /// - unit: unit
    ///
    /// Returns: Guid
    static member Create () =
#if JAVASCRIPT
      //      _                  ____            _       _
      //     | | __ ___   ____ _/ ___|  ___ _ __(_)_ __ | |_
      //  _  | |/ _` \ \ / / _` \___ \ / __| '__| | '_ \| __|
      // | |_| | (_| |\ V / (_| |___) | (__| |  | | |_) | |_
      //  \___/ \__,_| \_/ \__,_|____/ \___|_|  |_| .__/ \__|
      //                                          |_|

      Guid "JS GUID FIXME"
#else
      //    _   _ _____ _____
      //   | \ | | ____|_   _|
      //   |  \| |  _|   | |
      //  _| |\  | |___  | |
      // (_)_| \_|_____| |_|

      let stripEquals (str: string) =
        str.Substring(0, str.Length - 2)

      let guid = System.Guid.NewGuid()

      guid.ToByteArray()
      |> System.Convert.ToBase64String
      |> stripEquals
      |> Guid
#endif
