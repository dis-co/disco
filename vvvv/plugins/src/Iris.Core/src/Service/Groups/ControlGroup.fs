namespace Iris.Service.Groups

[<AutoOpen>]
module ControlGroup =

  [<RequireQualifiedAccess>]
  type Actions =
    | Load 
    | Save
    | Clone  
    
    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Load   -> 1
          | Save   -> 2
          | Clone  -> 3
  

