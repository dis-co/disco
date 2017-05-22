module FlatBuffersPlugin

open Fable
open Fable.AST

type FlatBuffersPlugin() =
    let apply r t args expr =
        Fable.Apply(expr, args, Fable.ApplyMeth, t, r)
    let get prop expr =
        Fable.Apply(expr, [Fable.Value(Fable.StringConst prop)], Fable.ApplyGet, Fable.Any, None)
    let staticField name =
        Fable.Value(Fable.IdentValue(Fable.Ident("Iris")))
        |> get "Serialization"
        |> get name
    interface IReplacePlugin with
        member x.TryReplace (com: Fable.ICompiler) (i: Fable.ApplyInfo) =
            match i.ownerFullName with
            | Naming.StartsWith "Iris.Web.Core.FlatBufferTypes" _ ->
                match i.callee with
                | None ->
                    staticField i.methodName
                | Some callee when i.ownerFullName.EndsWith "Constructor"
                                || i.ownerFullName.EndsWith "EnumFB" ->
                    match i.methodKind with
                    | Fable.Getter ->
                        get i.methodName callee
                    | _ when i.methodName = "Create" ->
                        Fable.Apply(callee, i.args, Fable.ApplyCons, i.returnType, i.range)
                    | _ ->
                        get (Naming.lowerFirst i.methodName) callee
                        |> apply i.range i.returnType i.args
                | Some callee when i.ownerFullName.EndsWith "StateMachineFB"
                                && i.methodName.EndsWith "FB" ->
                    let cons =
                        Fable.Apply(staticField i.methodName, [], Fable.ApplyCons, Fable.Any, None)
                    get "Payload" callee
                    |> apply i.range i.returnType [cons]
                // Note instance properties keep the capital letter
                // and properties are applied as if they were methods
                | Some callee ->
                    get i.methodName callee
                    |> apply i.range i.returnType i.args
                |> Some
            | _ -> None
