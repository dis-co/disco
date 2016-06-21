namespace Iris.Web.Core

[<AutoOpen>]
module IrisMonad =
  type StateM<'a, 's> = 's -> ('a * 's)

  let inline (>>=) (m : StateM<'a,'s>) (f : 'a -> StateM<'b,'s>) : StateM<'b,'s> =
    fun s ->
      let (a, s') = m s
      in f a s'

  let returnM (a : 'a) : StateM<'a,'s> = fun s -> (a, s) // its a partially applied function!

  let runStateM (m : StateM<'a,'s>) (s : 's) : ('a * 's) = m s

  let putM (s : 's) : StateM<'a,'s> = fun s' -> ((), s)

  let getM : StateM<'a,'s> = fun s -> (s,s)

  let modify (f : 's -> 's) : StateM<unit, 's> = fun s -> ((),f s)

  let execState (m : StateM<'a,'s>) (s : 's) : 's  =
    let (_, s') = runStateM m s in s'

  let evalState (m : StateM<'a,'s>) (s : 's) : 'a =
    let (a, _) = runStateM m s in a'

  // runStateM ( modify ((+) 1)  >>=
  //   (fun _ -> modify ((+) 2)) >>=
  //   (fun _ -> modify ((+) 2)) >>=
  //   (fun _ -> getM)           >>=
  //   (fun a -> returnM (sprintf "ok. done: %d" a))) 0;;


  // type IrisMonad () =
  //   member x.Bind(comp, func) = Eventually.bind func comp
  //   member x.Return(value) = Eventually.result value
  //   member x.ReturnFrom(value) = value
  //   member x.Combine(expr1, expr2) = Eventually.combine expr1 expr2
  //   member x.Delay(func) = Eventually.delay func
  //   member x.Zero() = Eventually.result ()
  //   member x.TryWith(expr, handler) = Eventually.tryWith expr handler
  //   member x.TryFinally(expr, compensation) = Eventually.tryFinally expr compensation
  //   member x.For(coll:seq<_>, func) = Eventually.forLoop coll func
  //   member x.Using(resource, expr) = Eventually.using resource expr
