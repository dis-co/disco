open FSharp.Data
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Net
open Iris.Core.Types
open Iris.Core.Couch

(* iriscouch pw: 1riS *)

let hostOpt = "--host="
let projOpt = "--project="
let helpOpt = "--help"

let bailout (err:string) =
  printfn "Error:%s" err
  printfn "Bailing out."
  exit 1

let optMatches (pat:string) (opt:string) =
  let res = Regex.Match(opt, pat)
  if res.Success then true else false

let hasOption (attr:string) (xs:string list) =
  match List.tryFind (optMatches attr) xs with
    | Some(_) -> true
    | _       -> false

let helpRequested = hasOption helpOpt

let printHelp () =
  printfn "CouchImporter (nSynk GmbH, 2015)"
  printfn ""
  printfn "Import Iris project data into to a CouchDB instance."
  printfn ""
  printfn "Options:"
  printfn "    --help            show this help screen"
  printfn "    --project=<path>  import given project file"
  printfn "    --host=<url>      import on given CouchDB host (e.g. http://localhost:5984)"
  printfn ""
  printfn "In general, you are expected to setup and verify that you have a working"
  printfn "Iris installation. Imported projects retain their name."

let optionsValid xs =
  hasOption projOpt xs && hasOption hostOpt xs

let optionsInvalid () =
  printfn "Options invalid."
  printfn ""
  printfn "You must specify the --project and --host options!"
  printfn ""


let startUp xs =
  (* Explicitly call for help, or are you lazy? *)
  if helpRequested xs || List.isEmpty xs
  then
    printHelp ()
    exit 0

  (* Check validity of options passed-in *)
  if not <| optionsValid xs
  then
    optionsInvalid ()
    printHelp ()
    exit 0

let parseThing thing xs error = 
  let unparsed = List.find (optMatches thing) xs
  in match List.ofArray(unparsed.Split([|'='|])) with
     | _ :: m :: [] -> if String.length m > 0 // arguably it would be
                         then m               // nicer to use pattern matching but hey..
                         else bailout error
     | _ -> bailout error

let parseProject xs =
  let error = "You must supply a value to --project, e.g. --projec=./myproject.json"
  parseThing projOpt xs error

let parseHost xs =
  let error = "You must supply a value to --host, e.g. --host=http://localhost:5984"
  parseThing hostOpt xs error


[<Literal>]
let jsonSample = """
{
  "Updated": "2015-07-29T13:40:47.7744042+02:00",
  "Created": "2015-06-16T18:16:52.2366296+02:00",
  "CueLists": [
    {
      "Cues": [
        "82f011a1-bdf2-4647-9fa4-f1b23ae4e831"
      ],
      "Name": "Lamellen Open",
      "Id": "792f00ed-aefd-4c36-b8b5-e3711d405692"
    }
  ],
  "Cues": [
    {
      "Values": [
        {
          "Values": [
            {
              "Value": "htlo",
              "Behavior": 1
            },
            {
              "Value": 21,
              "Behavior": 2
            }
          ],
          "Target": "4b2659d4-57ec-482e-82ef-4e8cabaf1cde",
          "Type": 0
        }
      ],
      "Trigger": false,
      "Name": "L_Faecher",
      "Id": "2c0d0599-463e-4bd7-8c8f-ab6d405c2358"
    }
  ],
  "Patches": null,
  "FilePath": "C:\\MBIAA15\\Patches\\IRIS\\demo.json",
  "Dirty": false,
  "Name": "demo"
}
"""

[<Literal>]
let respSamp = """
{"ok":true,"id":"karsten","rev":"gebbert"}
"""

type ProjData = JsonProvider<jsonSample>
type CouchResp = JsonProvider<respSamp>

let sanitize (str:string) =
  let step1 = Regex.Replace(str, "/[^0-9a-zA-Z]/g", "_")
  Regex.Replace(step1, "/__+/g", "_")


let printStats (json:JsonProvider<jsonSample>.Root) = 
  printfn "Project Name: %s"    json.Name
  printfn "  # of Cues: %d"     json.Cues.Length
  printfn "  # of CueLists: %d" json.CueLists.Length


let mkCluster (json:JsonProvider<jsonSample>.Root) =
  let cluster = new Cluster()
  cluster.Name = new Name(json.Name + " Cluster") |> ignore
  cluster.Members = new List<ClusterMember>() |> ignore 
  cluster


let mkProject (json:JsonProvider<jsonSample>.Root) (id:IrisId) = 
  let database = (sanitize json.Name) + "_data"
  new Project( Name      = json.Name
             , Database  = database
             , ClusterId = id
             , Created   = json.Created 
             , Updated   = json.Updated )

(* I prefer Haskell-style APIs*)
let readFile = File.ReadAllText

let mkGet url = Http.Request (url, silentHttpErrors = true)

let mkPost url body =
  Http.Request
    ( url
    , headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ]
    , body = TextRequest body
    , silentHttpErrors = true)


let mkEmptyPut url =
  Http.Request
    ( url
    , httpMethod = "PUT"
    , silentHttpErrors = true )

let checkDB url =
  let resp = mkGet url
  match resp.StatusCode with
  | 200 -> true
  | _   -> false

let createDesign url (doc:DesignDoc) =
  let resp = mkPost url (doc.ToJSON())
  match resp.StatusCode with
    | 201 | 202 -> ()
    | _ -> bailout "could not create design document"

let mkDB url =
  let resp = mkEmptyPut url
  match resp.StatusCode with
  | 201 | 202 -> createDesign url (new CuesDesign())
                 createDesign url (new CueListsDesign())
  | _   -> bailout "Unable to create project data database"
    
let createProject url (cluster:Cluster) (json:JsonProvider<jsonSample>.Root) =
  let projecturl = url + "/projects"
  cluster.ToJSON()
  |> mkPost projecturl
  |> (fun resp ->
      match resp.Body with
      | Text(t) -> CouchResp.Parse(t)
      | _       -> bailout "This should never have happened.")
  |> (fun cresp -> mkProject json (new IrisId(cresp.Id)))
  |> (fun p -> p.ToJSON())
  |> mkPost projecturl
  |> (fun resp ->
      match resp.StatusCode with
      | 201 | 202 -> match resp.Body with
                     | Text(t) -> CouchResp.Parse(t)
                     | _       -> bailout "strange binary response from Couch!"
      | c         -> bailout "could not create project metadata: %d" c)
  |> (fun resp ->
      let url = projecturl + "/" + resp.Id
      let p = mkGet (url)
      match p.StatusCode with
      | 200 -> match p.Body with
               | Text(t) -> Project.FromJSON(t)
               | _       -> bailout "mmmmmm"
      | c -> bailout "this should not happen. ever.")

let castToObj (behavior:Behavior) (value:JsonProvider<jsonSample>.IntOrString) =
  match behavior with
  | Behavior.XSlider -> match value.JsonValue with
                        | JsonValue.Null -> 0 :> System.Object
                        | v              -> value.JsonValue.AsFloat() :> System.Object
  | Behavior.Toggle  -> value.JsonValue.AsBoolean() :> System.Object
  | _                -> value.JsonValue.AsString() :> System.Object

[<EntryPoint>]
let main argv =
  (* VALIDATION *)
  let args = List.ofArray argv
  startUp args

  (* OPTIONS PROCESSING *)
  let host     = parseHost args
  let filepath = parseProject args

  (* DATA *)
  let json = ProjData.Parse(readFile filepath)

  (* PROJECT DATABASE & METADATA DOCUMENTS *)
  let cluster = mkCluster json

  if not (checkDB (host + "/projects"))
  then bailout "Iris is not setup correctly. Project db missing"

  let project = createProject host cluster json
  let projDB = host + "/" + project.Database

  if not (checkDB projDB) then mkDB projDB

  (* PROCESSING CUES *)
  for old in json.Cues do
    let values = new List<CueValue>()

    for oldcueval in old.Values do
      // construct a list of pin values
      let pinvalues = new PinSlices()
      for value in oldcueval.Values do
        let behavior = enum<Behavior> value.Behavior
        let newval = castToObj behavior value.Value
        let pinval = new PinSlice(behavior, newval)
        pinvalues.Add(pinval)

      // construct a cueval with pin values
      let cueval = CueValue.PinTarget
                      ( new IrisId(oldcueval.Target.ToString())
                      , pinvalues)
       in values.Add(cueval)

    let cue =
      new Cue ( _id = old.Id.ToString()
              , Name = old.Name
              , Values = values
              , Project = project.Database )

    cue.ToJSON()
    |> mkPost projDB
    |> (fun resp ->
        match resp.Body with
        | Text(t) -> CouchResp.Parse(t)
        | _       -> bailout "This should never have happened.")
    |> (fun cresp -> printfn "%s" (cresp.ToString()))
    |> ignore

  exit 0

