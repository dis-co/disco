namespace Iris.Serivce

open Argu
open System.Net
open Pallet.Core

[<AutoOpen>]
module AuxTypes =
  
  type Actor<'T> = MailboxProcessor<'T>
  /////////////////////////////////////////////////////////////////////
  //  ____  _        _         __  __            _     _             //
  // / ___|| |_ __ _| |_ ___  |  \/  | __ _  ___| |__ (_)_ __   ___  //
  // \___ \| __/ _` | __/ _ \ | |\/| |/ _` |/ __| '_ \| | '_ \ / _ \ //
  //  ___) | || (_| | ||  __/ | |  | | (_| | (__| | | | | | | |  __/ //
  // |____/ \__\__,_|\__\___| |_|  |_|\__,_|\___|_| |_|_|_| |_|\___| //
  /////////////////////////////////////////////////////////////////////

  type MembershipRequestResult =
    | Hello
    | ByeBye
    | Redirect

  type Command =
    | Add

  type Data = int

  type StateMachine =
    | OP  of cmd:Command * data:Data    // this will be the place regular state
                                        // machine commands go
    | StateDump of data: Data
