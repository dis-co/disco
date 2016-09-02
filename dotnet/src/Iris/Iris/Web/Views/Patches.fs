namespace Iris.Web.Views

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Patches =

  open Fable.Core
  open Fable.Import
  open Fable.Import.Browser

  open Iris.Web.Core
  open Iris.Core

  type Root () =
    let mutable plugins = new Plugins ()

    let header (ctx : ClientContext) =
      Div [] [|
        H1 [] [| Text "All Patches" |]

        Button
          [ OnClick (fun _ -> ctx.Trigger(ClientMessage.Stop)) ]
          [| Text "Restart Worker" |]

        Button
          [ OnClick (fun _ -> ctx.Trigger(ClientMessage.Undo)) ]
          [| Text "Undo" |]

        Button
          [ OnClick (fun _ -> ctx.Trigger(ClientMessage.Redo)) ]
          [| Text "Redo" |]

        Button
          [ OnClick (fun _ ->
                        match ctx.Session with
                        | Some(session) ->
                          let cue : Cue  =
                            { Id      = Id.Create()
                            ; Name    = "New Cue"
                            ; IOBoxes = [| |] }
                          let ev = session, CueEvent(Create, cue)
                          ctx.Trigger(ClientMessage.Event(ev))
                        | _ -> printfn "Cannot create cue. No worker?") ]
          [| Text "Create Cue" |]
      |]

    let footer = Div [ Class "foot"] [| Hr [] |]

    let ioboxView (context : ClientContext) (iobox : IOBox) : Html =
      if not (plugins.Has iobox)
      then plugins.Add iobox
             (fun iobox' ->
              match context.Session with
                | Some(session) ->
                  ClientMessage.Event(session,IOBoxEvent(Update,iobox'))
                  |> context.Trigger
                | _ -> printfn "no worker session found.")

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

      container <++ Array.map (ioboxView context) patch.IOBoxes

    let patchList (context : ClientContext) (patches : Patch array) =
      let container = Div [ ElmId "patches" ] [| |]
      container <++ Array.map (patchView context) patches

    let mainView (context : ClientContext) (content : Html) : Html =
      Div [ ElmId "main" ] [|
        header context
        content
        footer
      |]

    let patches (context : ClientContext) (state : State)  : Html =
      state.Patches
      |> (fun patches ->
            if Array.isEmpty patches
            then P [] [| Text "Empty" |]
            else patchList context patches)

    let destroy (context : ClientContext) (cue : Cue) =
      let handler (_ : MouseEvent) =
        match context.Session with
          | Some(session) ->
            let ev = ClientMessage<State>.Event(session, CueEvent(Delete, cue))
            context.Trigger(ev)
          | _ -> printfn "Cannot delete cue. No Worker?"

      Button [ OnClick handler ]
             [| Text "x" |]

    let cueView (context : ClientContext) (cue : Cue) =
      Div [ ElmId (string cue.Id); Class "cue" ]
          [| Text (string cue.Id)
          ; destroy context cue |]

    let cueList (context : ClientContext) (cues : Cue array) : Html =
      Array.map (cueView context) cues
      |> Div [ ElmId "cues" ]

    let cues (context : ClientContext) (state : State) : Html =
      Div [] [|
        // Header
        H3 [] [| Text "Cues" |]

        // List of cues
        state.Cues
        |> (fun cues ->
              if Array.isEmpty cues then
                P [] [| Text "No cues" |]
              else
                cueList context cues);
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
