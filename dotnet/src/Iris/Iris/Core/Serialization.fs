namespace Iris.Core

/// Serialization Interface 

[<AutoOpen>]
module Serialization =

  //  _____                     _
  // | ____|_ __   ___ ___   __| | ___
  // |  _| | '_ \ / __/ _ \ / _` |/ _ \
  // | |___| | | | (_| (_) | (_| |  __/
  // |_____|_| |_|\___\___/ \__,_|\___|

  /// Using static constraints, we can enforce that given types must implement a
  /// member to convert itself into a byte buffer. The compiler will throw
  /// errors if we don't supply it with values that implement that member. This
  /// is like using an interface, with the added benefit that this solution does
  /// not require the interface type to be used in type signatures throughout
  /// the program. This is much cleaner, and makes up for the ugly signature and
  /// implementation here.
  let inline encode< ^T when ^T : (member Encode : unit -> byte array)> (value: ^T) : byte array = 
    (^T : (member Encode : unit -> byte array) value)

  //  ____                     _
  // |  _ \  ___  ___ ___   __| | ___
  // | | | |/ _ \/ __/ _ \ / _` |/ _ \
  // | |_| |  __/ (_| (_) | (_| |  __/
  // |____/ \___|\___\___/ \__,_|\___|

  /// Decode requires any passed type to implement a static member called
  /// Inflate that attempts converting a passed byte buffer into the type
  /// specifyed in the ambient code. If the conversion succeeds, the result is
  /// a value of type ^T wrappend in Some, else None. As with `encode` the
  /// benefits of this approach are clear. Additionally, since there is no way
  /// to enforce the presence of a static member on a type via intefaces, this
  /// great since it allows us to do just that.
  let inline decode< ^T when ^T : (static member Decode : byte array -> ^T option)> bytes : ^T option =
    (^T : (static member Decode : byte array -> ^T option) bytes)
