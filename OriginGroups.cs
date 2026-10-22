using System;
using System.Linq;

namespace AniimoAffinities {
    internal sealed class OriginEntry {
        internal int Type, Mask;
    }
    internal static class OriginGroups {
        // Inputs: selected types, direction and displayed targets. Output: unique targets ordered by contributing origins, as ElementOriginGroups in SaveOverride. Fallback: empty targets. Raises: invalid model indices.
        internal static OriginEntry[] Build(int main,int sub,int column,int[] targets) {
            var combined=Model.Factors(main,sub);
            var first=Model.Factors(main,-1);
            var second=sub<0?null:Model.Factors(sub,-1);
            return targets.Select(type=> {
                double sign=combined[column*9+type]-1;
                int mask=(first[column*9+type]-1)*sign>0.000001?1:0;
                if(second!=null && (second[column*9+type]-1)*sign>0.000001)mask|=2;
                return new OriginEntry { Type=type,Mask=mask };
            }).OrderBy(entry=>entry.Mask==2?1:entry.Mask==3?.5:0).ToArray();
        }
    }
}
