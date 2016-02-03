namespace Iris.Core.Types

open LibGit2Sharp

[<AutoOpen>]
module Context =

  type Context() as self =
    [<DefaultValue>] val mutable Signature : Signature option
    [<DefaultValue>] val mutable Project   : Project   option

    do
      self.Project <- None

    member self.LoadProject(path : FilePath) : unit =
      self.Project <- loadProject path 

    member self.SaveProject(msg : string) : unit =
      if Option.isSome self.Signature
      then 
        let signature = Option.get self.Signature
        match self.Project with
          | Some(project) -> saveProject project signature msg
          | _ -> printfn "No project loaded."
      else printfn "Unable to save project. No signature supplied."

    member self.CreateProject(name : Name, path : FilePath) =
      let project = createProject name
      project.Path <- Some(path)
      self.Project <- Some(project)
      self.SaveProject(sprintf "Created %s" name)
      
