namespace Iris.Raft

type ValidationStep<'a,'err> =
  | ContinueValidation of 'a
  | ValidationResult of bool * 'err

  override self.ToString() =
    match self with
      | ContinueValidation(v) -> sprintf "ContinueValidation(%A)" v
      | ValidationResult(v,err)   -> sprintf "ValidationResult(%A,%A)" v err

module private ValidationImpl =

  let bind (v : ValidationStep<'a,'err>) (f : 'a -> ValidationStep<'a,'err>) =
    match v with
      | ContinueValidation(v)     -> f v
      | ValidationResult(ret,err) -> ValidationResult(ret,err)

  let returnV result _ = ValidationResult result

  let (.>) = bind

[<AutoOpen>]
module Validation =

  let validate (pred : 'a -> (bool * 'b)) (ret : bool) (v : 'a) =
    let result = pred v
    if fst result then ValidationResult((ret,snd result)) else ContinueValidation(v)

  type ValidationBuilder() =
    member x.Bind(comp, func) = ValidationImpl.bind comp func
    member x.Return(value) = ValidationResult value
    member x.ReturnFrom(value) = value
    member x.Delay(f) = f               // return the function for lazy evaluation
    member x.Run(f) = f()               // evaluate the delayed function above
    member x.Combine(a,b) =
      match a with
        | ContinueValidation _ -> b()             // the predicate failed, so continue
        | ValidationResult _   -> a               // return a itself, as the validation passed

  let validation = new ValidationBuilder()

  let rec runValidation = function
    | ContinueValidation _ as cont -> runValidation cont
    | ValidationResult(b,err) -> (b,err)
