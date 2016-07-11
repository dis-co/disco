namespace Iris.Core

#if JAVASCRIPT

#else
open FlatBuffers
open Iris.Serialization.Raft
#endif

//  _   _           _        ____  _        _
// | \ | | ___   __| | ___  / ___|| |_ __ _| |_ _   _ ___
// |  \| |/ _ \ / _` |/ _ \ \___ \| __/ _` | __| | | / __|
// | |\  | (_) | (_| |  __/  ___) | || (_| | |_| |_| \__ \
// |_| \_|\___/ \__,_|\___| |____/ \__\__,_|\__|\__,_|___/

type IrisNodeStatus =
  | Running
  | Failed
  | Paused

  with
    override self.ToString() =
      match self with
      | Running -> "Running"
      | Failed  -> "Failed"
      | Paused  -> "Paused"

    static member Parse (str: string) =
      match str with
      | "Running" -> Running
      | "Failed"  -> Failed
      | "Paused"  -> Paused
      | _         -> failwithf "IrisNodeStatus: failed to parse %s" str

#if JAVASCRIPT
#else
    member self.ToOffset() =
      match self with
      | Running -> IrisNodeStatusFB.Running
      | Failed  -> IrisNodeStatusFB.Failed
      | Paused  -> IrisNodeStatusFB.Paused

    static member FromFB (fb: IrisNodeStatusFB) =
      match fb with
      | IrisNodeStatusFB.Running -> Running
      | IrisNodeStatusFB.Failed  -> Failed
      | IrisNodeStatusFB.Paused  -> Paused
      | v                        -> failwithf "IrisNodeState: failed to parse fb %A" v
#endif


//  ___      _       _   _           _
// |_ _|_ __(_)___  | \ | | ___   __| | ___
//  | || '__| / __| |  \| |/ _ \ / _` |/ _ \
//  | || |  | \__ \ | |\  | (_) | (_| |  __/
// |___|_|  |_|___/ |_| \_|\___/ \__,_|\___|

type IrisNode =
  { MemberId : Guid
  ; HostName : string
  ; IpAddr   : IpAddress
  ; Port     : int
  ; Status   : IrisNodeStatus
  ; TaskId   : TaskId option }

  static member Create name host port =
    { MemberId = createGuid()
    ; HostName = name
    ; IpAddr   = IPv4Address host
    ; Port     = port
    ; Status   = Running
    ; TaskId   = None }

  override self.ToString() =
    sprintf "[id: %A] [hostname: %s] [Ip: %A] [port: %A] [status: %A]"
      self.MemberId
      self.HostName
      self.IpAddr
      self.Port
      self.Status

#if JAVASCRIPT
  //      _                  ____            _       _
  //     | | __ ___   ____ _/ ___|  ___ _ __(_)_ __ | |_
  //  _  | |/ _` \ \ / / _` \___ \ / __| '__| | '_ \| __|
  // | |_| | (_| |\ V / (_| |___) | (__| |  | | |_) | |_
  //  \___/ \__,_| \_/ \__,_|____/ \___|_|  |_| .__/ \__| specific
  //                                          |_|

#else
  //    _   _ _____ _____
  //   | \ | | ____|_   _|
  //   |  \| |  _|   | |
  //  _| |\  | |___  | |
  // (_)_| \_|_____| |_| specific

  member self.ToOffset (builder: FlatBufferBuilder) =
    let id = string self.MemberId |> builder.CreateString
    let ip = string self.IpAddr   |> builder.CreateString
    let hn = self.HostName        |> builder.CreateString
    let tid = Option.map (string >> builder.CreateString) self.TaskId
    let st = self.Status.ToOffset()

    IrisNodeFB.StartIrisNodeFB (builder)
    IrisNodeFB.AddMemberId     (builder, id)
    IrisNodeFB.AddHostName     (builder, hn)
    IrisNodeFB.AddIpAddr       (builder, ip)
    IrisNodeFB.AddPort         (builder, self.Port)
    IrisNodeFB.AddStatus       (builder, st)

    match tid with
      | Some offset -> IrisNodeFB.AddTaskId(builder, offset)
      | _           -> ()

    IrisNodeFB.EndIrisNodeFB(builder)

  static member FromFB (fb: IrisNodeFB) =
    let tid =
      match fb.TaskId with
        | null | "" -> None
        | str    -> Guid.TryParse str

    { MemberId = Guid.Parse fb.MemberId
    ; HostName = fb.HostName
    ; IpAddr   = IPv4Address fb.IpAddr
    ; Port     = fb.Port
    ; Status   = IrisNodeStatus.FromFB fb.Status
    ; TaskId   = tid }

type Node = Pallet.Core.Node<IrisNode>

#endif
