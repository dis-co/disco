[<FunScript.JS>]
module Iris.Web.Transport

open FunScript
open FunScript.TypeScript
open FunScript.TypeScript.autobahn

let defaultConfig (url : string) =
  let cnf = createEmpty<autobahn.IConnectionOptions> ()
  cnf.url <- url
  cnf

let connect (url : string) =
  let cnf  = defaultConfig url
  let conn = autobahn.Connection.Create cnf
  conn
