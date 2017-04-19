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

export type IPin = any;

export interface IIris {
  startContext(f: (info:any)=>void): void
  subscribeToLogs(f: (log: string)=>void): IDisposable
  subscribeToClock(f: (frames: number)=>void): IDisposable
  getClientContext(): IContext
  pinToKeyValuePairs(pin: IPin): [string, any][]
  updateSlices(pin: IPin, rowIndex: number, newValue: any)
  removeMember(projectConfig: any, memberId: any)
}