module rec Iris.Web.Widgets.CuePlayer

open System
open System.Collections.Generic
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open PinView
open Helpers

let [<Literal>] SELECTION_COLOR = "lightblue"

module private Helpers =
  type RCom = React.ComponentClass<obj>
  let touchesElement(el: Browser.Element, x: float, y: float): bool = importMember "../../../src/Util"

  let inline Class x = ClassName x
  let inline CustomKeyValue(k:string, v: obj):'a = !!(k,v)
  let inline (~%) x = createObj x

  let tryDic key value (dic: IDictionary<string, obj>) =
    match dic.TryGetValue(key) with
    | true, v when v = value -> true
    | _ -> false

  let findPin (pinId: Id) (state: IGlobalState) =
      match Map.tryFindPin pinId state.pinGroups with
      | Some pin -> pin
      | None -> failwithf "Cannot find pin with Id %O in GlobalState" pinId

  let findPinGroup (pinGroupId: Id) (state: IGlobalState) =
      match Map.tryFind pinGroupId state.pinGroups with
      | Some pinGroup -> pinGroup
      | None -> failwithf "Cannot find pin group with Id %O in GlobalState" pinGroupId

  let findCue (cueId: Id) (state: IGlobalState) =
      match Map.tryFind cueId state.cues with
      | Some cue -> cue
      | None -> failwithf "Cannot find cue with Id %O in GlobalState" cueId

  let cueMockup() =
    let cueList: CueList =
      { Id = Id.Create()
        Name = name "MockCueList"
        Groups = [||] }
    let cuePlayer =
      CuePlayer.create (name "MockCuePlayer") (Some cueList.Id)
    cueList, cuePlayer

  let updateSlicesValue (index: int) (value: obj) slices: Slices =
    match slices with
    | StringSlices(id, arr) -> StringSlices(id, Array.mapi (fun i el -> if i = index then value :?> string     else el) arr)
    | NumberSlices(id, arr) -> NumberSlices(id, Array.mapi (fun i el -> if i = index then value :?> double     else el) arr)
    | BoolSlices  (id, arr) -> BoolSlices  (id, Array.mapi (fun i el -> if i = index then value :?> bool       else el) arr)
    | ByteSlices  (id, arr) -> ByteSlices  (id, Array.mapi (fun i el -> if i = index then value :?> byte[]     else el) arr)
    | EnumSlices  (id, arr) -> EnumSlices  (id, Array.mapi (fun i el -> if i = index then value :?> Property   else el) arr)
    | ColorSlices (id, arr) -> ColorSlices (id, Array.mapi (fun i el -> if i = index then value :?> ColorSpace else el) arr)


  // TODO: Temporary solution, we should actually just call AddCue and the operation be done in the backend
  let updatePins (cue: Cue) (state: IGlobalState) =
    for slices in cue.Slices do
      let pin = findPin slices.Id state
      match slices, pin with
      | StringSlices (_, values), StringPin pin -> StringPin { pin with Values = values }
      | NumberSlices (_, values), NumberPin pin -> NumberPin { pin with Values = values }
      | BoolSlices   (_, values), BoolPin pin   -> BoolPin   { pin with Values = values }
      | ByteSlices   (_, values), BytePin pin   -> BytePin   { pin with Values = values }
      | EnumSlices   (_, values), EnumPin pin   -> EnumPin   { pin with Values = values }
      | ColorSlices  (_, values), ColorPin pin  -> ColorPin  { pin with Values = values }
      | _ -> failwithf "Slices and pin types don't match\nSlices: %A\nPin: %A\nCue Id: %O" slices pin cue.Id
      |> UpdatePin |> ClientContext.Singleton.Post

  let printCueList (cueList: CueList) =
    for group in cueList.Groups do
      printfn "CueGroup: %O (%O)" group.Name group.Id
      for cueRef in group.CueRefs do
        printfn "    CueRef: %O" cueRef.Id

  let inline withStyle (width: int) (offset: int) =
    Style [
      CSSProp.Width (string width + "px")
      MarginLeft (string offset + "px")
      MarginRight 0
      Padding 0
    ]

  module Array =
    let inline replaceById< ^t when ^t : (member Id : Id)> (newItem : ^t) (ar: ^t[]) =
      Array.map (fun (x: ^t) -> if (^t : (member Id : Id) newItem) = (^t : (member Id : Id) x) then newItem else x) ar

    // let inline replaceById (newItem : CueGroup) (ar: CueGroup[]) =
    //   Array.map (fun (x: CueGroup) -> if newItem.Id = x.Id then newItem else x) ar

    let insertAfter (i: int) (x: 't) (xs: 't[]) =
      let len = xs.Length
      if len = 0 (* && i = 0 *) then
        [|x|]
      elif i >= len then
        failwith "Index out of array bounds"
      elif i = (len - 1) then
        Array.append xs [|x|]
      else
        let xs2 = Array.zeroCreate<'t> (len + 1)
        for j = 0 to len do
          if j <= i then
            xs2.[j] <- xs.[j]
          elif j = (i + 1) then
            xs2.[j] <- x
          else
            xs2.[j] <- xs.[j - 1]
        xs2

type [<Pojo>] private CueState =
  { Cue: Cue
    IsOpen: bool
    Name: Name
    Offset: string
    Time: string }

type [<Pojo>] private CueProps =
  { Key: string
    Global: IGlobalModel
    CueRef: CueReference
    CueGroup: CueGroup
    CueList: CueList
    CueIndex: int
    CueGroupIndex: int
    SelectedCueIndex: int
    SelectedCueGroupIndex: int
    SelectCue: int -> int -> unit }

type private CueView(props) =
  inherit React.Component<CueProps, CueState>(props)
  let disposables = ResizeArray<IDisposableJS>()
  let mutable selfRef = Unchecked.defaultof<Browser.Element>
  do
    let cue = findCue props.CueRef.CueId props.Global.state
    base.setInitState({Cue = cue; IsOpen = false; Name = cue.Name; Offset = "0000"; Time = "00:00:00" })

  member this.componentDidMount() =
    let globalModel = this.props.Global
    disposables.Add(globalModel.subscribe(!^(nameof globalModel.state.cues), fun _ dic ->
      if tryDic "Id" this.state.Cue.Id dic then
        let cue = Map.find this.state.Cue.Id globalModel.state.cues
        this.setState({this.state with Cue=cue; Name=cue.Name})
    ))
    disposables.Add(this.props.Global.subscribeToEvent("drag", fun (ev: IDragEvent<Pin>) _ ->
        if selfRef <> null then
          let mutable highlight = false
          if touchesElement(selfRef, ev.x, ev.y) then
            match ev.``type`` with
            | "move" ->
              highlight <- true
            | "stop" ->
              if this.state.Cue.Slices |> Array.exists (fun slices -> slices.Id = ev.model.Id) then
                failwith "The cue already contains this pin"
              let newCue = { this.state.Cue with Slices = Array.append this.state.Cue.Slices [|ev.model.Slices|] }
              UpdateCue newCue |> ClientContext.Singleton.Post
            | _ -> ()
          if highlight
          then selfRef.classList.add("iris-highlight-blue")
          else selfRef.classList.remove("iris-highlight-blue")
    ))

  member this.componentWillUnmount() =
    for d in disposables do
      d.dispose()

  member this.RenderInput(width: int, offset: int, content) =
    span [
      withStyle width offset
      ClassName "contentEditable"
    ] [str content]

  member this.render() =
    let arrowButton =
      button [
        ClassName ("icon uiControll " + (if this.state.IsOpen then "icon-less" else "icon-more"))
        withStyle 15 0
        OnClick (fun ev ->
          ev.stopPropagation()
          this.setState({ this.state with IsOpen = not this.state.IsOpen}))
      ] []
    let playButton =
      button [
        ClassName "icon icon-play"
        withStyle 15 0
        OnClick (fun ev ->
          ev.stopPropagation()
          updatePins this.state.Cue this.props.Global.state // TODO: Send CallCue event instead
        )
      ] []
    let autocallButton =
      button [
        ClassName "icon icon-autocall"
        withStyle 40 0
        OnClick (fun ev ->
          ev.stopPropagation()
          // Browser.window.alert("Auto call!")
        )
      ] []
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
      && this.props.CueIndex = this.props.SelectedCueIndex
    let cueHeader =
      li [
        Key this.props.Key
        // Style [MarginLeft 20.]
        OnClick (fun _ ->
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex
            || this.props.CueIndex <> this.props.SelectedCueIndex then
            this.props.SelectCue this.props.CueGroupIndex this.props.CueIndex  )
      ] [
        arrowButton
        playButton
        this.RenderInput(40, 10, "0000")
        this.RenderInput(140, 0, "Untitled")
        this.RenderInput(50, 20, "00:00:00")
        this.RenderInput(50, 20, "shortkey")
        autocallButton
      ]
    if not this.state.IsOpen then
      div [
        Ref (fun el -> selfRef <- el)
        Style [BackgroundColor (if isSelected then SELECTION_COLOR else "inherit")]
      ] [cueHeader]
    else
      let pinGroups =
        this.state.Cue.Slices
        |> Array.mapi (fun i slices -> i, findPin slices.Id this.props.Global.state, slices)
        |> Array.groupBy (fun (_, pin, _) -> pin.PinGroup)
        |> Array.map(fun (pinGroupId, pinAndSlices) ->
          let pinGroup = findPinGroup pinGroupId this.props.Global.state
          li [Key (string pinGroupId)] [
            yield div [Class "iris-row-label"] [str (unwrap pinGroup.Name)]
            for i, pin, slices in pinAndSlices do
              yield com<PinView,_,_>
                { key = string pin.Id
                  ``global`` = this.props.Global
                  pin = pin
                  slices = Some slices
                  update = Some(fun valueIndex value -> this.UpdateCueValue(i, valueIndex, value))
                  onDragStart = None } []
          ])
        |> Array.toList
      div [
        Ref (fun el -> selfRef <- el)
        Style [BackgroundColor (if isSelected then SELECTION_COLOR else "inherit")]
      ] [
        cueHeader
        li [] [ul [ClassName "iris-listSorted"] pinGroups]
      ]
      // div [
      //     Class "cueplayer-list-header cueplayer-cue level"
      //     Style [BackgroundColor (if isSelected then SELECTION_COLOR else "inherit")]
      //     OnClick (fun _ ->
      //       if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex
      //         || this.props.CueIndex <> this.props.SelectedCueIndex then
      //         this.props.SelectCue this.props.CueGroupIndex this.props.CueIndex  )
      //   ] [
      //     div [Class "level-left"] [
      //       div [Class "level-item"] [
      //         span [
      //           Class arrowIconClass
      //           OnClick (fun ev ->
      //             ev.stopPropagation()
      //             this.setState({this.state with IsOpen = not this.state.IsOpen}))
      //         ] []]
      //       div [
      //         Class "cueplayer-button iris-icon cueplayer-player level-item"
      //         OnClick (fun ev ->
      //           ev.stopPropagation()
      //           updatePins this.state.Cue this.props.Global.state) // TODO: Send CallCue event instead
      //       ] [
      //         span [Class "iris-icon iris-icon-play"] []
      //       ]
      //     ]
      //     form [Class "level-item"; OnSubmit (fun ev -> ev.preventDefault())] [
      //       input [
      //         Class "cueplayer-cueDesc"
      //         Type "text"
      //         Name "cueoffset"
      //         Value !^this.state.Offset
      //         OnChange (fun ev -> this.setState({this.state with Offset = !!ev.target?value}))
      //       ]
      //       br []
      //     ]
      //     form [Class "level-item"; OnSubmit (fun ev -> ev.preventDefault())] [
      //       input [
      //         Class "cueplayer-cueDesc"
      //         Type "text"
      //         Name "cuename"
      //         Value !^(unwrap this.state.Name: string)
      //         OnChange (fun ev -> this.setState({this.state with Name = !!ev.target?value}))
      //         OnBlur (fun _ ->
      //           { this.state.Cue with Name = this.state.Name } |> UpdateCue |> ClientContext.Singleton.Post)
      //         OnKeyUp (fun ev -> if ev.keyCode = 13. (* ENTER *) then !!ev.target?blur())
      //       ]
      //       br []
      //     ]
      //     form [Class "level-item"; OnSubmit (fun ev -> ev.preventDefault())] [
      //       input [
      //         Class "cueplayer-cueDesc"
      //         Style [
      //           CSSProp.Width 60.
      //           MarginRight 5.
      //         ]
      //         Type "text"
      //         Name "cuetime"
      //         Value !^this.state.Time
      //         OnChange (fun ev -> this.setState({this.state with Time = !!ev.target?value}))
      //       ]
      //       br []
      //     ]
      //     div [Class "level-right"] [
      //       div [Class "cueplayer-button iris-icon level-item"; OnClick (fun ev ->
      //           ev.stopPropagation()
      //           // Create new Cue and CueReference
      //           let newCue = { this.state.Cue with Id = Id.Create() }
      //           let newCueRef = { this.props.CueRef with Id = Id.Create(); CueId = newCue.Id }
      //           // Insert new CueRef in the selected CueGroup after the selected cue
      //           let cueGroup = this.props.CueList.Groups.[this.props.CueGroupIndex]
      //           let newCueGroup = { cueGroup with CueRefs = Array.insertAfter this.props.CueIndex newCueRef cueGroup.CueRefs }
      //           // Update the CueList
      //           let newCueList = { this.props.CueList with Groups = Array.replaceById newCueGroup this.props.CueList.Groups }
      //           // Send messages to backend
      //           AddCue newCue |>  ClientContext.Singleton.Post
      //           UpdateCueList newCueList |> ClientContext.Singleton.Post
      //       )] [
      //         span [Class "iris-icon iris-icon-duplicate"] []
      //       ]
      //       div [Class "cueplayer-button iris-icon cueplayer-close level-item"; OnClick (fun ev ->
      //         ev.stopPropagation()
      //         let id = this.props.CueRef.Id
      //         // Change selection if this item was selected
      //         if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex then
      //           this.props.SelectCue this.props.CueGroupIndex 0
      //         let cueGroup = { this.props.CueGroup with CueRefs = this.props.CueGroup.CueRefs |> Array.filter (fun c -> c.Id <> id) }
      //         { this.props.CueList with Groups = Array.replaceById cueGroup this.props.CueList.Groups }
      //         |> UpdateCueList |> ClientContext.Singleton.Post)
      //       ] [span [Class "iris-icon iris-icon-close"] []]
      //     ]
      //   ]
      // div [] [
      //   if this.state.IsOpen then
      //     let pinGroups =
      //       this.state.Cue.Slices
      //       |> Array.mapi (fun i slices -> i, findPin slices.Id this.props.Global.state, slices)
      //       |> Array.groupBy (fun (_, pin, _) -> pin.PinGroup)
      //     printfn "PinGroups %A" pinGroups
      //     for (pinGroupId, pinAndSlices) in pinGroups do
      //       let pinGroup = findPinGroup pinGroupId this.props.Global.state
      //       yield
      //         div [Key (string pinGroupId); Class "iris-pingroup"] [
      //           yield div [Class "iris-pingroup-name"] [str (unwrap pinGroup.Name + ":")]
      //           for i, pin, slices in pinAndSlices do
      //             yield com<PinView,_,_>
      //               { key = string pin.Id
      //                 ``global`` = this.props.Global
      //                 pin = pin
      //                 slices = Some slices
      //                 update = Some(fun valueIndex value -> this.UpdateCueValue(i, valueIndex, value))
      //                 onDragStart = None } []
      //         ]
      // ]
    // ]

  member this.UpdateCueValue(sliceIndex: int, valueIndex: int, value: obj) =
    let newSlices =
      this.state.Cue.Slices |> Array.mapi (fun i slices ->
        if i = sliceIndex then updateSlicesValue valueIndex value slices else slices)
    { this.state.Cue with Slices = newSlices } |> UpdateCue |> ClientContext.Singleton.Post

// type [<Pojo>] private CueGroupState =
//   { IsOpen: bool
//   ; Name: Name }

// type [<Pojo>] private CueGroupProps =
//   { key: string
//     Global: GlobalModel
//     CueGroup: CueGroup
//     CueList: CueList
//     CueGroupIndex: int
//     SelectedCueGroupIndex: int
//     SelectedCueIndex: int
//     SelectCueGroup: int -> unit
//     SelectCue: int -> int -> unit }

// type private CueGroupView(props) =
//   inherit React.Component<CueGroupProps, CueGroupState>(props)
//   do base.setInitState({IsOpen = true; Name = props.CueGroup.Name })

//   member this.render() =
//     let arrowIconClass =
//       if this.state.IsOpen
//       then "iris-icon iris-icon-caret-down-two"
//       else "iris-icon iris-icon-caret-right"
//     let isSelected =
//       this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
//     div [] [
//       div [
//         Class "cueplayer-list-header cueplayer-cue level"
//         Style [BackgroundColor (if isSelected then SELECTION_COLOR else "inherit")]
//         OnClick (fun _ ->
//           if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex then
//             this.props.SelectCueGroup this.props.CueGroupIndex)
//       ] [
//         div [Class "level-left"] [
//           div [Class "level-item"] [
//             span [
//               Class arrowIconClass
//               OnClick (fun ev ->
//                 ev.stopPropagation()
//                 this.setState({this.state with IsOpen = not this.state.IsOpen}))
//             ] []]
//           div [
//             Class "cueplayer-button iris-icon cueplayer-player level-item"
//             OnClick (fun ev ->
//               ev.stopPropagation()
//               // Fire all cues in the group
//               for cueRef in this.props.CueGroup.CueRefs do
//                 let cue = findCue cueRef.CueId this.props.Global.State
//                 updatePins cue this.props.Global.State
//             )
//           ] [
//             span [Class "iris-icon iris-icon-play"] []
//           ]
//           form [Class "level-item"; OnSubmit (fun ev -> ev.preventDefault())] [
//             input [
//               Class "cueplayer-cueDesc"
//               Type "text"
//               Name "cuegroupname"
//               Value !^(unwrap this.state.Name: string)
//               OnChange (fun ev -> this.setState({this.state with Name = !!ev.target?value}))
//               OnBlur (fun _ ->
//                 let newGroup = { this.props.CueGroup with Name = this.state.Name }
//                 { this.props.CueList with Groups = Array.replaceById newGroup this.props.CueList.Groups  }
//                 |> UpdateCueList |> ClientContext.Singleton.Post)
//               OnKeyUp (fun ev -> if ev.keyCode = 13. (* ENTER *) then !!ev.target?blur())
//             ]
//             span [] [str (sprintf "(%i)" this.props.CueGroup.CueRefs.Length)]
//             br []
//           ]
//         ]
//         div [Class "level-right"] [
//           div [Class "cueplayer-button iris-icon cueplayer-close level-item"; OnClick (fun ev ->
//             ev.stopPropagation()
//             let id = this.props.CueGroup.Id
//             // Change selection if this item was selected
//             if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex then
//               this.props.SelectCueGroup 0
//             { this.props.CueList with Groups = this.props.CueList.Groups |> Array.filter (fun c -> c.Id <> id) }
//             |> UpdateCueList |> ClientContext.Singleton.Post)
//           ] [span [Class "iris-icon iris-icon-close"] []]
//         ]
//       ]
//       div [
//         Style [
//           Display "flex"
//           FlexDirection "column"
//         ]
//       ] [
//         if this.state.IsOpen then
//           for i=0 to this.props.CueGroup.CueRefs.Length - 1 do
//             let cueRef = this.props.CueGroup.CueRefs.[i]
//             yield com<CueView,_,_>
//               { key = string cueRef.Id
//                 Global = this.props.Global
//                 CueRef = cueRef
//                 CueGroup = this.props.CueGroup
//                 CueList = this.props.CueList
//                 CueIndex = i
//                 CueGroupIndex = this.props.CueGroupIndex
//                 SelectedCueIndex = this.props.SelectedCueIndex
//                 SelectedCueGroupIndex = this.props.SelectedCueGroupIndex
//                 SelectCue = this.props.SelectCue }
//               []
//       ]
//     ]

type CuePlayerModel() =
  interface IWidgetModel with
    member __.view = typeof<CuePlayerView>
    member __.name = "Cue Player"
    member __.layout =
      { x = 0; y = 0;
        w = 8; h = 5;
        minW = 2; maxW = 10;
        minH = 1; maxH = 10; }

type [<Pojo>] CuePlayerState =
  { CueList: CueList option
    SelectedCueGroupIndex: int
    SelectedCueIndex: int }

type CuePlayerView(props) =
  inherit React.Component<IWidgetProps<CuePlayerModel>, CuePlayerState>(props)
  let disposables = ResizeArray<IDisposable>()
  let globalModel = props.``global`` :?> GlobalModel
  do
    // TODO: Mock code, create player if it doesn't exist
    if Map.count globalModel.State.cuePlayers = 0 then
      let cueList, cuePlayer = cueMockup()
      AddCueList cueList |> ClientContext.Singleton.Post
      AddCuePlayer cuePlayer |> ClientContext.Singleton.Post
      base.setInitState({ CueList = Some cueList; SelectedCueGroupIndex = 0; SelectedCueIndex = 0 })
    else
      // TODO: Use a dropdown to choose the player/list
      let cueList =
        Seq.tryHead globalModel.State.cuePlayers
        |> Option.bind (fun kv -> kv.Value.CueList)
        |> Option.bind (fun id -> Map.tryFind id globalModel.State.cueLists)
      base.setInitState({ CueList = cueList; SelectedCueGroupIndex = 0; SelectedCueIndex = 0 })

  member this.componentDidMount() =
    let state = globalModel.State
    disposables.Add(globalModel.Subscribe(!^[|nameof(state.cueLists); nameof(state.cuePlayers)|], fun _ dic ->
      match this.state.CueList with
      | Some cueList ->
        if tryDic (nameof cueList.Id) cueList.Id dic then
          let cueList = Map.find cueList.Id globalModel.State.cueLists
          this.setState({this.state with CueList=Some cueList})
      | None -> ()))

  member this.componentWillUnmount() =
    for d in disposables do
      d.Dispose()

  member this.render() =
    let inline labelAtts w o: IHTMLProp list =
      [ClassName "iris-list-label"; withStyle w o]
    let headers =
      li [
        Key "labels"
        ClassName "iris-list-label-row"
      ] [
        div (labelAtts 15 0) []
        div (labelAtts 15 0) []
        div (labelAtts 40 10) [str "Nr.:"]
        div (labelAtts 140 0) [str "Cue name"]
        div (labelAtts 50 20) [str "Delay"]
        div (labelAtts 50 20) [str "Shortkey"]
        div (labelAtts 40 0) [str "AutoCall"]
      ]
    ul [ClassName "iris-list"] (
      this.state.CueList
      // TODO: Temporarily assume just one group
      |> Option.bind (fun cueList -> Seq.tryHead cueList.Groups)
      |> function
        | Some group ->
          [ yield headers
            for i=0 to group.CueRefs.Length - 1 do
              let cueRef = group.CueRefs.[i]
              yield com<CueView,_,_>
                { Key = string cueRef.Id
                  Global = this.props.``global``
                  CueRef = cueRef
                  CueGroup = group
                  CueList = this.state.CueList.Value
                  CueIndex = i
                  CueGroupIndex = 0 //this.props.CueGroupIndex
                  SelectedCueIndex = this.state.SelectedCueIndex
                  SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
                  SelectCue = fun g c -> this.setState({this.state with SelectedCueGroupIndex = g; SelectedCueIndex = c }) }
                [] ]
        | None -> [headers]
    )
    // div [Class "cueplayer-container"] [
    //   // HEADER
    //   yield
    //     div [Class "cueplayer-list-header"] [
    //       div [Class "cueplayer-button cueplayer-go"] [
    //         span [
    //           Class "iris-icon"
    //           CustomKeyValue("data-icon", "c")
    //         ] [str "GO"]
    //       ]
    //       div [Class "cueplayer-button iris-icon"] [
    //         span [Class "iris-icon iris-icon-fast-backward"] []
    //       ]
    //       div [Class "cueplayer-button iris-icon"] [
    //         span [Class "iris-icon iris-icon-fast-forward"] []
    //       ]
    //       div [
    //         Class "cueplayer-button"
    //         OnClick (fun _ ->
    //           match this.state.CueList with
    //           | None -> failwith "There is no cue list available to add the cue"
    //           | Some cueList ->
    //             if cueList.Groups.Length = 0 then
    //               failwith "A Cue Group must be added first"
    //             // Create new Cue and CueReference
    //             let newCue = { Id = Id.Create(); Name = name "Untitled"; Slices = [||] }
    //             let newCueRef = { Id = Id.Create(); CueId = newCue.Id; AutoFollow = -1; Duration = -1; Prewait = -1 }
    //             // Insert new CueRef in the selected CueGroup after the selected cue
    //             let cueGroup = cueList.Groups.[this.state.SelectedCueGroupIndex]
    //             let newCueGroup = { cueGroup with CueRefs = Array.insertAfter this.state.SelectedCueIndex newCueRef cueGroup.CueRefs }
    //             // Update the CueList
    //             let newCueList = { cueList with Groups = Array.replaceById newCueGroup cueList.Groups }
    //             // Send messages to backend
    //             AddCue newCue |>  ClientContext.Singleton.Post
    //             UpdateCueList newCueList |> ClientContext.Singleton.Post
    //           )
    //       ] [str "Add Cue"]
    //       div [
    //         Class "cueplayer-button"
    //         OnClick (fun _ ->
    //           match this.state.CueList with
    //           | None -> failwith "There is no cue list available to add the group"
    //           | Some cueList ->
    //             // Create new CueGroup and insert it after the selected one
    //             let newCueGroup = { Id = Id.Create(); Name = name "Untitled"; CueRefs = [||] }
    //             let newCueList = { cueList with Groups = Array.insertAfter this.state.SelectedCueGroupIndex newCueGroup cueList.Groups }
    //             // Send messages to backend
    //             UpdateCueList newCueList |> ClientContext.Singleton.Post
    //           )
    //       ] [str "Add Group"]
    //       div [Style [Clear "both"]] []
    //     ]
    //   // CUE GROUPS
    //   match this.state.CueList with
    //   | None -> ()
    //   | Some cueList ->
    //     for i=0 to (cueList.Groups.Length-1) do
    //       let cueGroup = cueList.Groups.[i]
    //       yield com<CueGroupView,_,_>
    //         { key = (string cueGroup.Id) + ":" + (unwrap cueGroup.Name)
    //           Global = globalModel
    //           CueGroup = cueGroup
    //           CueList = cueList
    //           CueGroupIndex = i
    //           SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
    //           SelectedCueIndex = this.state.SelectedCueIndex
    //           SelectCueGroup = fun g -> this.setState({this.state with SelectedCueGroupIndex = g; SelectedCueIndex = 0})
    //           SelectCue = fun g c -> this.setState({this.state with SelectedCueGroupIndex = g; SelectedCueIndex = c }) }
    //         []
    // ]
