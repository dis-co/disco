/// Documentation for my library
///
/// ## Example
///
///     let h = Library.hello 1
///     printfn "%d" h
///
[<ReflectedDefinition>]
module Iris.Core.Types.Pin

/// Returns 42
///
/// ## Parameters
///  - `num` - whatever
let hello num = num

type Pin (Name : string) = class
  let mutable name = Name

  member this.Name
    with get () = name
    and  set n  = name <- n
end

