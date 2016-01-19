using System;
using System.Collections.Generic;
using System.Linq;

namespace Iris.Core.Types
{
    public class PinSlices : List<PinSlice>
    {
        /// <summary>
        /// Serialize a PinSlices collection to a Spread-compatible string.
        /// </summary>
        /// <returns>Spread string</returns>
        public string ToSpread()
        {
            return this.Select(pv => pv.ToSpread())
                .Aggregate(String.Empty, (s1, s2) => {
                        if(s1 == String.Empty)
                            return s2;
                        return s1 + "," + s2;
                    });
        }

        public string ToSpread(int idx)
        {
            return this.Select(pv => pv.ToSpread(idx))
                .Aggregate(String.Empty, (s1, s2) => {
                        if(s1 == String.Empty)
                            return s2;
                        return s1 + "," + s2;
                    });
        }
    }
}
