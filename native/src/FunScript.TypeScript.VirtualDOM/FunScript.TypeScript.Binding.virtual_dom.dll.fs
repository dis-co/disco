
namespace FunScript.TypeScript.VirtualDOM
type AnonymousType441 = interface end

namespace FunScript.TypeScript.VirtualDOM
type AnonymousType442 = interface end

namespace FunScript.TypeScript.VirtualDOM
type AnonymousType443 = interface end

namespace FunScript.TypeScript.VirtualDOM
type AnonymousType444 = interface end

namespace FunScript.TypeScript.VirtualDOM
type AnonymousType445 = interface end

namespace FunScript.TypeScript.VirtualDOM
type Thunk = interface end

namespace FunScript.TypeScript.VirtualDOM
type VHook = interface end

namespace FunScript.TypeScript.VirtualDOM
type VNode = interface end

namespace FunScript.TypeScript.VirtualDOM
type VPatch = interface end

namespace FunScript.TypeScript.VirtualDOM
type VProperties = interface end

namespace FunScript.TypeScript.VirtualDOM
type VText = interface end

namespace FunScript.TypeScript.VirtualDOM
type Globals = interface end

namespace FunScript.TypeScript.VirtualDOM
type Widget = interface end

namespace FunScript.TypeScript.VirtualDOM
type createProperties =
        inherit VProperties


namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_0 =


    type FunScript.TypeScript.VirtualDOM.AnonymousType441 with 

            [<FunScript.JSEmitInline("({0}[{1}])"); CompiledName("Item_42")>]
            member __.Item with get(i : string) : string = failwith "never" and set (i : string) (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_1 =


    type FunScript.TypeScript.VirtualDOM.AnonymousType442 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_4")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_2")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_2 =


    type FunScript.TypeScript.VirtualDOM.AnonymousType443 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_5")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_3")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_3 =


    type FunScript.TypeScript.VirtualDOM.AnonymousType444 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_6")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_4")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_4 =


    type FunScript.TypeScript.VirtualDOM.AnonymousType445 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_7")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_5")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_5 =


    type FunScript.TypeScript.VirtualDOM.Thunk with 

            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_36")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.vnode)"); CompiledName("vnode")>]
            member __.vnode with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.render({1}))"); CompiledName("render")>]
            member __.render(previous : obj) : obj = failwith "never"
            [<FunScript.JSEmitInline("({0}.render = {1})"); CompiledName("renderAux")>]
            member __.``render <-``(func : System.Func<obj, obj>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_6 =


    type FunScript.TypeScript.VirtualDOM.VHook with 

            [<FunScript.JSEmitInline("({0}.hook({1}, {2}))"); CompiledName("hook")>]
            member __.hook(node : FunScript.TypeScript.Element, propertyName : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.hook = {1})"); CompiledName("hookAux")>]
            member __.``hook <-``(func : System.Func<FunScript.TypeScript.Element, string, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unhook({1}, {2}))"); CompiledName("unhook")>]
            member __.unhook(node : FunScript.TypeScript.Element, propertyName : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unhook = {1})"); CompiledName("unhookAux")>]
            member __.``unhook <-``(func : System.Func<FunScript.TypeScript.Element, string, unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_7 =


    type FunScript.TypeScript.VirtualDOM.VNode with 

            [<FunScript.JSEmitInline("({0}.tagName)"); CompiledName("tagName_1")>]
            member __.tagName with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.properties)"); CompiledName("properties")>]
            member __.properties with get() : FunScript.TypeScript.VirtualDOM.VProperties = failwith "never" and set (v : FunScript.TypeScript.VirtualDOM.VProperties) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.children)"); CompiledName("children_1")>]
            member __.children with get() : array<obj> = failwith "never" and set (v : array<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.key)"); CompiledName("key_5")>]
            member __.key with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.namespace)"); CompiledName("_namespace")>]
            member __._namespace with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.count)"); CompiledName("count_3")>]
            member __.count with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.hasWidgets)"); CompiledName("hasWidgets")>]
            member __.hasWidgets with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.hasThunks)"); CompiledName("hasThunks")>]
            member __.hasThunks with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.hooks)"); CompiledName("hooks")>]
            member __.hooks with get() : array<obj> = failwith "never" and set (v : array<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.descendantHooks)"); CompiledName("descendantHooks")>]
            member __.descendantHooks with get() : array<obj> = failwith "never" and set (v : array<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.version)"); CompiledName("version_4")>]
            member __.version with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_37")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_8 =


    type FunScript.TypeScript.VirtualDOM.VPatch with 

            [<FunScript.JSEmitInline("({0}.vNode)"); CompiledName("vNode")>]
            member __.vNode with get() : FunScript.TypeScript.VirtualDOM.VNode = failwith "never" and set (v : FunScript.TypeScript.VirtualDOM.VNode) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.patch)"); CompiledName("patch")>]
            member __.patch with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("(new {0}({1}, {2}, {3}))"); CompiledName("Create_473")>]
            member __.Create(_type : float, vNode : FunScript.TypeScript.VirtualDOM.VNode, patch : obj) : FunScript.TypeScript.VirtualDOM.VPatch = failwith "never"
            [<FunScript.JSEmitInline("(new {0} = {1})"); CompiledName("Create_473Aux")>]
            member __.``Create <-``(func : System.Func<float, FunScript.TypeScript.VirtualDOM.VNode, obj, FunScript.TypeScript.VirtualDOM.VPatch>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.version)"); CompiledName("version_5")>]
            member __.version with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_38")>]
            member __._type with get() : float = failwith "never" and set (v : float) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_9 =


    type FunScript.TypeScript.VirtualDOM.VProperties with 

            [<FunScript.JSEmitInline("({0}.attributes)"); CompiledName("attributes_2")>]
            member __.attributes with get() : FunScript.TypeScript.VirtualDOM.AnonymousType441 = failwith "never" and set (v : FunScript.TypeScript.VirtualDOM.AnonymousType441) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.style)"); CompiledName("style_8")>]
            member __.style with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}[{1}])"); CompiledName("Item_43")>]
            member __.Item with get(i : string) : obj = failwith "never" and set (i : string) (v : obj) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_10 =


    type FunScript.TypeScript.VirtualDOM.VText with 

            [<FunScript.JSEmitInline("({0}.text)"); CompiledName("text_9")>]
            member __.text with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("(new {0}({1}))"); CompiledName("Create_474")>]
            member __.Create(text : obj) : FunScript.TypeScript.VirtualDOM.VText = failwith "never"
            [<FunScript.JSEmitInline("(new {0} = {1})"); CompiledName("Create_474Aux")>]
            member __.``Create <-``(func : System.Func<obj, FunScript.TypeScript.VirtualDOM.VText>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.version)"); CompiledName("version_6")>]
            member __.version with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_39")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_11 =


    type FunScript.TypeScript.VirtualDOM.Globals with 

            [<FunScript.JSEmitInline("(VirtualDOM.create({0}, {?1}))"); CompiledName("create_5")>]
            static member create(vnode : FunScript.TypeScript.VirtualDOM.VText, ?opts : FunScript.TypeScript.VirtualDOM.AnonymousType442) : FunScript.TypeScript.Text = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create = {0})"); CompiledName("create_5Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VText, FunScript.TypeScript.VirtualDOM.AnonymousType442, FunScript.TypeScript.Text>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create({0}, {?1}))"); CompiledName("create_6")>]
            static member create(vnode : FunScript.TypeScript.VirtualDOM.VNode, ?opts : FunScript.TypeScript.VirtualDOM.AnonymousType443) : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create = {0})"); CompiledName("create_6Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VNode, FunScript.TypeScript.VirtualDOM.AnonymousType443, FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create({0}, {?1}))"); CompiledName("create_7")>]
            static member create(vnode : FunScript.TypeScript.VirtualDOM.Widget, ?opts : FunScript.TypeScript.VirtualDOM.AnonymousType444) : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create = {0})"); CompiledName("create_7Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Widget, FunScript.TypeScript.VirtualDOM.AnonymousType444, FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create({0}, {?1}))"); CompiledName("create_8")>]
            static member create(vnode : FunScript.TypeScript.VirtualDOM.Thunk, ?opts : FunScript.TypeScript.VirtualDOM.AnonymousType445) : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.create = {0})"); CompiledName("create_8Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Thunk, FunScript.TypeScript.VirtualDOM.AnonymousType445, FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.h({0}, {1}, {2}))"); CompiledName("h")>]
            static member h(tagName : string, properties : FunScript.TypeScript.VirtualDOM.createProperties, children : array<obj>) : FunScript.TypeScript.VirtualDOM.VNode = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.h = {0})"); CompiledName("hAux")>]
            static member ``h <-``(func : System.Func<string, FunScript.TypeScript.VirtualDOM.createProperties, array<obj>, FunScript.TypeScript.VirtualDOM.VNode>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.h({0}, {1}))"); CompiledName("h_1")>]
            static member h(tagName : string, children : array<obj>) : FunScript.TypeScript.VirtualDOM.VNode = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.h = {0})"); CompiledName("h_1Aux")>]
            static member ``h <-``(func : System.Func<string, array<obj>, FunScript.TypeScript.VirtualDOM.VNode>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Thunk, right : FunScript.TypeScript.VirtualDOM.Thunk) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diffAux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Thunk, FunScript.TypeScript.VirtualDOM.Thunk, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_1")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Thunk, right : FunScript.TypeScript.VirtualDOM.Widget) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_1Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Thunk, FunScript.TypeScript.VirtualDOM.Widget, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_2")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Thunk, right : FunScript.TypeScript.VirtualDOM.VText) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_2Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Thunk, FunScript.TypeScript.VirtualDOM.VText, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_3")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Thunk, right : FunScript.TypeScript.VirtualDOM.VNode) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_3Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Thunk, FunScript.TypeScript.VirtualDOM.VNode, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_4")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Widget, right : FunScript.TypeScript.VirtualDOM.Thunk) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_4Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Widget, FunScript.TypeScript.VirtualDOM.Thunk, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_5")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Widget, right : FunScript.TypeScript.VirtualDOM.Widget) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_5Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Widget, FunScript.TypeScript.VirtualDOM.Widget, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_6")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Widget, right : FunScript.TypeScript.VirtualDOM.VText) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_6Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Widget, FunScript.TypeScript.VirtualDOM.VText, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_7")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.Widget, right : FunScript.TypeScript.VirtualDOM.VNode) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_7Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Widget, FunScript.TypeScript.VirtualDOM.VNode, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_8")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VText, right : FunScript.TypeScript.VirtualDOM.Thunk) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_8Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VText, FunScript.TypeScript.VirtualDOM.Thunk, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_9")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VText, right : FunScript.TypeScript.VirtualDOM.Widget) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_9Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VText, FunScript.TypeScript.VirtualDOM.Widget, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_10")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VText, right : FunScript.TypeScript.VirtualDOM.VText) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_10Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VText, FunScript.TypeScript.VirtualDOM.VText, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_11")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VText, right : FunScript.TypeScript.VirtualDOM.VNode) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_11Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VText, FunScript.TypeScript.VirtualDOM.VNode, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_12")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VNode, right : FunScript.TypeScript.VirtualDOM.Thunk) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_12Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VNode, FunScript.TypeScript.VirtualDOM.Thunk, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_13")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VNode, right : FunScript.TypeScript.VirtualDOM.Widget) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_13Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VNode, FunScript.TypeScript.VirtualDOM.Widget, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_14")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VNode, right : FunScript.TypeScript.VirtualDOM.VText) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_14Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VNode, FunScript.TypeScript.VirtualDOM.VText, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff({0}, {1}))"); CompiledName("diff_15")>]
            static member diff(left : FunScript.TypeScript.VirtualDOM.VNode, right : FunScript.TypeScript.VirtualDOM.VNode) : array<FunScript.TypeScript.VirtualDOM.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.diff = {0})"); CompiledName("diff_15Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.VNode, FunScript.TypeScript.VirtualDOM.VNode, array<FunScript.TypeScript.VirtualDOM.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.patch({0}, {1}, {?2}))"); CompiledName("patch_1")>]
            static member patch<'T when 'T :> FunScript.TypeScript.Element>(rootNode : 'T, patches : array<FunScript.TypeScript.VirtualDOM.VPatch>, ?renderOptions : obj) : 'T = failwith "never"
            [<FunScript.JSEmitInline("(VirtualDOM.patch = {0})"); CompiledName("patch_1Aux")>]
            static member ``patch <-``<'T when 'T :> FunScript.TypeScript.Element>(func : System.Func<'T, array<FunScript.TypeScript.VirtualDOM.VPatch>, obj, 'T>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_12 =


    type FunScript.TypeScript.VirtualDOM.Widget with 

            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_40")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.init())"); CompiledName("init")>]
            member __.init() : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("({0}.init = {1})"); CompiledName("initAux")>]
            member __.``init <-``(func : System.Func<FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.update({1}, {2}))"); CompiledName("update_3")>]
            member __.update(previous : FunScript.TypeScript.VirtualDOM.Widget, domNode : FunScript.TypeScript.Element) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.update = {1})"); CompiledName("update_3Aux")>]
            member __.``update <-``(func : System.Func<FunScript.TypeScript.VirtualDOM.Widget, FunScript.TypeScript.Element, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.destroy({1}))"); CompiledName("destroy")>]
            member __.destroy(node : FunScript.TypeScript.Element) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.destroy = {1})"); CompiledName("destroyAux")>]
            member __.``destroy <-``(func : System.Func<FunScript.TypeScript.Element, unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_13 =


    type FunScript.TypeScript.VirtualDOM.createProperties with 

            [<FunScript.JSEmitInline("({0}.key)"); CompiledName("key_6")>]
            member __.key with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.namespace)"); CompiledName("_namespace_1")>]
            member __._namespace with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
