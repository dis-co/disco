import { GlobalModel } from "../fable/Frontend/GlobalModel.fs";
export default GlobalModel;

/*
import { IIris, IDisposable, IServiceInfo, IService, IProject } from "./Interfaces"

declare var IrisLib: IIris;

let counter = 0;
const LOG_MAX = 100;

export default class GlobalModel {
  logSubscription: IDisposable;
  clockSubscription: IDisposable;
  subscribers: Map<string, Map<number, (x:any)=>void>>;
  eventSubscribers: Map<string, Map<number, (x:any)=>void>>;
  state: {
    logs: [number, string][],
    tabs: Map<number,any>,
    widgets: Map<number,any>,
    useRightClick: boolean,
    serviceInfo: IServiceInfo,
    clock: number,
    project: IProject,
    services: IService[]
  };

  constructor() {
    IrisLib.startContext(info => {
      if (this.logSubscription == null) {
        this.logSubscription = IrisLib.subscribeToLogs(log => {
          this.addLog(log);
        })
      }
      if (this.clockSubscription == null) {
        this.clockSubscription = IrisLib.subscribeToClock(frames =>
          this.__setState("clock", frames)
        )
      }
      if (this.state.serviceInfo.version === "0.0.0") {
        const ctx = IrisLib.getClientContext();
        this.__setState("serviceInfo", ctx.ServiceInfo);
      }
      this.__setState("pinGroups", info != null ? info.state.PinGroups : null);
      this.__setState("project", info != null ? info.state.Project : null);
      this.__setState("services", info != null ? info.state.DiscoveredServices : null);
    });

    this.subscribers = new Map();
    this.eventSubscribers = new Map();
    this.state = {
      logs: [],
      tabs: new Map(),
      widgets: new Map(),
      useRightClick: false,
      serviceInfo: {
        version: "0.0.0",
        buildNumber: "0"
      },
      clock: 0,
      project: null,
      services: []
    };
  }

  subscribe(keys: string | string[], subscriber: (x:any)=>void) {
    let subscribers = this.subscribers, disposables: IDisposable[] = [];
    if (typeof keys === "string") {
      keys = [keys];
    }
    for (let key of keys) {
      let id = counter++;
      if (!subscribers.has(key)) {
        subscribers.set(key, new Map());
      }
      subscribers.get(key).set(id, subscriber);
      disposables.push({
        // `subscribers` must be captured so the closure below works
        dispose() {
          subscribers.get(key).delete(id);
        }
      })
    }
    return {
      dispose() {
        disposables.forEach(x => x.dispose());
      }
    }
  }

  subscribeToEvent(event: string, subscriber: (x:any)=>void) {
    let id = counter++, subscribers = this.eventSubscribers;
    if (!subscribers.has(event)) {
      subscribers.set(event, new Map());
    }
    subscribers.get(event).set(id, subscriber);
    console.log("Subscription to event", event)
    // `subscribers` must be captured so the closure below works
    return {
      dispose() {
        subscribers.get(event).delete(id);
      }
    }
  }

  __notify(key: string, value: any = (this.state as any)[key]) {
    if (this.subscribers.has(key)) {
      this.subscribers.get(key).forEach(subscriber => subscriber(value));
    }
  }

  __setState(key: string, value: any) {
    (this.state as any)[key] = value;
    this.__notify(key, value);
  }

  useRightClick(value: boolean) {
    this.__setState("useRightClick", value);
  }

  addWidget(id: number, widget: any) {
    if (widget === void 0) {
      widget = id;
      id = counter++;
    }
    this.state.widgets.set(id, widget);
    this.__notify("widgets");
    return id;
  }

  removeWidget(id: number) {
    this.state.widgets.delete(id);
    this.__notify("widgets");
  }

  addTab(id: number, tab: any) {
    if (tab === void 0) {
      tab = id;
      id = counter++;
    }
    this.state.tabs.set(id, tab);
    this.__notify("tabs");
    return id;
  }

  removeTab(id: any) {
    this.state.tabs.delete(id);
    this.__notify("tabs");
  }

  addLog(log: string) {
    var logs = this.state.logs;
    if (logs.length > LOG_MAX) {
      var diff = Math.floor(LOG_MAX / 100);
      logs.splice(logs.length - diff, diff);
    }
    logs.splice(0, 0, [counter++, log]);
    this.__notify("logs", logs);
  }

  triggerEvent(event: string, data: any) {
    if (this.eventSubscribers.has(event)) {
      this.eventSubscribers.get(event).forEach(subscriber => subscriber(data));
    }
  }
}
*/