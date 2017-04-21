namespace Iris.Core

// * Imports

open System

#nowarn "42"

// * UoM

type UoM = class end
  with
    static member inline Wrap< ^W, ^t, ^tm when (^W or ^t) : (static member IsUoM : ^t * ^tm -> unit)> (t : ^t) =
      #if FABLE_COMPILER
      unbox t
      #else
      (# "" t : ^tm #)
      #endif

    static member inline UnWrap< ^W, ^t, ^tm when (^W or ^t) : (static member IsUoM : ^t * ^tm -> unit)> (t : ^tm) =
      #if FABLE_COMPILER
      unbox t
      #else
      (# "" t : ^t #)
      #endif

[<AutoOpen>]
module UoM =
  let inline wrap (x : 't) : 'tm = UoM.Wrap<UoM, 't, 'tm> x
  let inline unwrap (x : 'tm) : 't = UoM.UnWrap<UoM, 't, 'tm> x

// * Units of Measure

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

type UoM with
  // Be *very* careful when writing this; bad args will result in invalid IL
  static member IsUoM(_ : string, _ : string<'Measure>) = ()
  static member IsUoM(_ : bool, _ : bool<'Measure>) = ()
  static member IsUoM(_ : uint8, _ : uint8<'Measure>) = ()
  static member IsUoM(_ : uint16, _ : uint16<'Measure>) = ()
  static member IsUoM(_ : uint32, _ : uint32<'Measure>) = ()
  static member IsUoM(_ : uint64, _ : uint64<'Measure>) = ()

// Extending UoM to new types
//
// [<MeasureAnnotatedAbbreviation>]
// type Foo<[<Measure>] 'Measure> = Foo
// and Foo = Foo
// with
//     // Be *very* careful when writing this; bad args will result in invalid IL
//     static member IsUoM(_ : Foo, _ : Foo<'Measure>) = ()
