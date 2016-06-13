namespace Pallet.Core

open System

[<AutoOpen>]
module Aliases = 

  type Id     = Guid
  type Index  = uint32
  type Term   = uint32
  type NodeId = uint32
  type Err    = string

  let (|+) = (|>)

  let inline flip f b a = f a b
  let inline constant a _ = a
  let inline uncurry f (a,b) = f a b
