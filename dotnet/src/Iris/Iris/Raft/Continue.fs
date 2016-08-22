namespace Iris.Raft

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
