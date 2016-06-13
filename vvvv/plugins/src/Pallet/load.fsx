#I "./bin/Debug"
#r "Pallet.dll"

open Pallet.Core

let mkLog = 
  Log.empty
  |> Log.push (Guid.NewGuid()) 1u Normal "one"
  |> Log.push (Guid.NewGuid()) 1u Normal "two"
  |> Log.push (Guid.NewGuid()) 1u Normal "three"
  |> Log.push (Guid.NewGuid()) 1u Normal "four"
  |> Log.push (Guid.NewGuid()) 1u Normal "five"
  |> Log.push (Guid.NewGuid()) 1u Normal "six"
