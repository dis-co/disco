module Disco.Web.Tooltips

open System
open Disco.Core


module Pin =

  let tooltip pin =
    match pin with
    | StringPin data ->
      let formatString = @"String Pin:
      Name:       {0}
      Persisted:  {1}
      Online:     {2}
      Type:       {3}
      VecSize:    {4}
      Behavior:   {5}
      MaxChars:   {6}
      "
      String.Format(formatString,
        data.Name,
        data.Persisted,
        data.Online,
        string data.PinConfiguration,
        string data.VecSize,
        string data.Behavior,
        data.MaxChars)
    | NumberPin data ->
      let format = @"Number Pin:

      Name:       {0}
      Persisted:  {1}
      Online:     {2}
      Type:       {3}
      VecSize:    {4}
      Min:        {5}
      Max:        {6}
      Unit:       {7}
      Precision:  {8}
      "
      String.Format(format,
        data.Name,
        data.Persisted,
        data.Online,
        string data.PinConfiguration,
        string data.VecSize,
        data.Min,
        data.Max,
        data.Unit,
        data.Precision)
    | BoolPin data ->
      let formatBool = @"Bool Pin:

      Name:       {0}
      Persisted:  {1}
      Online:     {2}
      Type:       {3}
      VecSize:    {4}
      "
      String.Format(formatBool,
        data.Name,
        data.Persisted,
        data.Online,
        string data.PinConfiguration,
        string data.VecSize)
    | BytePin data ->
      let formatByte = @"Byte Pin:

      Name:       {0}
      Persisted:  {1}
      Online:     {2}
      Type:       {3}
      VecSize:    {4}
      "
      String.Format(formatByte,
        data.Name,
        data.Persisted,
        data.Online,
        string data.PinConfiguration,
        string data.VecSize)
    | EnumPin data ->
      let formatEnum = @"Enum Pin:

      Name:       {0}
      Persisted:  {1}
      Online:     {2}
      Type:       {3}
      VecSize:    {4}
      "
      String.Format(formatEnum,
        data.Name,
        data.Persisted,
        data.Online,
        string data.PinConfiguration,
        string data.VecSize)
    | ColorPin data ->
    let formatColor = @"Color Pin:

    Name:       {0}
    Persisted:  {1}
    Online:     {2}
    Type:       {3}
    VecSize:    {4}
    "
    String.Format(formatColor,
      data.Name,
      data.Persisted,
      data.Online,
      string data.PinConfiguration,
      string data.VecSize)

module Navbar =

  module Window =
    let [<Literal>] log =               "Open Log"
    let [<Literal>] inspector =         "Open Inspector"
    let [<Literal>] fileBrowser =       "Open File Browser"
    let [<Literal>] graph =             "Open Graph View"
    let [<Literal>] players =           "Open Players"
    let [<Literal>] cues =              "Open Cues"
    let [<Literal>] cueLists =          "Open Cue Lists"
    let [<Literal>] pinMappings =       "Open PinMappings"
    let [<Literal>] project =           "Open Project Overview"
    let [<Literal>] clusterSettings =   "Open Cluster Settings"
    let [<Literal>] clients =           "Open Clients"
    let [<Literal>] sessions =          "Open Sessions"
    let [<Literal>] testWidget1 =       "Open Simple Value Slider"
    let [<Literal>] testWidget2 =       "Open Byte Pin Widget"
    let [<Literal>] testWidget3 =       "Open Enumeration Widget"
    let [<Literal>] testWidget4 =       "Open String Pin Widget"
    let [<Literal>] testWidget5 =       "Open XY Slider"
    let [<Literal>] testWidget6 =       "Open Color Picker"

  module Edit =

    let [<Literal>] undo =          "Undo"
    let [<Literal>] redo =          "Redo"
    let [<Literal>] resetDirty =    "Ignore pins that have changed since the last save"
    let [<Literal>] settings =      "Show Settings"

  module Project =

    let [<Literal>] create =    "Create a new project in the current Workspace Directory"
    let [<Literal>] load =      "Load an existing Project"
    let [<Literal>] save =      "Save your current Project"
    let [<Literal>] unload =    "Close the current Project"
    let [<Literal>] shutDown =  "Exit Iris"

module GraphView =

  let [<Literal>] resetDirty = "reset pins marked as dirty in state"
  let [<Literal>] persistAll = "add all pins to the project"
  let [<Literal>] persistSelected = "add selected pins to the project"
  let [<Literal>] createCue = "create cue from selected pins"
  let [<Literal>] addToCue = "add selected pins to one or more cues"
  let [<Literal>] showPlayers = "show pin groups that belong to cue players"
  let [<Literal>] hidePlayers = "hide pin groups that belong to cue players"
  let [<Literal>] showWidgets = "show pin groups that belong to a widget"
  let [<Literal>] hideWidgets = "hide pin groups that belong to a widget"

module CuePlayerView =

  let [<Literal>] lock = "Prevent all editing operations on this player"
  let [<Literal>] unlock = "Make this Player editable again"
  let [<Literal>] addGroup = "Inserts a new Cue group at the current location."
  let [<Literal>] createCue = "Create a new Cue the current location."
  let [<Literal>] addCue = "Add an existing Cue the current location."
  let [<Literal>] duplicateCue = "Duplicate the currently selected Cue"

module PlayerListView =
  let [<Literal>] createCuePlayer = "Open Cue Player for this Cue"
  let [<Literal>] removeCuePlayer = "Remove Cue Player from Cue Player List"
  let [<Literal>] updatePlayerName = "Update Cue Players name"

module CueListView =
  let [<Literal>] listName = "Double Click to edit the name of the Cue List"

module CueView =
  let [<Literal>] updateCueName = "Update name of the Cue"

module CuesView =
  let [<Literal>] updateCuePlayerName = "Update name of the Cue Player"

module CueGroupView =
  let [<Literal>]  updateCueGroupName = "Update Cue Group name"

