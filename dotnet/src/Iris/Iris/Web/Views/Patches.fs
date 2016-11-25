[<AutoOpen; RequireQualifiedAccess>]
module Iris.Web.Views.Patches

open Fable.Core
open Fable.Import
open Fable.Import.Browser

open Iris.Core
open Iris.Web.Core
open Iris.Web.Core.Html

type Root () =
  let mutable plugins = new Plugins ()

  let header (ctx : ClientContext) =
    Div [] [|
      H1 [] [| Text "All Patches" |]

      Button
        [ OnClick (fun _ -> ctx.Trigger(ClientMessage.Stop)) ]
        [| Text "Restart Worker" |]

      Button
        [ OnClick (fun _ ->
                   let cmd = StateMachine.Command AppCommand.Undo
                   ctx.Trigger(ClientMessage.Event(ctx.Session, cmd))) ]
        [| Text "Undo" |]

      Button
        [ OnClick (fun _ ->
                   let cmd = StateMachine.Command AppCommand.Redo
                   ctx.Trigger(ClientMessage.Event(ctx.Session, cmd))) ]
        [| Text "Redo" |]

      Button
        [ OnClick (fun _ ->
                    let cue : Cue  =
                      { Id      = Id.Create()
                      ; Name    = "New Cue"
                      ; IOBoxes = [| |] }
                    let ev = ctx.Session, AddCue cue
                    ctx.Trigger(ClientMessage.Event(ev))) ]
        [| Text "Create Cue" |]
    |]

  let footer = Div [ Class "foot"] [| Hr [] |]

  let ioboxView (context : ClientContext) (iobox : IOBox) : Html =
    if not (plugins.Has iobox)
    then plugins.Add iobox
           (fun _ ->
            ClientMessage.ClientLog "Bla bla iobox update wtf?"
            |> context.Trigger)

    let container =
      Li [] [|
        Strong [] [| Text iobox.Name |]
      |]

    match plugins.Get iobox with
      | Some(instance) -> container <+ Raw(instance.Render iobox)
      |  _             -> container

  let patchView (context : ClientContext) (patch : Patch) : Html =
    let container =
      Div [ ElmId (string patch.Id);  Class "patch" ] [|
        H3 [] [| Text "Patch:" |]
        P  [] [| Text patch.Name |]
      |]

    let children =
      patch.IOBoxes
      |> Seq.map (fun kv -> ioboxView context kv.Value)
      |> Seq.toArray

    container <++ children

  let patchList (context : ClientContext) (patches : Patch array) =
    let container = Div [ ElmId "patches" ] [| |]
    container <++  Array.map (patchView context) patches

  let mainView (context : ClientContext) (content : Html) : Html =
    Div [ ElmId "main" ] [|
      header context
      content
      footer
    |]

  let patches (context : ClientContext) (state : State)  : Html =
    if state.Patches.Count = 0 then
      P [] [| Text "Empty" |]
    else
      state.Patches
      |> Seq.toArray
      |> Array.map (fun kv -> kv.Value)
      |> patchList context

  let destroy (context : ClientContext) (cue : Cue) =
    let handler (_ : MouseEvent) =
        let ev = ClientMessage<_>.Event(context.Session, RemoveCue cue)
        context.Trigger(ev)

    Button [ OnClick handler ]
           [| Text "x" |]

  let cueView (context : ClientContext) (cue : Cue) =
    IrisCue
      [ ElmId (string cue.Id)
      ; Class "cue"
      ; OnPlay (fun _ -> printfn "hello. play!") ]
      [| P [ Class "id" ] [| Text (string cue.Id) |]
      ; P [ Class "name" ] [| Text (string cue.Name) |]
      ; destroy context cue |]

  let cueList (context : ClientContext) (cues : Cue array) : Html =
    Array.map (cueView context) cues
    |> Div [ ElmId "cues" ]

  let cues (context : ClientContext) (state : State) : Html =
    let cuelist =
      if state.Cues.Count = 0 then
        P [] [| Text "No cues" |]
      else
        state.Cues
        |> Seq.toArray
        |> Array.map (fun kv -> kv.Value)
        |> cueList context

    Div [] [|
      // Header
      H3 [] [| Text "Cues" |]
      cuelist
    |]

  let content (context : ClientContext) (state : State) : Html =
    Div [] [|
      patches context state
      Hr []
      cues context state
    |]

  (*-------------------- RENDERER --------------------*)

  interface IWidget<State,ClientContext> with
    member self.Dispose () = ()

    member self.Render (state : State) (context : ClientContext) =
      content context state
      |> mainView context
      |> renderHtml
