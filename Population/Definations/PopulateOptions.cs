using System.Reflection;
using System.Text.RegularExpressions;

namespace Populates.Definations;

internal static class PopulateOptions
{
    internal const StringComparison IgnoreCaseCompare = StringComparison.OrdinalIgnoreCase;
    internal const StringSplitOptions TrimSplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;
    internal const RegexOptions IgnoreCaseOptions = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
    internal const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
    internal const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
}
