namespace Iris.Core

open System 

type BrowserSession =
  { SessionId: Guid
  ; UserName:  UserName
  ; IPAddress: IpAddress
  ; UserAgent: UserAgent
  }
