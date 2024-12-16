using static Populates.Definations.PopulateConstant.SpecialCharacter;
using static Populates.Definations.PopulateOptions;

namespace Populates.Definations;

internal static class PopulateConstant
{
    internal const int BeginIndex = 0;

    internal const string PopulateAlias = "populate";
    internal const string FieldAlias = "fields";
    internal const string IdFields = "Id";
    internal const string OneLevelWildcard = $"1{Asterisk}";
    internal const string HasValue = nameof(HasValue);

    internal static class SpecialCharacter
    {
        internal const char Dot = '.';
        internal const char Comma = ',';
        internal const char Caret = '^';
        internal const char Colons = ':';
        internal const char DollarSign = '$';
        internal const string Asterisk = "*";
        internal const string PoundSign = "#";
    }

    internal static class MethodAlias
    {
        internal const string Max = nameof(Enumerable.Max);
        internal const string Any = nameof(Enumerable.Any);
        internal const string ToStringAlias = nameof(ToString);
        internal const string Contains = nameof(Enumerable.Contains);
        internal const string Select = nameof(Enumerable.Select);
        internal const string SelectMany = nameof(Enumerable.SelectMany);
        internal const string StartsWith = nameof(string.StartsWith);
        internal const string EndsWith = nameof(string.EndsWith);
        internal const string Where = nameof(Enumerable.Where);
        internal const string ToList = nameof(Enumerable.ToList);
        internal const string ToArray = nameof(Enumerable.ToArray);
    }

    internal static bool EqualAsterisk(this string input) => string.Equals(input, Asterisk, IgnoreCaseCompare);

    internal static bool EqualSelect(this string input) => string.Equals(input, MethodAlias.Select, IgnoreCaseCompare);

    internal static bool EqualSelectMany(this string input) => string.Equals(input, MethodAlias.SelectMany, IgnoreCaseCompare);

    internal static string AttachDot(this string prefix) => string.Concat(prefix, Dot);

    internal static string AttachAsterisk(this string prefix) => string.Concat(prefix, Asterisk);
}
