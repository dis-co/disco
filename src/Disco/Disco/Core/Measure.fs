(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

open System

#nowarn "42"

// * Extended Units of Measure

#if FABLE_COMPILER
// For some reason it seems these types are deriving from Object in Fable
// causing and error when the script is compiled. Just add the Erase attribute
// to remove these from the generated JS code.
[<MeasureAnnotatedAbbreviation; Fable.Core.Erase>]
type string<[<Measure>] 'Measure> = string

[<MeasureAnnotatedAbbreviation; Fable.Core.Erase>]
type bool<[<Measure>] 'Measure> = bool

[<MeasureAnnotatedAbbreviation; Fable.Core.Erase>]
type uint8<[<Measure>] 'Measure> = uint8

[<MeasureAnnotatedAbbreviation; Fable.Core.Erase>]
type uint16<[<Measure>] 'Measure> = uint16

[<MeasureAnnotatedAbbreviation; Fable.Core.Erase>]
type uint32<[<Measure>] 'Measure> = uint32

[<MeasureAnnotatedAbbreviation; Fable.Core.Erase>]
type uint64<[<Measure>] 'Measure> = uint64
#else
[<MeasureAnnotatedAbbreviation>]
type string<[<Measure>] 'Measure> = string

[<MeasureAnnotatedAbbreviation>]
type bool<[<Measure>] 'Measure> = bool

[<MeasureAnnotatedAbbreviation>]
type uint8<[<Measure>] 'Measure> = uint8

[<MeasureAnnotatedAbbreviation>]
type uint16<[<Measure>] 'Measure> = uint16

[<MeasureAnnotatedAbbreviation>]
type uint32<[<Measure>] 'Measure> = uint32

[<MeasureAnnotatedAbbreviation>]
type uint64<[<Measure>] 'Measure> = uint64
#endif

// * UoM

type UoM = class end
  with
    static member inline Wrap< ^W, ^t, ^tm when (^W or ^t) : (static member IsUoM : ^t * ^tm -> unit)> (t : ^t) : ^tm =
      #if FABLE_COMPILER
      unbox t
      #else
      (# "" t : ^tm #)
      #endif

    static member inline UnWrap< ^W, ^t, ^tm when (^W or ^t) : (static member IsUoM : ^t * ^tm -> unit)> (t : ^tm) : ^t =
      #if FABLE_COMPILER
      unbox t
      #else
      (# "" t : ^t #)
      #endif

    static member IsUoM(_ : string, _ : string<'Measure>) = ()
    static member IsUoM(_ : bool, _ : bool<'Measure>) = ()
    static member IsUoM(_ : uint8, _ : uint8<'Measure>) = ()
    static member IsUoM(_ : uint16, _ : uint16<'Measure>) = ()
    static member IsUoM(_ : uint32, _ : uint32<'Measure>) = ()
    static member IsUoM(_ : uint64, _ : uint64<'Measure>) = ()

[<AutoOpen>]
module UoM =
  let inline wrap (x : 't) : 'tm = UoM.Wrap<UoM, 't, 'tm> x
  let inline unwrap (x : 'tm) : 't = UoM.UnWrap<UoM, 't, 'tm> x

// * Ops

[<AutoOpen>]
module Ops =

  [<AutoOpen>]
  module Uint32 =
    let inline (.++.) (left: uint32<_>) (right: uint32<_>) : uint32<_> =
      let l:uint32 = unwrap left
      let r:uint32 = unwrap right
      wrap (l + r)

    let inline (.--.) (left: uint32<_>) (right: uint32<_>) : uint32<_> =
      let l:uint32 = unwrap left
      let r:uint32 = unwrap right
      wrap (l - r)



// Extending UoM to new types
//
// [<MeasureAnnotatedAbbreviation>]
// type Foo<[<Measure>] 'Measure> = Foo
// and Foo = Foo
// with
//     // Be *very* careful when writing this; bad args will result in invalid IL
//     static member IsUoM(_ : Foo, _ : Foo<'Measure>) = ()
