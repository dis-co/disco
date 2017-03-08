namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

type PinGroupYaml(id, name, pins) as self =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable Pins : PinYaml array

  new () = new PinGroupYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.Pins <- pins

#endif

//  ____       _       _
// |  _ \ __ _| |_ ___| |__
// | |_) / _` | __/ __| '_ \
// |  __/ (_| | || (__| | | |
// |_|   \__,_|\__\___|_| |_|

type PinGroup =
  { Id   : Id
    Name : Name
    Pins : Map<Id,Pin> }

  //  _   _           ____  _
  // | | | | __ _ ___|  _ \(_)_ __
  // | |_| |/ _` / __| |_) | | '_ \
  // |  _  | (_| \__ \  __/| | | | |
  // |_| |_|\__,_|___/_|   |_|_| |_|

  static member HasPin (group : PinGroup) (id: Id) : bool =
    Map.containsKey id group.Pins

  //  _____ _           _ ____  _
  // |  ___(_)_ __   __| |  _ \(_)_ __
  // | |_  | | '_ \ / _` | |_) | | '_ \
  // |  _| | | | | | (_| |  __/| | | | |
  // |_|   |_|_| |_|\__,_|_|   |_|_| |_|

  static member FindPin (groups : Map<Id, PinGroup>) (id : Id) : Pin option =
    let folder (m : Pin option) _ (group: PinGroup) =
      match m with
        | Some _ as res -> res
        |      _        -> Map.tryFind id group.Pins
    Map.fold folder None groups

  //   ____            _        _           ____  _
  //  / ___|___  _ __ | |_ __ _(_)_ __  ___|  _ \(_)_ __
  // | |   / _ \| '_ \| __/ _` | | '_ \/ __| |_) | | '_ \
  // | |__| (_) | | | | || (_| | | | | \__ \  __/| | | | |
  //  \____\___/|_| |_|\__\__,_|_|_| |_|___/_|   |_|_| |_|

  static member ContainsPin (groups : Map<Id,PinGroup>) (id: Id) : bool =
    let folder m _ p =
      if m then m else PinGroup.HasPin p id || m
    Map.fold folder false groups

  //     _       _     _ ____  _
  //    / \   __| | __| |  _ \(_)_ __
  //   / _ \ / _` |/ _` | |_) | | '_ \
  //  / ___ \ (_| | (_| |  __/| | | | |
  // /_/   \_\__,_|\__,_|_|   |_|_| |_|

  static member AddPin (group : PinGroup) (pin : Pin) : PinGroup=
    if PinGroup.HasPin group pin.Id then
      group
    else
      { group with Pins = Map.add pin.Id pin group.Pins }

  //  _   _           _       _       ____  _
  // | | | |_ __   __| | __ _| |_ ___|  _ \(_)_ __
  // | | | | '_ \ / _` |/ _` | __/ _ \ |_) | | '_ \
  // | |_| | |_) | (_| | (_| | ||  __/  __/| | | | |
  //  \___/| .__/ \__,_|\__,_|\__\___|_|   |_|_| |_|
  //       |_|

  static member UpdatePin (group : PinGroup) (pin : Pin) : PinGroup =
    if PinGroup.HasPin group pin.Id then
      let mapper _ (other: Pin) =
        if other.Id = pin.Id then pin else other
      { group with Pins = Map.map mapper group.Pins }
    else
      group

  //  _   _           _       _       ____  _ _
  // | | | |_ __   __| | __ _| |_ ___/ ___|| (_) ___ ___  ___
  // | | | | '_ \ / _` |/ _` | __/ _ \___ \| | |/ __/ _ \/ __|
  // | |_| | |_) | (_| | (_| | ||  __/___) | | | (_|  __/\__ \
  //  \___/| .__/ \__,_|\__,_|\__\___|____/|_|_|\___\___||___/
  //       |_|

  static member UpdateSlices (group : PinGroup) (slices: Slices) : PinGroup =
    if PinGroup.HasPin group slices.Id then
      let mapper _ (pin: Pin) =
        if pin.Id = slices.Id then
          pin.SetSlices slices
        else pin
      { group with Pins = Map.map mapper group.Pins }
    else
      group

  //  ____                               ____  _
  // |  _ \ ___ _ __ ___   _____   _____|  _ \(_)_ __
  // | |_) / _ \ '_ ` _ \ / _ \ \ / / _ \ |_) | | '_ \
  // |  _ <  __/ | | | | | (_) \ V /  __/  __/| | | | |
  // |_| \_\___|_| |_| |_|\___/ \_/ \___|_|   |_|_| |_|

  static member RemovePin (group : PinGroup) (pin : Pin) : PinGroup =
    { group with Pins = Map.remove pin.Id group.Pins }


  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member self.ToYamlObject () =
    let yaml = new PinGroupYaml()
    yaml.Id <- string self.Id
    yaml.Name <- self.Name
    yaml.Pins <- self.Pins
                   |> Map.toArray
                   |> Array.map (snd >> Yaml.toYaml)
    yaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: PinGroupYaml) =
    either {
      let! pins =
        Array.fold
          (fun (m: Either<IrisError,Map<Id,Pin>>) pinyml -> either {
            let! pins = m
            let! (pin : Pin) = Yaml.fromYaml pinyml
            return Map.add pin.Id pin pins
          })
          (Right Map.empty)
          yml.Pins

      return { Id = Id yml.Id
               Name = yml.Name
               Pins = pins }
    }

  static member FromYaml (str: string) : Either<IrisError,PinGroup> =
    let serializer = new Serializer()
    serializer.Deserialize<PinGroupYaml>(str)
    |> Yaml.fromYaml

  #endif

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: PinGroupFB) =
    either {
      let! pins =
        let arr = Array.zeroCreate fb.PinsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id,Pin>>) _ -> either {
              let! (i, pins) = m

              #if FABLE_COMPILER
              let! pin = i |> fb.Pins |> Pin.FromFB
              #else
              let! pin =
                let nullable = fb.Pins(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Pin.FromFB
                else
                  "Could not parse empty PinFB"
                  |> Error.asParseError "PinGroup.FromFB"
                  |> Either.fail
              #endif

              return (i + 1, Map.add pin.Id pin pins)
            })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      return { Id = Id fb.Id
               Name = fb.Name
               Pins = pins }
    }

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PinGroupFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let pinoffsets =
      self.Pins
      |> Map.toArray
      |> Array.map (fun (_,pin: Pin) -> pin.ToOffset(builder))

    let pins = PinGroupFB.CreatePinsVector(builder, pinoffsets)
    PinGroupFB.StartPinGroupFB(builder)
    PinGroupFB.AddId(builder, id)
    PinGroupFB.AddName(builder, name)
    PinGroupFB.AddPins(builder, pins)
    PinGroupFB.EndPinGroupFB(builder)

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError,PinGroup> =
    Binary.createBuffer bytes
    |> PinGroupFB.GetRootAsPinGroupFB
    |> PinGroup.FromFB

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError, PinGroup> =
    either {
      let! data = Asset.read path
      let! group = Yaml.decode data
      return group
    }

  static member LoadAll(basePath: FilePath) : Either<IrisError, PinGroup array> =
    either {
      try
        let dir = basePath </> PINGROUP_DIR
        let files = Directory.GetFiles(dir, sprintf "*%s" ASSET_EXTENSION)

        let! (_,groups) =
          let arr =
            files
            |> Array.length
            |> Array.zeroCreate
          Array.fold
            (fun (m: Either<IrisError, int * PinGroup array>) path ->
              either {
                let! (idx,groups) = m
                let! group = PinGroup.Load path
                groups.[idx] <- group
                return (idx + 1, groups)
              })
            (Right(0, arr))
            files

        return groups
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError "PinGroup.LoadAll"
            |> Either.fail
    }

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member self.AssetPath
    with get () =
      let filepath =
        sprintf "%s_%s%s"
          (String.sanitize self.Name)
          (string self.Id)
          ASSET_EXTENSION
      PINGROUP_DIR </> filepath

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member group.Save (basePath: FilePath) =
    either {
      let path = basePath </> Asset.path group
      let data = Yaml.encode group
      let! _ = Asset.write path (Payload data)
      return ()
    }

  #endif
