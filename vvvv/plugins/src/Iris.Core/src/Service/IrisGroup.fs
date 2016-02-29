namespace Iris.Service.Core

open System
open Iris.Core.Types
open Iris.Service.Groups

[<AutoOpen>]
module IrisGroup =

  //  ___      _      ____
  // |_ _|_ __(_)___ / ___|_ __ ___  _   _ _ __
  //  | || '__| / __| |  _| '__/ _ \| | | | '_ \
  //  | || |  | \__ \ |_| | | | (_) | |_| | |_) |
  // |___|_|  |_|___/\____|_|  \___/ \__,_| .__/
  //                                      |_|
  type IrisGroup(groupname : string, project : Project) = 
    let pinGroup = new PinGroup(project, groupname)
    let cueGroup = new CueGroup(project, groupname)

    do
      pinGroup.Join()
      cueGroup.Join()

    interface IDisposable with
      member self.Dispose() =
        pinGroup.Leave()
        cueGroup.Leave()

