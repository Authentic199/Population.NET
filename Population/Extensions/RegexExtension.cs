using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using static Infrastructure.Facades.Populates.Definations.PopulateConstant;
using static Infrastructure.Facades.Populates.Definations.PopulateConstant.SpecialCharacter;
using static Infrastructure.Facades.Populates.Definations.PopulateOptions;

namespace Infrastructure.Facades.Populates.Extensions;

internal static partial class RegexExtension
{
    internal const string NestedPopulateReplacePattern = ".$2$3"; // Example: root[populate][nested] -> root.nested
    internal const string TwoCaptureReplacePattern = "$2"; // Example: a[1] -> 1, populate[code] -> code

    internal static readonly string LastNestedFieldPattern = LastNestedFieldRegex().ToString(); // Example Matches: [fields]$, [fields][12]$
    internal static readonly string StartFieldOrderPattern = StartFieldOrderRg().ToString(); // Example Matches: ^fields[0], ^fields
    internal static readonly string LastPopulateOrderPattern = LastPopulateOrderRg().ToString(); // Example Matches: [populate][0]$, [populate]$ -> $ is last of string
    internal static readonly string StartPopulateOrderPattern = StartPopulateOrderRg().ToString(); // Example Matches: ^populate[0]$, ^populate[123]$ -> '^' is Start of string, '$' is last of string
    internal static readonly string NestedPopulatePattern = NestedPopulateRegex().ToString(); // Example Matches: [populate][Province]
    internal static readonly string StartPopulateBracketPattern = StartPopulateBracketRegex().ToString();  // Example Matches: populate[abc]
    internal static readonly string PopulateBracketPattern = PopulateBracketRegex().ToString(); // Example Matches: populate[abc][populate][1234]
    internal static readonly string BracketedPattern = BracketedRegex().ToString(); // Example: Matches [123], [root], [$xyz]
    internal static readonly string EndIndexPattern = EndIndexPatternRg().ToString(); // Example Matches: [1234]
    internal static readonly string OperatorPattern = OperatorPatternRg().ToString(); // Example Matches: [$example]
    internal static readonly string InOperatorIndexPattern = InOperatorIndexPatternRg().ToString(); // Example Matches: [$in][1234]
    internal static readonly string NextLogicalIndexPattern = NextLogicalIndexPatternRegex().ToString(); // Example Matches: [$or][1234]
    internal static readonly string UniqueIndexPattern = $"{Caret}{{{BeginIndex}}}{EndIndexPatternRg()}"; // Example Matches: Sorts[1234]
    internal static readonly string SpecialCharacterPattern = SpecialCharacterRg().ToString(); // Example Matches: Sorts[1234]

    internal static readonly Regex OperatorPatternRegex = OperatorPatternRg(); // Example: Matches [$eq], [$in], [$contains]
    internal static readonly Regex SpecialBracketedPatternRegex = SpecialBracketedPatternRg(); // Example: Matches [$or][0][another][$eq]
    internal static readonly Regex InOperatorIndexPatternRegex = InOperatorIndexPatternRg(); // Example: Matches [$in][1234]
    internal static readonly Regex NextLogicalOperatorPatternRegex = NextLogicalOperatorPatternRg();  // Example: Matches [$or] or [$and]
    internal static readonly Regex NextLogicalIndexPropertyPatternRegex = NextLogicalIndexPropertyPatternRg(); // Example: Matches [$or][1234][propertyName]
    internal static readonly Regex StartFieldOrderRegex = StartFieldOrderRg(); // Example Matches: fields[0], fields
    internal static readonly Regex LastPopulateOrderRegex = LastPopulateOrderRg();  // Example Matches: [populate][0]$, [populate]$ -> $ is last of string
    internal static readonly Regex SpecialCharacterRegex = SpecialCharacterRg();  // Example Matches: [populate][0]$, [populate]$ -> $ is last of string
    internal static readonly Regex LevelAsterisk = LevelAsteriskRg();

    private const string ReplaceBracketWithDotPattern = ".$1"; // Example: [1][2] -> 1.2, [code][name] -> code.name

    internal static bool IsSingleMatch(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string regex)
        => Regex.Matches(input, regex, IgnoreCaseOptions).Count == 1;

    internal static string RegexReplace(this string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement, RegexOptions? options = default)
        => Regex.Replace(input, pattern, replacement, options ?? IgnoreCaseOptions);

    internal static string ReplaceBracketToDot(this string input)
        => BracketedRegex().Replace(input, ReplaceBracketWithDotPattern).Trim(Dot);

    internal static string BuildPropertyRegex(string objectName, string propertyName)
        => new StringBuilder()
        .Append(Caret)
        .Append(objectName)
        .Append("\\[")
        .Append(propertyName)
        .Append("\\]")
        .Append(DollarSign)
        .ToString();

    internal static string BuildDotAlphanumericRegex(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return AlphanumericRg().ToString();
        }

        return new StringBuilder()
        .Append(Caret)
        .Append(path)
        .Append(DotAlphanumericRegex())
        .ToString();
    }

    internal static bool TryExtractBracketedValue(this string bracketedPattern, out string value)
    {
        if (!BracketedRegex().IsMatch(bracketedPattern))
        {
            value = string.Empty;
            return false;
        }

        value = bracketedPattern.Trim('[', ']');
        return true;
    }

    [GeneratedRegex("(\\[(\\d+)\\])$", IgnoreCaseOptions)]
    private static partial Regex EndIndexPatternRg();

    [GeneratedRegex("(\\[(\\$)(?!or|and)[A-Za-z]+\\])", IgnoreCaseOptions)]
    private static partial Regex OperatorPatternRg();

    [GeneratedRegex("(\\[(\\$)?[A-Za-z0-9]+\\])+$", IgnoreCaseOptions)]
    private static partial Regex SpecialBracketedPatternRg();

    [GeneratedRegex("(\\[\\$(not)?in\\])(\\[(\\d+)\\])$", IgnoreCaseOptions)]
    private static partial Regex InOperatorIndexPatternRg();

    [GeneratedRegex("(\\[\\$\\b(or|and)\\b\\])", IgnoreCaseOptions)]
    private static partial Regex NextLogicalOperatorPatternRg();

    [GeneratedRegex("(\\[\\$\\b(or|and)\\b\\])(\\[(\\d+)\\])(\\[([A-Za-z])+\\])", IgnoreCaseOptions)]
    private static partial Regex NextLogicalIndexPropertyPatternRg();

    [GeneratedRegex("^fields(\\[[\\d]+\\])*$", IgnoreCaseOptions)]
    private static partial Regex StartFieldOrderRg();

    [GeneratedRegex("^(populate(\\[(\\d+)\\]){1})$", IgnoreCaseOptions)]
    private static partial Regex StartPopulateOrderRg();

    [GeneratedRegex("(\\[populate\\](\\[(\\d+)\\]){0,1})$", IgnoreCaseOptions)]
    private static partial Regex LastPopulateOrderRg();

    [GeneratedRegex("\\[populate\\](\\[([A-Za-z])([A-Za-z0-9]+)\\])", IgnoreCaseOptions)]
    private static partial Regex NestedPopulateRegex();

    [GeneratedRegex("^populate(\\[([A-Za-z0-9]+)\\])")]
    private static partial Regex StartPopulateBracketRegex();

    [GeneratedRegex("\\[([^\\]]+)\\]", IgnoreCaseOptions)]
    private static partial Regex BracketedRegex();

    [GeneratedRegex("(\\[\\$\\b(or|and)\\b\\])(\\[(\\d+)\\])", IgnoreCaseOptions)]
    private static partial Regex NextLogicalIndexPatternRegex();

    [GeneratedRegex("(\\[fields\\](\\[\\d+\\]){0,1})$")]
    private static partial Regex LastNestedFieldRegex();

    [GeneratedRegex("^populate(\\[([^\\]]+)\\])*$")]
    private static partial Regex PopulateBracketRegex();

    [GeneratedRegex("[\\.]([A-Za-z0-9])+$")]
    private static partial Regex DotAlphanumericRegex();

    [GeneratedRegex("^([A-Za-z0-9])+$")]
    private static partial Regex AlphanumericRg();

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex SpecialCharacterRg();

    [GeneratedRegex("[0-9]\\*$")]
    private static partial Regex LevelAsteriskRg();
}
