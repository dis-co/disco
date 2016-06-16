namespace Iris.Core

//  ____  _           _
// |  _ \(_)___ _ __ | | __ _ _   _
// | | | | / __| '_ \| |/ _` | | | |
// | |_| | \__ \ |_) | | (_| | |_| |
// |____/|_|___/ .__/|_|\__,_|\__, |
//             |_|            |___/

type Display =
  { Id        : Id
  ; Name      : Name
  ; Size      : Rect
  ; Signals   : Signal list
  ; RegionMap : RegionMap
  }
