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
      div <||> [|
        h1 <|> text "All Patches";
        button
           <@> onClick (fun _ -> ctx.Trigger(ClientMessage.Stop))
           <|> text "Restart Worker";
        button
           <@> onClick (fun _ -> ctx.Trigger(ClientMessage.Undo))
           <|> text "Undo";
        button
           <@> onClick (fun _ -> ctx.Trigger(ClientMessage.Redo))
           <|> text "Redo";
        button
           <@> onClick (fun _ ->
                        match ctx.Session with
                          | Some(session) ->
                             let ev = (session, CueEvent(Create, None))
                             in ctx.Trigger(ClientMessage.Event(ev))
                          | _ -> printfn "Cannot create cue. No worker?")
           <|> text "Create Cue";
        |]

    let footer = div <@> class' "foot" <|> hr

    let ioboxView (context : ClientContext) (iobox : IOBox) : Html =
      if not (plugins.Has iobox)
      then plugins.Add iobox
             (fun iobox' ->
              match context.Session with
                | Some(session) ->
                  ClientMessage.Event(session,IOBoxEvent(Update,iobox'))
                  |> context.Trigger
                | _ -> printfn "no worker session found.")

      let container = li <|> (strong <|> text (iobox.Name))

      match plugins.Get iobox with
        | Some(instance) -> container <|> Raw(instance.Render iobox)
        |  _             -> container

    let patchView (context : ClientContext) (patch : Patch) : Html =
      let container =
        div <@> id' patch.Id <@> class' "patch" <||>
          [| h3 <|> text "Patch:"
           ; p  <|> text (patch.Name)
           |]

      container <||> Array.map (ioboxView context) patch.IOBoxes

    let patchList (context : ClientContext) (patches : Patch array) =
      let container = div <@> id' "patches"
      container <||> Array.map (patchView context) patches

    let mainView (context : ClientContext) (content : Html) : Html =
      div <@> id' "main" <||> [| header context; content ; footer |]


    let patches (context : ClientContext) (state : State)  : Html =
      state.Patches
      |> (fun patches ->
            if Array.length patches = 0
            then p <|> text "Empty"
            else patchList context patches)


    let destroy (context : ClientContext) (cue : Cue) =
      let handler (_ : Event) = 
        match context.Session with
          | Some(session) -> 
            let ev = ClientMessage<State>.Event(session, CueEvent(Delete, Some(cue)))
            context.Trigger(ev)
          | _ -> printfn "Cannot delete cue. No Worker?"

      button <@> onClick handler
             <|> text "x"


    let cueView (context : ClientContext) (cue : Cue) =
      div <@> id' cue.Id <@> class' "cue"
          <|> text cue.Name
          <|> destroy context cue 

    let cueList (context : ClientContext) (cues : Cue array) : Html =
      div <@> id' "cues"
          <||> Array.map (cueView context) cues

    let cues (context : ClientContext) (state : State) : Html =
      div <||>
        [| h3 <|> text "Cues";
           state.Cues
           |> (fun cues ->
                 if Array.length cues = 0
                 then p <|> text "No cues"
                 else cueList context cues);
         |]

    let content (context : ClientContext) (state : State) : Html =
      div <||>
        [| patches context state
         ; hr
         ; cues context state
         |]
      
    (*-------------------- RENDERER --------------------*)

    interface IWidget<State,ClientContext> with
      member self.Dispose () = ()

      member self.Render (state : State) (context : ClientContext) =
        content context state
        |> mainView context
        |> renderHtml
