
namespace FunScript.TypeScript.autobahn
type Connection = interface end

namespace FunScript.TypeScript.autobahn
type ICallOptions = interface end

namespace FunScript.TypeScript.autobahn
type ICloseEventDetails = interface end

namespace FunScript.TypeScript.autobahn
type IConnectionOptions = interface end

namespace FunScript.TypeScript.autobahn
type IError = interface end

namespace FunScript.TypeScript.autobahn
type IEvent = interface end

namespace FunScript.TypeScript.autobahn
type IInvocation = interface end

namespace FunScript.TypeScript.autobahn
type ILog = interface end

namespace FunScript.TypeScript.autobahn
type IPublication = interface end

namespace FunScript.TypeScript.autobahn
type IPublishOptions = interface end

namespace FunScript.TypeScript.autobahn
type IRegisterOptions = interface end

namespace FunScript.TypeScript.autobahn
type IRegistration = interface end

namespace FunScript.TypeScript.autobahn
type IResult = interface end

namespace FunScript.TypeScript.autobahn
type ISubscribeOptions = interface end

namespace FunScript.TypeScript.autobahn
type ISubscription = interface end

namespace FunScript.TypeScript.autobahn
type ITransport = interface end

namespace FunScript.TypeScript.autobahn
type ITransportDefinition = interface end

namespace FunScript.TypeScript.autobahn
type ITransportFactory = interface end

namespace FunScript.TypeScript.autobahn
type ITransports = interface end

namespace FunScript.TypeScript.autobahn
type IUtil = interface end

namespace FunScript.TypeScript.autobahn
type Session = interface end

namespace FunScript.TypeScript.autobahn
type Globals = interface end


namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_0 =


    type FunScript.TypeScript.autobahn.Connection with 

            [<FunScript.JSEmitInline("(new autobahn.Connection({?0}))"); CompiledName("Create_473")>]
            static member Create(?options : FunScript.TypeScript.autobahn.IConnectionOptions) : FunScript.TypeScript.autobahn.Connection = failwith "never"
            [<FunScript.JSEmitInline("(new autobahn.Connection = {0})"); CompiledName("Create_473Aux")>]
            static member ``Create <-``(func : System.Func<FunScript.TypeScript.autobahn.IConnectionOptions, FunScript.TypeScript.autobahn.Connection>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.open())"); CompiledName("_open_6")>]
            member __._open() : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.open = {1})"); CompiledName("_open_6Aux")>]
            member __.``_open <-``(func : System.Func<unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.close({1}, {2}))"); CompiledName("close_8")>]
            member __.close(reason : string, message : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.close = {1})"); CompiledName("close_8Aux")>]
            member __.``close <-``(func : System.Func<string, string, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.onopen)"); CompiledName("onopen_1")>]
            member __.onopen with get() : System.Func<FunScript.TypeScript.autobahn.Session, obj, unit> = failwith "never" and set (v : System.Func<FunScript.TypeScript.autobahn.Session, obj, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.onclose)"); CompiledName("onclose_1")>]
            member __.onclose with get() : System.Func<string, obj, bool> = failwith "never" and set (v : System.Func<string, obj, bool>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_1 =


    type FunScript.TypeScript.autobahn.ICallOptions with 

            [<FunScript.JSEmitInline("({0}.timeout)"); CompiledName("timeout_3")>]
            member __.timeout with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.receive_progress)"); CompiledName("receive_progress")>]
            member __.receive_progress with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.disclose_me)"); CompiledName("disclose_me")>]
            member __.disclose_me with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_2 =


    type FunScript.TypeScript.autobahn.ICloseEventDetails with 

            [<FunScript.JSEmitInline("({0}.wasClean)"); CompiledName("wasClean_1")>]
            member __.wasClean with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.reason)"); CompiledName("reason_2")>]
            member __.reason with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.code)"); CompiledName("code_10")>]
            member __.code with get() : float = failwith "never" and set (v : float) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_3 =


    type FunScript.TypeScript.autobahn.IConnectionOptions with 

            [<FunScript.JSEmitInline("({0}.use_es6_promises)"); CompiledName("use_es6_promises")>]
            member __.use_es6_promises with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.use_deferred)"); CompiledName("use_deferred")>]
            member __.use_deferred with get() : System.Func<obj> = failwith "never" and set (v : System.Func<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.transports)"); CompiledName("transports")>]
            member __.transports with get() : array<FunScript.TypeScript.autobahn.ITransportDefinition> = failwith "never" and set (v : array<FunScript.TypeScript.autobahn.ITransportDefinition>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.retry_if_unreachable)"); CompiledName("retry_if_unreachable")>]
            member __.retry_if_unreachable with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.max_retries)"); CompiledName("max_retries")>]
            member __.max_retries with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.initial_retry_delay)"); CompiledName("initial_retry_delay")>]
            member __.initial_retry_delay with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.max_retry_delay)"); CompiledName("max_retry_delay")>]
            member __.max_retry_delay with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.retry_delay_growth)"); CompiledName("retry_delay_growth")>]
            member __.retry_delay_growth with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.retry_delay_jitter)"); CompiledName("retry_delay_jitter")>]
            member __.retry_delay_jitter with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.url)"); CompiledName("url_4")>]
            member __.url with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.protocols)"); CompiledName("protocols")>]
            member __.protocols with get() : array<string> = failwith "never" and set (v : array<string>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.realm)"); CompiledName("realm")>]
            member __.realm with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.authmethods)"); CompiledName("authmethods")>]
            member __.authmethods with get() : array<string> = failwith "never" and set (v : array<string>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.authid)"); CompiledName("authid")>]
            member __.authid with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_4 =


    type FunScript.TypeScript.autobahn.IError with 

            [<FunScript.JSEmitInline("({0}.error)"); CompiledName("error_10")>]
            member __.error with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.args)"); CompiledName("args")>]
            member __.args with get() : array<obj> = failwith "never" and set (v : array<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.kwargs)"); CompiledName("kwargs")>]
            member __.kwargs with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_5 =


    type FunScript.TypeScript.autobahn.IEvent with 

            [<FunScript.JSEmitInline("({0}.publication)"); CompiledName("publication")>]
            member __.publication with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.publisher)"); CompiledName("publisher")>]
            member __.publisher with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.topic)"); CompiledName("topic")>]
            member __.topic with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_6 =


    type FunScript.TypeScript.autobahn.IInvocation with 

            [<FunScript.JSEmitInline("({0}.caller)"); CompiledName("caller_1")>]
            member __.caller with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.progress)"); CompiledName("progress")>]
            member __.progress with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.procedure)"); CompiledName("procedure")>]
            member __.procedure with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_7 =


    type FunScript.TypeScript.autobahn.ILog with 

            [<FunScript.JSEmitInline("({0}.debug())"); CompiledName("debug_2")>]
            member __.debug() : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.debug = {1})"); CompiledName("debug_2Aux")>]
            member __.``debug <-``(func : System.Func<unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.debug({1...}))"); CompiledName("debug_3")>]
            member __.debugOverload2([<System.ParamArray>] args : array<obj>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_8 =


    type FunScript.TypeScript.autobahn.IPublication with 

            [<FunScript.JSEmitInline("({0}.id)"); CompiledName("id_5")>]
            member __.id with get() : float = failwith "never" and set (v : float) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_9 =


    type FunScript.TypeScript.autobahn.IPublishOptions with 

            [<FunScript.JSEmitInline("({0}.exclude)"); CompiledName("exclude")>]
            member __.exclude with get() : array<float> = failwith "never" and set (v : array<float>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.eligible)"); CompiledName("eligible")>]
            member __.eligible with get() : array<float> = failwith "never" and set (v : array<float>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.disclose_me)"); CompiledName("disclose_me_1")>]
            member __.disclose_me with get() : FunScript.TypeScript.Boolean = failwith "never" and set (v : FunScript.TypeScript.Boolean) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_10 =


    type FunScript.TypeScript.autobahn.IRegisterOptions with 

            [<FunScript.JSEmitInline("({0}.disclose_caller)"); CompiledName("disclose_caller")>]
            member __.disclose_caller with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_11 =


    type FunScript.TypeScript.autobahn.IRegistration with 

            [<FunScript.JSEmitInline("({0}.procedure)"); CompiledName("procedure_1")>]
            member __.procedure with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.endpoint)"); CompiledName("endpoint")>]
            member __.endpoint with get() : System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IInvocation, unit> = failwith "never" and set (v : System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IInvocation, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.options)"); CompiledName("options_2")>]
            member __.options with get() : FunScript.TypeScript.autobahn.IRegisterOptions = failwith "never" and set (v : FunScript.TypeScript.autobahn.IRegisterOptions) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.session)"); CompiledName("session")>]
            member __.session with get() : FunScript.TypeScript.autobahn.Session = failwith "never" and set (v : FunScript.TypeScript.autobahn.Session) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.id)"); CompiledName("id_6")>]
            member __.id with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.active)"); CompiledName("active")>]
            member __.active with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unregister())"); CompiledName("unregister")>]
            member __.unregister() : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unregister = {1})"); CompiledName("unregisterAux")>]
            member __.``unregister <-``(func : System.Func<unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_12 =


    type FunScript.TypeScript.autobahn.IResult with 

            [<FunScript.JSEmitInline("({0}.args)"); CompiledName("args_1")>]
            member __.args with get() : array<obj> = failwith "never" and set (v : array<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.kwargs)"); CompiledName("kwargs_1")>]
            member __.kwargs with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_13 =


    type FunScript.TypeScript.autobahn.ISubscribeOptions with 

            [<FunScript.JSEmitInline("({0}.match)"); CompiledName("_match_2")>]
            member __._match with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_14 =


    type FunScript.TypeScript.autobahn.ISubscription with 

            [<FunScript.JSEmitInline("({0}.topic)"); CompiledName("topic_1")>]
            member __.topic with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.handler)"); CompiledName("handler")>]
            member __.handler with get() : System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IEvent, unit> = failwith "never" and set (v : System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IEvent, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.options)"); CompiledName("options_3")>]
            member __.options with get() : FunScript.TypeScript.autobahn.ISubscribeOptions = failwith "never" and set (v : FunScript.TypeScript.autobahn.ISubscribeOptions) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.session)"); CompiledName("session_1")>]
            member __.session with get() : FunScript.TypeScript.autobahn.Session = failwith "never" and set (v : FunScript.TypeScript.autobahn.Session) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.id)"); CompiledName("id_7")>]
            member __.id with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.active)"); CompiledName("active_1")>]
            member __.active with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unsubscribe())"); CompiledName("unsubscribe")>]
            member __.unsubscribe() : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unsubscribe = {1})"); CompiledName("unsubscribeAux")>]
            member __.``unsubscribe <-``(func : System.Func<unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_15 =


    type FunScript.TypeScript.autobahn.ITransport with 

            [<FunScript.JSEmitInline("({0}.onopen)"); CompiledName("onopen_2")>]
            member __.onopen with get() : System.Func<unit> = failwith "never" and set (v : System.Func<unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.onmessage)"); CompiledName("onmessage_7")>]
            member __.onmessage with get() : System.Func<array<obj>, unit> = failwith "never" and set (v : System.Func<array<obj>, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.onclose)"); CompiledName("onclose_2")>]
            member __.onclose with get() : System.Func<FunScript.TypeScript.autobahn.ICloseEventDetails, unit> = failwith "never" and set (v : System.Func<FunScript.TypeScript.autobahn.ICloseEventDetails, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.send({1}))"); CompiledName("send_3")>]
            member __.send(message : array<obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.send = {1})"); CompiledName("send_3Aux")>]
            member __.``send <-``(func : System.Func<array<obj>, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.close({1}, {?2}))"); CompiledName("close_9")>]
            member __.close(errorCode : float, ?reason : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.close = {1})"); CompiledName("close_9Aux")>]
            member __.``close <-``(func : System.Func<float, string, unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_16 =


    type FunScript.TypeScript.autobahn.ITransportDefinition with 

            [<FunScript.JSEmitInline("({0}.url)"); CompiledName("url_5")>]
            member __.url with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.protocols)"); CompiledName("protocols_1")>]
            member __.protocols with get() : array<string> = failwith "never" and set (v : array<string>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_36")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_17 =


    type FunScript.TypeScript.autobahn.ITransportFactory with 

            [<FunScript.JSEmitInline("({0}.type)"); CompiledName("_type_37")>]
            member __._type with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.create())"); CompiledName("create_5")>]
            member __.create() : FunScript.TypeScript.autobahn.ITransport = failwith "never"
            [<FunScript.JSEmitInline("({0}.create = {1})"); CompiledName("create_5Aux")>]
            member __.``create <-``(func : System.Func<FunScript.TypeScript.autobahn.ITransport>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_18 =


    type FunScript.TypeScript.autobahn.ITransports with 

            [<FunScript.JSEmitInline("({0}.register({1}, {2}))"); CompiledName("register")>]
            member __.register(name : string, factory : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.register = {1})"); CompiledName("registerAux")>]
            member __.``register <-``(func : System.Func<string, obj, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.isRegistered({1}))"); CompiledName("isRegistered")>]
            member __.isRegistered(name : string) : bool = failwith "never"
            [<FunScript.JSEmitInline("({0}.isRegistered = {1})"); CompiledName("isRegisteredAux")>]
            member __.``isRegistered <-``(func : System.Func<string, bool>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.get({1}))"); CompiledName("get_13")>]
            member __.get(name : string) : obj = failwith "never"
            [<FunScript.JSEmitInline("({0}.get = {1})"); CompiledName("get_13Aux")>]
            member __.``get <-``(func : System.Func<string, obj>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.list())"); CompiledName("list_1")>]
            member __.list() : array<obj> = failwith "never"
            [<FunScript.JSEmitInline("({0}.list = {1})"); CompiledName("list_1Aux")>]
            member __.``list <-``(func : System.Func<array<obj>>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_19 =


    type FunScript.TypeScript.autobahn.IUtil with 

            [<FunScript.JSEmitInline("({0}.assert({1}, {2}))"); CompiledName("_assert_2")>]
            member __._assert(condition : bool, message : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.assert = {1})"); CompiledName("_assert_2Aux")>]
            member __.``_assert <-``(func : System.Func<bool, string, unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_20 =


    type FunScript.TypeScript.autobahn.Session with 

            [<FunScript.JSEmitInline("({0}.id)"); CompiledName("id_8")>]
            member __.id with get() : float = failwith "never" and set (v : float) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.realm)"); CompiledName("realm_1")>]
            member __.realm with get() : string = failwith "never" and set (v : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.isOpen)"); CompiledName("isOpen_1")>]
            member __.isOpen with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.features)"); CompiledName("features")>]
            member __.features with get() : obj = failwith "never" and set (v : obj) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.caller_disclose_me)"); CompiledName("caller_disclose_me")>]
            member __.caller_disclose_me with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.publisher_disclose_me)"); CompiledName("publisher_disclose_me")>]
            member __.publisher_disclose_me with get() : bool = failwith "never" and set (v : bool) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.subscriptions)"); CompiledName("subscriptions")>]
            member __.subscriptions with get() : array<array<FunScript.TypeScript.autobahn.ISubscription>> = failwith "never" and set (v : array<array<FunScript.TypeScript.autobahn.ISubscription>>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.registrations)"); CompiledName("registrations")>]
            member __.registrations with get() : array<FunScript.TypeScript.autobahn.IRegistration> = failwith "never" and set (v : array<FunScript.TypeScript.autobahn.IRegistration>) : unit = failwith "never"
            [<FunScript.JSEmitInline("(new autobahn.Session({0}, {1}, {2}))"); CompiledName("Create_474")>]
            static member Create(transport : FunScript.TypeScript.autobahn.ITransport, defer : System.Func<obj>, challenge : System.Func<obj>) : FunScript.TypeScript.autobahn.Session = failwith "never"
            [<FunScript.JSEmitInline("(new autobahn.Session = {0})"); CompiledName("Create_474Aux")>]
            static member ``Create <-``(func : System.Func<FunScript.TypeScript.autobahn.ITransport, System.Func<obj>, System.Func<obj>, FunScript.TypeScript.autobahn.Session>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.join({1}, {2}, {3}))"); CompiledName("join_1")>]
            member __.join(realm : string, authmethods : array<string>, authid : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.join = {1})"); CompiledName("join_1Aux")>]
            member __.``join <-``(func : System.Func<string, array<string>, string, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.leave({1}, {2}))"); CompiledName("leave")>]
            member __.leave(reason : string, message : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.leave = {1})"); CompiledName("leaveAux")>]
            member __.``leave <-``(func : System.Func<string, string, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.call({1}, {?2}, {?3}, {?4}))"); CompiledName("call_2")>]
            member __.call<'TResult>(procedure : string, ?args : array<obj>, ?kwargs : obj, ?options : FunScript.TypeScript.autobahn.ICallOptions) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.call = {1})"); CompiledName("call_2Aux")>]
            member __.``call <-``<'TResult>(func : System.Func<string, array<obj>, obj, FunScript.TypeScript.autobahn.ICallOptions, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.publish({1}, {?2}, {?3}, {?4}))"); CompiledName("publish")>]
            member __.publish(topic : string, ?args : array<obj>, ?kwargs : obj, ?options : FunScript.TypeScript.autobahn.IPublishOptions) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.publish = {1})"); CompiledName("publishAux")>]
            member __.``publish <-``(func : System.Func<string, array<obj>, obj, FunScript.TypeScript.autobahn.IPublishOptions, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.subscribe({1}, {2}, {?3}))"); CompiledName("subscribe")>]
            member __.subscribe(topic : string, handler : System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IEvent, unit>, ?options : FunScript.TypeScript.autobahn.ISubscribeOptions) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.subscribe = {1})"); CompiledName("subscribeAux")>]
            member __.``subscribe <-``(func : System.Func<string, System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IEvent, unit>, FunScript.TypeScript.autobahn.ISubscribeOptions, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.register({1}, {2}, {?3}))"); CompiledName("register_1")>]
            member __.register(procedure : string, endpoint : System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IInvocation, unit>, ?options : FunScript.TypeScript.autobahn.IRegisterOptions) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.register = {1})"); CompiledName("register_1Aux")>]
            member __.``register <-``(func : System.Func<string, System.Func<array<obj>, obj, FunScript.TypeScript.autobahn.IInvocation, unit>, FunScript.TypeScript.autobahn.IRegisterOptions, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unsubscribe({1}))"); CompiledName("unsubscribe_1")>]
            member __.unsubscribe(subscription : FunScript.TypeScript.autobahn.ISubscription) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unsubscribe = {1})"); CompiledName("unsubscribe_1Aux")>]
            member __.``unsubscribe <-``(func : System.Func<FunScript.TypeScript.autobahn.ISubscription, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unregister({1}))"); CompiledName("unregister_1")>]
            member __.unregister(registration : FunScript.TypeScript.autobahn.IRegistration) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.unregister = {1})"); CompiledName("unregister_1Aux")>]
            member __.``unregister <-``(func : System.Func<FunScript.TypeScript.autobahn.IRegistration, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.prefix({1}, {2}))"); CompiledName("prefix_2")>]
            member __.prefix(prefix : string, uri : string) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.prefix = {1})"); CompiledName("prefix_2Aux")>]
            member __.``prefix <-``(func : System.Func<string, string, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.resolve({1}))"); CompiledName("resolve")>]
            member __.resolve(curie : string) : string = failwith "never"
            [<FunScript.JSEmitInline("({0}.resolve = {1})"); CompiledName("resolveAux")>]
            member __.``resolve <-``(func : System.Func<string, string>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.onjoin)"); CompiledName("onjoin")>]
            member __.onjoin with get() : System.Func<obj, unit> = failwith "never" and set (v : System.Func<obj, unit>) : unit = failwith "never"
            [<FunScript.JSEmitInline("({0}.onleave)"); CompiledName("onleave")>]
            member __.onleave with get() : System.Func<string, obj, unit> = failwith "never" and set (v : System.Func<string, obj, unit>) : unit = failwith "never"

namespace FunScript.TypeScript

[<AutoOpen>]
module TypeExtensions_autobahn_21 =


    type FunScript.TypeScript.autobahn.Globals with 

            [<FunScript.JSEmitInline("(autobahn.util)"); CompiledName("util")>]
            static member util with get() : FunScript.TypeScript.autobahn.IUtil = failwith "never" and set (v : FunScript.TypeScript.autobahn.IUtil) : unit = failwith "never"
            [<FunScript.JSEmitInline("(autobahn.log)"); CompiledName("log_3")>]
            static member log with get() : FunScript.TypeScript.autobahn.ILog = failwith "never" and set (v : FunScript.TypeScript.autobahn.ILog) : unit = failwith "never"
            [<FunScript.JSEmitInline("(autobahn.transports)"); CompiledName("transports_1")>]
            static member transports with get() : FunScript.TypeScript.autobahn.ITransports = failwith "never" and set (v : FunScript.TypeScript.autobahn.ITransports) : unit = failwith "never"
