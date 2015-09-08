/// Type definitions for AutobahnJS v0.9.6
// Project: http://autobahn.ws/js/
// Definitions by: Elad Zelingher <https://github.com/darkl/>, Andy Hawkins <https://github.com/a904guy/,http://a904guy.com/,http://www.bmbsqd.com>
// Definitions: https://github.com/borisyankov/DefinitelyTyped

declare module autobahn {

    export class Session {
        id: number;
        realm: string;
        isOpen: boolean;
        features: any;
        caller_disclose_me: boolean;
        publisher_disclose_me: boolean;
        subscriptions: ISubscription[][];
        registrations: IRegistration[];

        constructor(transport: ITransport, defer: () => any, challenge: () => any);

        join(realm: string, authmethods: string[], authid: string): void;

        leave(reason: string, message: string): void;

        call<TResult>(procedure: string, args?: any[], kwargs?: any, options?: ICallOptions): void;

        publish(topic: string, args?: any[], kwargs?: any, options?: IPublishOptions): void;

        subscribe(topic: string, handler: (args?: any[], kwargs?: any, details?: IEvent) => void, options?: ISubscribeOptions): void;

        register(procedure: string, endpoint: (args?: any[], kwargs?: any, details?: IInvocation) => void, options?: IRegisterOptions): void;

        unsubscribe(subscription: ISubscription): void;

        unregister(registration: IRegistration): void;

        prefix(prefix: string, uri: string): void;

        resolve(curie: string): string;

        onjoin: (roleFeatures: any) => void;
        onleave: (reason: string, details: any) => void;
    }

    interface IInvocation {
        caller?: number;
        progress?: boolean;
        procedure: string;
    }

    interface IEvent {
        publication: number;
        publisher?: number;
        topic: string;
    }

    interface IResult {
        args: any[];
        kwargs: any;
    }

    interface IError {
        error: string;
        args: any[];
        kwargs: any;
    }

    interface ISubscription {
        topic: string;
        handler: (args?: any[], kwargs?: any, details?: IEvent) => void;
        options: ISubscribeOptions;
        session: Session;
        id: number;
        active: boolean;
        unsubscribe(): void;
    }

    interface IRegistration {
        procedure: string;
        endpoint: (args?: any[], kwargs?: any, details?: IInvocation) => void;
        options: IRegisterOptions;
        session: Session;
        id: number;
        active: boolean;
        unregister(): void;
    }

    interface IPublication {
        id: number;
    }

    interface ICallOptions {
        timeout?: number;
        receive_progress?: boolean;
        disclose_me?: boolean;
    }

    interface IPublishOptions {
        exclude?: number[];
        eligible?: number[];
        disclose_me? : Boolean;
    }

    interface ISubscribeOptions {
        match? : string;
    }

    interface IRegisterOptions {
        disclose_caller?: boolean;
    }

    export class Connection {
        constructor(options?: IConnectionOptions);

        open(): void;

        close(reason: string, message: string): void;

        onopen: (session: Session, details: any) => void;
        onclose: (reason: string, details: any) => boolean;
    }

    interface ITransportDefinition {
        url?: string;
        protocols?: string[];
        type: string;
    }

    interface IConnectionOptions {
        use_es6_promises?: boolean;
        use_deferred?: () => any;
        transports?: ITransportDefinition[];
        retry_if_unreachable?: boolean;
        max_retries?: number;
        initial_retry_delay?: number;
        max_retry_delay?: number;
        retry_delay_growth?: number;
        retry_delay_jitter?: number;
        url?: string;
        protocols?: string[];
        realm?: string;
        authmethods?: string[];
        authid?: string;
    }

    interface ICloseEventDetails {
        wasClean: boolean;
        reason: string;
        code: number;
    }

    interface ITransport {
        onopen: () => void;
        onmessage: (message: any[]) => void;
        onclose: (details: ICloseEventDetails) => void;

        send(message: any[]): void;
        close(errorCode: number, reason?: string): void;
    }

    interface ITransportFactory {
        //constructor(options: any);
        type: string;
		    create(): ITransport;
	  }

	  interface ITransports {
		    register(name: string, factory: any): void;
		    isRegistered(name: string): boolean;
		    get(name: string): any;
		    list(): any[];
	  }

	  interface ILog {
		    debug(...args: any[]): void;
	  }

	  interface IUtil {
		    assert(condition: boolean, message: string): void;
	  }

	  var util: IUtil;
	  var log: ILog;
	  var transports: ITransports;
}

declare module "autobahn" {
	  export = autobahn;
}
