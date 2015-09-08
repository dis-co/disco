
namespace FunScript.TypeScript.virtualDom
type AnonymousType441 = interface end

namespace FunScript.TypeScript.virtualDom
type AnonymousType442 = interface end

namespace FunScript.TypeScript.virtualDom
type AnonymousType443 = interface end

namespace FunScript.TypeScript.virtualDom
type AnonymousType444 = interface end

namespace FunScript.TypeScript.virtualDom
type AnonymousType445 = interface end

namespace FunScript.TypeScript.virtualDom
type Thunk = interface end

namespace FunScript.TypeScript.virtualDom
type VHook = interface end

namespace FunScript.TypeScript.virtualDom
type VNode = interface end

namespace FunScript.TypeScript.virtualDom
type VPatch = interface end

namespace FunScript.TypeScript.virtualDom
type VProperties = interface end

namespace FunScript.TypeScript.virtualDom
type VText = interface end

namespace FunScript.TypeScript.virtualDom
type Widget = interface end

namespace FunScript.TypeScript.virtualDom
type Globals = interface end

namespace FunScript.TypeScript.virtualDom
type createProperties =
        inherit VProperties


namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_0 =


    type FunScript.TypeScript.virtualDom.AnonymousType441 with 

            [<FunScript.JSEmitInline("({0}[{1}])"); CompiledName("Item_42")>]
            member __.Item with get(i : string) : string = failwith "never" and set (i : string) (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_1 =


    type FunScript.TypeScript.virtualDom.AnonymousType442 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_4")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_2")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_2 =


    type FunScript.TypeScript.virtualDom.AnonymousType443 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_5")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_3")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_3 =


    type FunScript.TypeScript.virtualDom.AnonymousType444 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_6")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_4")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_4 =


    type FunScript.TypeScript.virtualDom.AnonymousType445 with 

            [<FunScript.JSEmitInline("({0}.document)"); CompiledName("document_7")>]
            member __.document with get() : FunScript.TypeScript.Document = failwith "never" and set (v : FunScript.TypeScript.Document) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.warn)"); CompiledName("warn_5")>]
            member __.warn with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_5 =


    type FunScript.TypeScript.virtualDom.Thunk with 

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


    type FunScript.TypeScript.virtualDom.VHook with 

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


    type FunScript.TypeScript.virtualDom.VNode with 

            [<FunScript.JSEmitInline("({0}.tagName)"); CompiledName("tagName_1")>]
            member __.tagName with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.properties)"); CompiledName("properties")>]
            member __.properties with get() : FunScript.TypeScript.virtualDom.VProperties = failwith "never" and set (v : FunScript.TypeScript.virtualDom.VProperties) : unit = failwith "never"
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


    type FunScript.TypeScript.virtualDom.VPatch with 

            [<FunScript.JSEmitInline("({0}.vNode)"); CompiledName("vNode")>]
            member __.vNode with get() : FunScript.TypeScript.virtualDom.VNode = failwith "never" and set (v : FunScript.TypeScript.virtualDom.VNode) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.patch)"); CompiledName("patch")>]
            member __.patch with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("(new {0}({1}, {2}, {3}))"); CompiledName("Create_473")>]
            member __.Create(_type : float, vNode : FunScript.TypeScript.virtualDom.VNode, patch : obj) : FunScript.TypeScript.virtualDom.VPatch = failwith "never"
            [<FunScript.JSEmitInline("(new {0} = {1})"); CompiledName("Create_473Aux")>]
            member __.``Create <-``(func : System.Func<float, FunScript.TypeScript.virtualDom.VNode, obj, FunScript.TypeScript.virtualDom.VPatch>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.version)"); CompiledName("version_5")>]
            member __.version with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_38")>]
            member __._type with get() : float = failwith "never" and set (v : float) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_9 =


    type FunScript.TypeScript.virtualDom.VProperties with 

            [<FunScript.JSEmitInline("({0}.attributes)"); CompiledName("attributes_2")>]
            member __.attributes with get() : FunScript.TypeScript.virtualDom.AnonymousType441 = failwith "never" and set (v : FunScript.TypeScript.virtualDom.AnonymousType441) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.style)"); CompiledName("style_8")>]
            member __.style with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}[{1}])"); CompiledName("Item_43")>]
            member __.Item with get(i : string) : obj = failwith "never" and set (i : string) (v : obj) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_10 =


    type FunScript.TypeScript.virtualDom.VText with 

            [<FunScript.JSEmitInline("({0}.text)"); CompiledName("text_9")>]
            member __.text with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("(new {0}({1}))"); CompiledName("Create_474")>]
            member __.Create(text : obj) : FunScript.TypeScript.virtualDom.VText = failwith "never"
            [<FunScript.JSEmitInline("(new {0} = {1})"); CompiledName("Create_474Aux")>]
            member __.``Create <-``(func : System.Func<obj, FunScript.TypeScript.virtualDom.VText>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.version)"); CompiledName("version_6")>]
            member __.version with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_39")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_11 =


    type FunScript.TypeScript.virtualDom.Widget with 

            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_40")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.init())"); CompiledName("init")>]
            member __.init() : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("({0}.init = {1})"); CompiledName("initAux")>]
            member __.``init <-``(func : System.Func<FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.update({1}, {2}))"); CompiledName("update_3")>]
            member __.update(previous : FunScript.TypeScript.virtualDom.Widget, domNode : FunScript.TypeScript.Element) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.update = {1})"); CompiledName("update_3Aux")>]
            member __.``update <-``(func : System.Func<FunScript.TypeScript.virtualDom.Widget, FunScript.TypeScript.Element, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.destroy({1}))"); CompiledName("destroy")>]
            member __.destroy(node : FunScript.TypeScript.Element) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.destroy = {1})"); CompiledName("destroyAux")>]
            member __.``destroy <-``(func : System.Func<FunScript.TypeScript.Element, unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_12 =


    type FunScript.TypeScript.virtualDom.createProperties with 

            [<FunScript.JSEmitInline("({0}.key)"); CompiledName("key_6")>]
            member __.key with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.namespace)"); CompiledName("_namespace_1")>]
            member __._namespace with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_virtual_dom_13 =


    type FunScript.TypeScript.virtualDom.Globals with 

            [<FunScript.JSEmitInline("(virtualDom.create({0}, {?1}))"); CompiledName("create_5")>]
            static member create(vnode : FunScript.TypeScript.virtualDom.VText, ?opts : FunScript.TypeScript.virtualDom.AnonymousType442) : FunScript.TypeScript.Text = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create = {0})"); CompiledName("create_5Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.virtualDom.VText, FunScript.TypeScript.virtualDom.AnonymousType442, FunScript.TypeScript.Text>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create({0}, {?1}))"); CompiledName("create_6")>]
            static member create(vnode : FunScript.TypeScript.virtualDom.VNode, ?opts : FunScript.TypeScript.virtualDom.AnonymousType443) : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create = {0})"); CompiledName("create_6Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.virtualDom.VNode, FunScript.TypeScript.virtualDom.AnonymousType443, FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create({0}, {?1}))"); CompiledName("create_7")>]
            static member create(vnode : FunScript.TypeScript.virtualDom.Widget, ?opts : FunScript.TypeScript.virtualDom.AnonymousType444) : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create = {0})"); CompiledName("create_7Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.virtualDom.Widget, FunScript.TypeScript.virtualDom.AnonymousType444, FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create({0}, {?1}))"); CompiledName("create_8")>]
            static member create(vnode : FunScript.TypeScript.virtualDom.Thunk, ?opts : FunScript.TypeScript.virtualDom.AnonymousType445) : FunScript.TypeScript.Element = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.create = {0})"); CompiledName("create_8Aux")>]
            static member ``create <-``(func : System.Func<FunScript.TypeScript.virtualDom.Thunk, FunScript.TypeScript.virtualDom.AnonymousType445, FunScript.TypeScript.Element>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.h({0}, {1}, {2}))"); CompiledName("h")>]
            static member h(tagName : string, properties : FunScript.TypeScript.virtualDom.createProperties, children : array<obj>) : FunScript.TypeScript.virtualDom.VNode = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.h = {0})"); CompiledName("hAux")>]
            static member ``h <-``(func : System.Func<string, FunScript.TypeScript.virtualDom.createProperties, array<obj>, FunScript.TypeScript.virtualDom.VNode>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.h({0}, {1}))"); CompiledName("h_1")>]
            static member h(tagName : string, children : array<obj>) : FunScript.TypeScript.virtualDom.VNode = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.h = {0})"); CompiledName("h_1Aux")>]
            static member ``h <-``(func : System.Func<string, array<obj>, FunScript.TypeScript.virtualDom.VNode>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Thunk, right : FunScript.TypeScript.virtualDom.Thunk) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diffAux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Thunk, FunScript.TypeScript.virtualDom.Thunk, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_1")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Thunk, right : FunScript.TypeScript.virtualDom.Widget) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_1Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Thunk, FunScript.TypeScript.virtualDom.Widget, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_2")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Thunk, right : FunScript.TypeScript.virtualDom.VText) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_2Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Thunk, FunScript.TypeScript.virtualDom.VText, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_3")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Thunk, right : FunScript.TypeScript.virtualDom.VNode) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_3Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Thunk, FunScript.TypeScript.virtualDom.VNode, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_4")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Widget, right : FunScript.TypeScript.virtualDom.Thunk) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_4Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Widget, FunScript.TypeScript.virtualDom.Thunk, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_5")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Widget, right : FunScript.TypeScript.virtualDom.Widget) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_5Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Widget, FunScript.TypeScript.virtualDom.Widget, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_6")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Widget, right : FunScript.TypeScript.virtualDom.VText) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_6Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Widget, FunScript.TypeScript.virtualDom.VText, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_7")>]
            static member diff(left : FunScript.TypeScript.virtualDom.Widget, right : FunScript.TypeScript.virtualDom.VNode) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_7Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.Widget, FunScript.TypeScript.virtualDom.VNode, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_8")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VText, right : FunScript.TypeScript.virtualDom.Thunk) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_8Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VText, FunScript.TypeScript.virtualDom.Thunk, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_9")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VText, right : FunScript.TypeScript.virtualDom.Widget) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_9Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VText, FunScript.TypeScript.virtualDom.Widget, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_10")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VText, right : FunScript.TypeScript.virtualDom.VText) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_10Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VText, FunScript.TypeScript.virtualDom.VText, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_11")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VText, right : FunScript.TypeScript.virtualDom.VNode) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_11Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VText, FunScript.TypeScript.virtualDom.VNode, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_12")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VNode, right : FunScript.TypeScript.virtualDom.Thunk) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_12Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VNode, FunScript.TypeScript.virtualDom.Thunk, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_13")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VNode, right : FunScript.TypeScript.virtualDom.Widget) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_13Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VNode, FunScript.TypeScript.virtualDom.Widget, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_14")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VNode, right : FunScript.TypeScript.virtualDom.VText) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_14Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VNode, FunScript.TypeScript.virtualDom.VText, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff({0}, {1}))"); CompiledName("diff_15")>]
            static member diff(left : FunScript.TypeScript.virtualDom.VNode, right : FunScript.TypeScript.virtualDom.VNode) : array<FunScript.TypeScript.virtualDom.VPatch> = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.diff = {0})"); CompiledName("diff_15Aux")>]
            static member ``diff <-``(func : System.Func<FunScript.TypeScript.virtualDom.VNode, FunScript.TypeScript.virtualDom.VNode, array<FunScript.TypeScript.virtualDom.VPatch>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.patch({0}, {1}, {?2}))"); CompiledName("patch_1")>]
            static member patch<'T when 'T :> FunScript.TypeScript.Element>(rootNode : 'T, patches : array<FunScript.TypeScript.virtualDom.VPatch>, ?renderOptions : obj) : 'T = failwith "never"
            [<FunScript.JSEmitInline("(virtualDom.patch = {0})"); CompiledName("patch_1Aux")>]
            static member ``patch <-``<'T when 'T :> FunScript.TypeScript.Element>(func : System.Func<'T, array<FunScript.TypeScript.virtualDom.VPatch>, obj, 'T>) : unit = failwith "never"
