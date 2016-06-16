namespace Iris.Core

// __     ___               ____            _
// \ \   / (_) _____      _|  _ \ ___  _ __| |_
//  \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
//   \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
//    \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

type ViewPort =
  { Id             : Id
  ; Name           : Name
  ; Position       : Coordinate
  ; Size           : Rect
  ; OutputPosition : Coordinate
  ; OutputSize     : Rect
  ; Overlap        : Rect
  ; Description    : string
  }
