namespace Iris.Web

open FunScript
open FunScript.TypeScript
open FunScript.TypeScript.autobahn

[<FunScript.JS>]
module Transport = 
  let defaultConfig (url : string) =
    let cnf = createEmpty<autobahn.IConnectionOptions> ()
    cnf.url <- url
    cnf

  let connect (url : string) =
    let cnf  = defaultConfig url
    let conn = autobahn.Connection.Create cnf
    conn
