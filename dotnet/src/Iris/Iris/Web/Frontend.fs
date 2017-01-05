module Iris.Web.Frontend

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Web.Lib

type ReactApp =
  abstract mount: StateInfo*((StateInfo->unit)->unit)->unit

let reactApp: ReactApp = importDefault "ReactApp"

let awaitObservable (picker: 'T->'U option) (observable: IObservable<'T>) =
  let mutable disp = Unchecked.defaultof<IDisposable>
  Async.FromContinuations(fun (cont,_,_) ->
      disp <- observable.Subscribe(fun ev ->
        match picker ev with
        | Some res ->
          disp.Dispose()
          cont(res)
        | None -> ()
  ))

ClientContext.Start()
|> Promise.iter (fun context ->
// TODO: Check if we really need the session id at this point,
// There's a risk that `ClientMessage.Initialized` goes missed here
// (e.g. if `ClientContext.Start` returns too late)

//  let! initInfo =
//    context.OnMessage
//    |> awaitObservable (function
//      | ClientMessage.Initialized id ->
//        Some { context = context; session = Session.Empty id; state = State.Empty }
//      | _ -> None)

  let initInfo =
    let session = Guid.NewGuid() |> string |> Id |> Session.Empty
    { context = context; session = session; state = State.Empty }
    
  reactApp.mount(initInfo, fun f ->
    context.OnMessage
    |> Observable.add (function
      | ClientMessage.Render state ->
        match Map.tryFind context.Session state.Sessions with
        | Some session ->
          f { context = context; session = session; state = state }
        | None -> ()
      | _ -> ())
  )

  registerKeyHandlers context
)



