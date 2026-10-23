using System;

namespace AniimoAffinities
{
    internal static class OriginGroups
    {
        // Inputs: selected types, direction and displayed targets. Output: unique targets ordered by contributing origins, as ElementOriginGroups in SaveOverride. Fallback: empty targets. Raises: invalid model indices.
        internal static OriginEntry[] Build(int main, int sub, int column, int[] targets)
        {
            var combined = Model.Factors(main, sub);
            var first = Model.Factors(main, -1);
            var second = sub < 0 ? null : Model.Factors(sub, -1);
            var result = new OriginEntry[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                int type = targets[i];
                double sign = combined[column * 9 + type] - 1;
                int mask = (first[column * 9 + type] - 1) * sign > 0.000001 ? 1 : 0;
                if (second != null && (second[column * 9 + type] - 1) * sign > 0.000001)
                    mask |= 2;
                var entry = new OriginEntry { Type = type, Mask = mask };
                // At most nine displayed types: insert without allocating a
                // sorting pipeline, retaining input order within each group.
                int position = i;
                while (position > 0 && Priority(result[position - 1].Mask) > Priority(mask))
                {
                    result[position] = result[position - 1];
                    position--;
                }
                result[position] = entry;
            }
            return result;
        }
        private static int Priority(int mask)
        {
            return mask == 2 ? 2 : mask == 3 ? 1 : 0;
        }
    }
}
