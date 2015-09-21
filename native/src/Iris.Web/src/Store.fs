[<FunScript.JS>]
module Iris.Web.Store

open FunScript
open FunScript.TypeScript
open FunScript.Core.Map

type PersonId  = PersonId of int
type Person    = { Name : string; Age : int }

type DataStore () = class
  member x.Dispatch _ = "hello"
end
