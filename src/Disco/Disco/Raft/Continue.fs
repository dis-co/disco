(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Raft

type Continue<'a> =
  | Cont of 'a
  | Ret  of 'a

[<RequireQualifiedAccess>]
module Continue =
  let next = Cont
  let finish = Ret

  let bind v f =
    match v with
      | Cont a -> f a
      | Ret  a -> Ret a

  let rec runCont = function
    | Cont _ as cont -> runCont cont
    | Ret a -> a
