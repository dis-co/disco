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
type RCom = React.ComponentClass<obj>

let [<Literal>] SELECTION_COLOR = "lightblue"

module private Helpers =
  type RCom = React.ComponentClass<obj>
  let ContentEditable: RCom = importDefault "../../../src/widgets/ContentEditable"
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
    let cueGroup =
      { Id = Id.Create()
        Name = name "MockCueGroup"
        CueRefs = [||] }
    let cueList: CueList =
      { Id = Id.Create()
        Name = name "MockCueList"
        Groups = [|cueGroup|] }
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
      match slices with
      | StringSlices (_, values) -> StringSlices(pin.Id, values)
      | NumberSlices (_, values) -> NumberSlices(pin.Id, values)
      | BoolSlices   (_, values) -> BoolSlices(pin.Id, values)
      | ByteSlices   (_, values) -> ByteSlices(pin.Id, values)
      | EnumSlices   (_, values) -> EnumSlices(pin.Id, values)
      | ColorSlices  (_, values) -> ColorSlices(pin.Id, values)
      |> UpdateSlices |> ClientContext.Singleton.Post

  let printCueList (cueList: CueList) =
    for group in cueList.Groups do
      printfn "CueGroup: %O (%O)" group.Name group.Id
      for cueRef in group.CueRefs do
        printfn "    CueRef: %O" cueRef.Id

  module Array =
    let inline replaceById< ^t when ^t : (member Id : Id)> (newItem : ^t) (ar: ^t[]) =
      Array.map (fun (x: ^t) -> if (^t : (member Id : Id) newItem) = (^t : (member Id : Id) x) then newItem else x) ar

    let insertAfter (i: int) (x: 't) (xs: 't[]) =
      let len = xs.Length
      if len = 0 (* && i = 0 *) then
        [|x|]
      elif i >= len then
        failwith "Index out of array bounds"
      elif i < 0 then
        Array.append [|x|] xs
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
    IsOpen: bool }

type [<Pojo>] private CueProps =
  { key: string
    Global: GlobalModel
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
    base.setInitState({Cue = cue; IsOpen = false })

  member this.componentDidMount() =
    let globalModel = this.props.Global
    disposables.Add(globalModel.subscribe(!^(nameof globalModel.state.cues), fun _ dic ->
      if tryDic "Id" this.state.Cue.Id dic then
        let cue = Map.find this.state.Cue.Id globalModel.state.cues
        this.setState({this.state with Cue=cue})
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
                printfn "The cue already contains this pin"
              else
                let newCue = { this.state.Cue with Slices = Array.append this.state.Cue.Slices [|ev.model.Slices|] }
                UpdateCue newCue |> ClientContext.Singleton.Post
                this.setState({ this.state with IsOpen = true })
            | _ -> ()
          if highlight
          then selfRef.classList.add("iris-highlight", "iris-blue")
          else selfRef.classList.remove("iris-highlight", "iris-blue")
    ))

  member this.componentWillUnmount() =
    for d in disposables do
      d.dispose()

  member this.RenderInput(widthPercentage: int, content: string, ?update: string->unit) =
    let content =
      match update with
      | Some update ->
        from ContentEditable
          %["tagName" ==> "span"
            "html" ==> content
            "onChange" ==> update] []
      | None -> span [] [str content]
    td [ClassName ("width-" + string widthPercentage)] [content]

  member this.render() =
    let arrowButton =
      td [ClassName "width-5"] [
        button [
          ClassName ("iris-button iris-icon icon-control " + (if this.state.IsOpen then "icon-less" else "icon-more"))
          OnClick (fun ev ->
            ev.stopPropagation()
            this.setState({ this.state with IsOpen = not this.state.IsOpen}))
        ] []
      ]
    let playButton =
      td [ClassName "width-5"] [
        button [
          ClassName "iris-button iris-icon icon-play"
          OnClick (fun ev ->
            ev.stopPropagation()
            updatePins this.state.Cue this.props.Global.state // TODO: Send CallCue event instead
          )
        ] []
      ]
    let autocallButton =
      td [ClassName "width-10"; Style [TextAlign "center"]] [
        button [
          ClassName "iris-button iris-icon icon-autocall"
          OnClick (fun ev ->
            ev.stopPropagation()
            // Browser.window.alert("Auto call!")
          )
        ] []
      ]
    let removeButton =
      td [ClassName "width-5"] [
        button [
          ClassName "iris-button iris-icon icon-control icon-close"
          OnClick (fun ev ->
            ev.stopPropagation()
            let id = this.props.CueRef.Id
            // Change selection if this item was selected
            if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex then
              this.props.SelectCue this.props.CueGroupIndex 0
            let cueGroup = { this.props.CueGroup with CueRefs = this.props.CueGroup.CueRefs |> Array.filter (fun c -> c.Id <> id) }
            { this.props.CueList with Groups = Array.replaceById cueGroup this.props.CueList.Groups }
            |> UpdateCueList |> ClientContext.Singleton.Post)
        ] []
      ]
    let isSelected =
      if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
        && this.props.CueIndex = this.props.SelectedCueIndex
      then "iris-cue iris-selected" else "iris-cue"
    let cueHeader =
      tr [
        OnClick (fun _ ->
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex
            || this.props.CueIndex <> this.props.SelectedCueIndex then
            this.props.SelectCue this.props.CueGroupIndex this.props.CueIndex  )
      ] [
        arrowButton
        playButton
        this.RenderInput(10, String.Format("{0:0000}", this.props.CueIndex + 1))
        this.RenderInput(25, unwrap this.state.Cue.Name, (fun txt ->
          { this.state.Cue with Name = name txt } |> UpdateCue |> ClientContext.Singleton.Post))
        this.RenderInput(20, "00:00:00")
        this.RenderInput(20, "shortkey")
        autocallButton
        removeButton
      ]
    let rows =
      if not this.state.IsOpen then
        [cueHeader]
      else
        let pinGroups =
          this.state.Cue.Slices
          |> Array.mapi (fun i slices -> i, findPin slices.Id this.props.Global.state, slices)
          |> Array.groupBy (fun (_, pin, _) -> pin.PinGroup)
          |> Array.map(fun (pinGroupId, pinAndSlices) ->
            let pinGroup = findPinGroup pinGroupId this.props.Global.state
            li [Key (string pinGroupId)] [
              yield div [] [str (unwrap pinGroup.Name)]
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
        [cueHeader; tr [] [td [ColSpan 8.] [ul [ClassName "iris-graphview"] pinGroups]]]
    tr [] [
      td [ColSpan 8.] [
        table [
          ClassName ("iris-table " + isSelected)
          Ref (fun el -> selfRef <- el)
        ] [tbody [] rows]]
    ]

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
//                 let cue = findCue cueRef.CueId this.props.Global.state
//                 updatePins cue this.props.Global.state
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
  let clickObservable = GenericObservable()
  member __.titleBar =
    button [
      ClassName "iris-button"
      OnClick (fun _ -> clickObservable.Trigger())
    ] [str "Add Cue"]
  member __.addCue =
    clickObservable :> IObservable<_>
  interface IWidgetModel with
    member __.view = typeof<CuePlayerView>
    member __.name = "Cue Player"
    member __.layout =
      { x = 0; y = 0;
        w = 8; h = 5;
        minW = 4; maxW = 10;
        minH = 4; maxH = 10; }

type [<Pojo>] CuePlayerState =
  { CueList: CueList option
    SelectedCueGroupIndex: int
    SelectedCueIndex: int }

type CuePlayerView(props) =
  inherit React.Component<IWidgetProps<CuePlayerModel>, CuePlayerState>(props)
  let disposables = ResizeArray<IDisposableJS>()
  let globalModel = props.``global``
  do
    // TODO: Mock code, create player if it doesn't exist
    if Map.count props.``global``.state.cuePlayers = 0 then
      let cueList, cuePlayer = cueMockup()
      AddCueList cueList |> ClientContext.Singleton.Post
      AddCuePlayer cuePlayer |> ClientContext.Singleton.Post
      base.setInitState({ CueList = Some cueList; SelectedCueGroupIndex = -1; SelectedCueIndex = -1 })
    else
      // TODO: Use a dropdown to choose the player/list
      let cueList =
        Seq.tryHead props.``global``.state.cuePlayers
        |> Option.bind (fun kv -> kv.Value.CueList)
        |> Option.bind (fun id -> Map.tryFind id props.``global``.state.cueLists)
      base.setInitState({ CueList = cueList; SelectedCueGroupIndex = -1; SelectedCueIndex = -1 })

  member this.componentDidMount() =
    let state = globalModel.state
    disposables.Add(globalModel.subscribe(!^[|nameof(state.cueLists); nameof(state.cuePlayers)|], fun _ dic ->
      match this.state.CueList with
      | Some cueList ->
        if tryDic (nameof cueList.Id) cueList.Id dic then
          let cueList = Map.find cueList.Id globalModel.state.cueLists
          this.setState({this.state with CueList=Some cueList})
      | None -> ()))
    disposables.Add(this.props.model.addCue.Subscribe(fun () ->
      match this.state.CueList with
      | None -> failwith "There is no cue list available to add the group"
      | Some cueList ->
        if cueList.Groups.Length = 0 then
          failwith "A Cue Group must be added first"
        // Create new Cue and CueReference
        let newCue = { Id = Id.Create(); Name = name "Untitled"; Slices = [||] }
        let newCueRef = { Id = Id.Create(); CueId = newCue.Id; AutoFollow = -1; Duration = -1; Prewait = -1 }
        // Insert new CueRef in the selected CueGroup after the selected cue
        let cueGroup = cueList.Groups.[max this.state.SelectedCueGroupIndex 0]
        let idx =
          if this.state.SelectedCueIndex < 0
          then cueGroup.CueRefs.Length - 1
          else this.state.SelectedCueIndex
        let newCueGroup = { cueGroup with CueRefs = Array.insertAfter idx newCueRef cueGroup.CueRefs }
        // Update the CueList
        let newCueList = { cueList with Groups = Array.replaceById newCueGroup cueList.Groups }
        // Send messages to backend
        AddCue newCue |> ClientContext.Singleton.Post
        UpdateCueList newCueList |> ClientContext.Singleton.Post
    ).ToIDisposableJS())

  member this.componentWillUnmount() =
    for d in disposables do
      d.dispose()

  member this.RenderCues() =
    this.state.CueList
    // TODO: Temporarily assume just one group
    |> Option.bind (fun cueList -> Seq.tryHead cueList.Groups)
    |> Option.map (fun group ->
      group.CueRefs
      |> Array.mapi (fun i cueRef ->
        com<CueView,_,_>
          { key = string cueRef.Id
            Global = this.props.``global``
            CueRef = cueRef
            CueGroup = group
            CueList = this.state.CueList.Value
            CueIndex = i
            CueGroupIndex = 0 //this.props.CueGroupIndex
            SelectedCueIndex = this.state.SelectedCueIndex
            SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
            SelectCue = fun g c -> this.setState({this.state with SelectedCueGroupIndex = g; SelectedCueIndex = c }) }
          [])
      |> Array.toList)
    |> defaultArg <| []

  member this.render() =
    table [ClassName "iris-table"] [
      thead [Key "header"] [
        tr [] [
          th [ClassName "width-5"] [str ""]
          th [ClassName "width-5"] [str ""]
          th [ClassName "width-10"] [str "Nr."]
          th [ClassName "width-25"] [str "Cue name"]
          th [ClassName "width-20"] [str "Delay"]
          th [ClassName "width-20"] [str "Shortkey"]
          th [ClassName "width-10"; Style [TextAlign "center"]] [str "Autocall"]
          th [ClassName "width-5"] [str ""]
        ]
      ]
      tbody [] (this.RenderCues())
    ]
