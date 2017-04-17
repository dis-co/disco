export interface IDisposable {
  dispose(): void;
}

export interface ILayout {
  x: number, y: number,
  w: number, h: number,
  minW: number, maxW: number,
  minH: number, maxH: number
}

export interface IServiceInfo {
  version: string,
  buildNumber: string
}

export interface IContext {
  ServiceInfo: IServiceInfo
}

export interface Iris {
  startContext(f: (info:any)=>void): void
  subscribeToLogs(f: (log: string)=>void): IDisposable
  subscribeToClock(f: (frames: number)=>void): IDisposable
  getClientContext(): IContext;
}