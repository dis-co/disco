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

export interface IIris {
  toString(o: any): string
  startContext(f: (info:any)=>void): void
  subscribeToLogs(f: (log: string)=>void): IDisposable
  subscribeToClock(f: (frames: number)=>void): IDisposable
  getClientContext(): IContext
  pinToKeyValuePairs(pin: IPin): [string, any][]
  updateSlices(pin: IPin, rowIndex: number, newValue: any)
  removeMember(projectConfig: any, memberId: any)
  addMember(info: any)
}

// Temp placeholders
export type IPin = any;
export type IProject = any;
export type IService = any;

export interface Event {
  name: string
}

export interface DragEvent extends Event {
  type: "move" | "stop"
  x: number,
  y: number,
  origin?: any,
  model?: any
}