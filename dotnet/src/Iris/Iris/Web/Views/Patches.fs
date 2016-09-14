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
          [ OnClick (fun _ ->
                     let cmd = StateMachine.Command AppCommand.Undo
                     match ctx.Session with
                     | Some session -> ctx.Trigger(ClientMessage.Event(session, cmd))
                     | _            -> printfn "No session active. Worker not running?") ]
          [| Text "Undo" |]

        Button
          [ OnClick (fun _ ->
                     let cmd = StateMachine.Command AppCommand.Redo
                     match ctx.Session with
                     | Some session -> ctx.Trigger(ClientMessage.Event(session, cmd))
                     | _            -> printfn "No session active. Worker not running??") ]
          [| Text "Redo" |]

        Button
          [ OnClick (fun _ ->
                        match ctx.Session with
                        | Some(session) ->
                          let cue : Cue  =
                            { Id      = Id.Create()
                            ; Name    = "New Cue"
                            ; IOBoxes = [| |] }
                          let ev = session, AddCue cue
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
                  ClientMessage.ClientLog "Bla bla iobox update wtf?"
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
        match context.Session with
          | Some(session) ->
            let ev = ClientMessage<State>.Event(session, RemoveCue cue)
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
