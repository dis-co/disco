namespace Iris.Core

// __     __
// \ \   / /_   ____   ____   __
//  \ \ / /\ \ / /\ \ / /\ \ / /
//   \ V /  \ V /  \ V /  \ V /
//    \_/    \_/    \_/    \_/

type VvvvExe =
  { Executable : FilePath
  ; Version    : Version
  ; Required   : bool 
  }

type VvvvPlugin =
  { Name : Name
  ; Path : FilePath
  }

