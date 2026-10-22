using System.Linq;

namespace AniimoAffinities
{
    internal static class AccessibleMatchups
    {
        internal static string Describe(Selection selection)
        {
            if (selection.Main < 0)
                return "Select one or two types to compare attack and defense.";
            var factors = Model.Factors(selection.Main, selection.Sub);
            return string.Join("; ", Enumerable.Range(0, Model.Names.Length).Select(i => Model.Names[i] + ": attack " + Model.Factor(factors[i]) + ", damage received " + Model.Factor(factors[9 + i])).ToArray());
        }
    }
}
