namespace Iris.Core.Types

[<AutoOpen>]
[<ReflectedDefinition>]
module Project =

  (* ---------- Project ---------- *)
  type Project =
    { Name      : string
    ; Path      : FilePath
    ; Copyright : string
    ; Author    : string
    ; Year      : uint32
    }
