namespace Iris.Core
//  ____            _
// |  _ \ ___  __ _(_) ___  _ __
// | |_) / _ \/ _` | |/ _ \| '_ \
// |  _ <  __/ (_| | | (_) | | | |
// |_| \_\___|\__, |_|\___/|_| |_|
//            |___/

type Region =
  { Id             : Id
  ; Name           : Name
  ; SrcPosition    : Coordinate
  ; SrcSize        : Rect
  ; OutputPosition : Coordinate
  ; OutputSize     : Rect
  }

//  ____            _             __  __
// |  _ \ ___  __ _(_) ___  _ __ |  \/  | __ _ _ __
// | |_) / _ \/ _` | |/ _ \| '_ \| |\/| |/ _` | '_ \
// |  _ <  __/ (_| | | (_) | | | | |  | | (_| | |_) |
// |_| \_\___|\__, |_|\___/|_| |_|_|  |_|\__,_| .__/
//            |___/                           |_|

type RegionMap =
  { SrcViewportId : Id
  ; Regions       : Region list
  }
