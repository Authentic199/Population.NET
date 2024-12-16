using Population.Extensions;
using Population.Public;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Population.Internal.Projection;

internal sealed class PopulateAnalyzer : IEquatable<PopulateAnalyzer>
{
    /// <summary>
    /// Initializes a <see cref="PopulateAnalyzer"/> instance with the specified populate type and populate keys.
    /// </summary>
    /// <param name="populateType">The type to populate.</param>
    /// <param name="populateKeys">The keys used for populating.</param>
    /// <returns>A new <see cref="PopulateAnalyzer"/> instance.</returns>
    internal static PopulateAnalyzer Init(Type populateType, IEnumerable<string> populateKeys) => new(populateType, populateKeys);

    private readonly Lazy<MetaPropertyBag> lazyBag;

    private PopulateAnalyzer(Type populateType, IEnumerable<string> populateKeys)
    {
        PopulateType = populateType;
        lazyBag = new(() => populateType.GetMetaPropertyBag(
            ignores: [typeof(JsonIgnoreAttribute)],
            populateIgnores: typeof(S3FilePath))
        );
        PopulateKeys = ResolvePopulateKeys(populateKeys);
    }

    internal Type PopulateType { get; }

    internal HashSet<string> PopulateKeys { get; }

    public bool Equals(PopulateAnalyzer? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PopulateType.Equals(other.PopulateType) && PopulateKeys.SequenceEqual(other.PopulateKeys);
    }

    public override bool Equals(object? obj) => obj is PopulateAnalyzer populateAnalyzer && Equals(populateAnalyzer);

    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(PopulateType);
        foreach (string key in PopulateKeys)
        {
            hashCode.Add(key);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Retrieves a collection of properties based on the specified member path and populate keys.
    /// </summary>
    /// <param name="memberPath">The <see cref="MemberPath"/> representing the hierarchical path of the member.</param>
    /// <returns>
    /// A collection of <see cref="PropertyInfo"/> objects that match the populate keys and criteria for the given <paramref name="memberPath"/>.
    /// </returns>
    /// <remarks>
    /// This method filters and combines properties directly accessible and referenced by the specified <paramref name="memberPath"/>. 
    /// It uses metadata stored in the <see cref="MetaPropertyBag"/> to determine the matching properties.
    /// </remarks>
    internal ICollection<PropertyInfo> GetPropertySelection(MemberPath memberPath)
    {
        if (!PopulateKeys.Contains(memberPath.PopulateKey) || !lazyBag.Value.TryGetValue(memberPath, out List<MetaProperty>? metaProperties))
        {
            return [];
        }

        return [.. GetDirectProperties(metaProperties, GetFieldPopulates(memberPath)), .. GetReferenceProperties(metaProperties, memberPath)];
    }

    /// <summary>
    /// Splits a dot-separated input string into parts and appends an asterisk to each intermediate segment.
    /// </summary>
    /// <param name="input">The dot-separated input string to process.</param>
    /// <returns>
    /// An enumerable of strings, where each intermediate segment of the input is appended with an asterisk.
    /// </returns>
    /// <remarks>
    /// This method processes the input string by splitting it at each dot, iteratively building intermediate segments,
    /// and appending an asterisk (*) to each segment. The resulting segments are returned one at a time using <c>yield return</c>.
    /// </remarks>
    private static IEnumerable<string> SplitAsterisk(string input)
    {
        string currentPart = string.Empty;
        string[] parts = input.Split(Dot, TrimSplitOptions);
        for (int i = 0; i < parts.Length - 1; i++)
        {
            currentPart = string.IsNullOrWhiteSpace(currentPart)
                ? parts[i] : string.Join(Dot, currentPart, parts[i]);

            yield return currentPart.AttachAsterisk();
        }
    }

    /// <summary>
    /// Determines whether a string starts with or is equal to a specified value, using a specific comparison option.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="value">The value to compare against.</param>
    /// <param name="comparison">The <see cref="StringComparison"/> option to use for the comparison.</param>
    /// <returns>
    /// <c>true</c> if the <paramref name="input"/> starts with or is equal to <paramref name="value"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method combines the functionality of <see cref="string.StartsWith"/> and <see cref="string.Equals"/> to 
    /// perform a unified check using the specified <paramref name="comparison"/> option.
    /// </remarks>
    private static bool StartWithOrEqual(string input, string value, StringComparison comparison)
        => input.StartsWith(value, comparison) || input.Equals(value, comparison);

    /// <summary>
    /// Retrieves properties that are directly accessible, based on the specified metadata and field populate keys.
    /// </summary>
    /// <param name="metaProperties">A list of <see cref="MetaProperty"/> objects representing metadata for properties.</param>
    /// <param name="fieldPopulates">A set of field populate keys to filter the properties.</param>
    /// <returns>
    /// An enumerable of <see cref="PropertyInfo"/> objects that are not marked as references and match the field populate keys.
    /// </returns>
    /// <remarks>
    /// This method filters non-reference properties from the metadata and ensures that they match the provided field populate keys.
    /// If no field populate keys are specified, all non-reference properties are included.
    /// </remarks>
    private static IEnumerable<PropertyInfo> GetDirectProperties(List<MetaProperty> metaProperties, HashSet<string> fieldPopulates)
        => metaProperties
            .Where(x => !x.IsReferences && (fieldPopulates.Count == 0 || fieldPopulates.Contains(x.PropertyPath)))
            .Select(x => x.Property);

    /// <summary>
    /// Retrieves properties that are references, based on the specified metadata and member path.
    /// </summary>
    /// <param name="metaProperties">A list of <see cref="MetaProperty"/> objects representing metadata for properties.</param>
    /// <param name="memberPath">The <see cref="MemberPath"/> representing the current hierarchical path.</param>
    /// <returns>
    /// An enumerable of <see cref="PropertyInfo"/> objects that are marked as references and match the populate keys.
    /// </returns>
    /// <remarks>
    /// This method filters reference properties from the metadata by verifying their associated populate keys and ensuring their existence 
    /// in the metadata bag.
    /// </remarks>
    private IEnumerable<PropertyInfo> GetReferenceProperties(List<MetaProperty> metaProperties, MemberPath memberPath)
        => from MetaProperty metaProperty in metaProperties.Where(x => x.IsReferences)
           let referencePath = MemberPath.Init(memberPath.Value, metaProperty.Property.Name)
           where PopulateKeys.Contains(referencePath.PopulateKey) && lazyBag.Value.ContainsKey(referencePath)
           select metaProperty.Property;

    /// <summary>
    /// Retrieves a set of field populate keys that match the given member path.
    /// </summary>
    /// <param name="memberPath">The <see cref="MemberPath"/> used to build the regex for matching populate keys.</param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> containing the matching populate keys, compared in a case-insensitive manner.
    /// </returns>
    /// <remarks>
    /// This method uses a regex pattern built from the <paramref name="memberPath"/> to match populate keys 
    /// and constructs a set for efficient comparison.
    /// </remarks>
    private HashSet<string> GetFieldPopulates(MemberPath memberPath)
        => new(
            PopulateKeys.Where(x => Regex.IsMatch(x, BuildDotAlphanumericRegex(memberPath.Value), IgnoreCaseOptions)),
            StringComparer.OrdinalIgnoreCase
        );

    /// <summary>
    /// Resolves a collection of populate keys into a processed set of keys based on specific patterns and rules.
    /// </summary>
    /// <param name="populateKeys">The collection of input populate keys to resolve.</param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> containing the resolved populate keys, with duplicates removed and comparisons made case-insensitive.
    /// </returns>
    /// <remarks>
    /// This method processes each input key by:
    /// <list type="bullet">
    /// <item>Handling pound sign ("#") keys to include all populate keys from the metadata bag.</item>
    /// <item>Handling level-asterisk keys to include keys based on their hierarchical level and path.</item>
    /// <item>Splitting keys with asterisks into individual parts for further processing.</item>
    /// The resulting keys are returned as a case-insensitive set.
    /// </list>
    /// </remarks>
    private HashSet<string> ResolvePopulateKeys(IEnumerable<string> populateKeys)
    {
        List<string> resolved = [];
        foreach (string populateKey in populateKeys)
        {
            if (TryHandlePoundSignKey(populateKey, resolved))
            {
                break;
            }

            resolved.Add(Asterisk);

            if (TryHandleLevelAsteriskKey(populateKey, resolved))
            {
                continue;
            }

            resolved.AddRange(SplitAsterisk(populateKey));
            resolved.Add(populateKey);
        }

        return new HashSet<string>(resolved, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to handle a populate key that represents a pound sign ("#"), which indicates inclusion of all keys.
    /// </summary>
    /// <param name="populateKey">The populate key to check and handle.</param>
    /// <param name="resolved">The list to which resolved keys should be added.</param>
    /// <returns>
    /// <c>true</c> if the key is a pound sign and was successfully handled; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If the key matches the pound sign, this method adds all populate keys from the metadata bag to the 
    /// <paramref name="resolved"/> list and returns <c>true</c>. If the key does not match, no action is taken, and <c>false</c> is returned.
    /// </remarks>
    private bool TryHandlePoundSignKey(string populateKey, List<string> resolved)
    {
        if (!populateKey.Equals(PoundSign, IgnoreCaseCompare))
        {
            return false;
        }

        resolved.AddRange(lazyBag.Value.Select(x => x.Key.PopulateKey));
        return true;
    }

    /// <summary>
    /// Attempts to handle a populate key that matches a level-asterisk pattern, resolving keys based on their level and path.
    /// </summary>
    /// <param name="populateKey">The populate key to check and handle.</param>
    /// <param name="resolved">The list to which resolved keys should be added.</param>
    /// <returns>
    /// <c>true</c> if the key matches a level-asterisk pattern and was successfully handled; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks if the key matches the level-asterisk pattern (e.g., "*2"), which specifies a hierarchical level.
    /// If matched:
    /// <list type="bullet">
    /// <item>If the path is empty, all keys up to the specified level are added to <paramref name="resolved"/>.</item>
    /// <item>If the path is non-empty, keys matching the path and within the level constraint are added to <paramref name="resolved"/>.</item>
    /// </list>
    /// </remarks>
    private bool TryHandleLevelAsteriskKey(string populateKey, List<string> resolved)
    {
        if (!LevelAsterisk.IsMatch(populateKey))
        {
            return false;
        }

        MatchCollection matches = LevelAsterisk.Matches(populateKey);
        if (matches.Count != 1)
        {
            return false;
        }

        MetaPropertyBag metaPropertyBag = lazyBag.Value;
        int level = int.Parse(matches[0].Value.Trim(Asterisk[0]));
        string path = LevelAsterisk.Replace(populateKey, string.Empty);

        if (string.IsNullOrEmpty(path))
        {
            resolved.AddRange(metaPropertyBag.Where(x => x.Key.Level <= level).Select(x => x.Key.PopulateKey));
            return true;
        }

        MemberPath memberPath = metaPropertyBag.FirstOrDefault(x => x.Key.Value.Equals(path, IgnoreCaseCompare)).Key;
        resolved.AddRange(metaPropertyBag
            .Where(x => StartWithOrEqual(x.Key.Value, path, IgnoreCaseCompare) && x.Key.Level <= memberPath.Level + level)
            .Select(x => x.Key.PopulateKey));

        return true;
    }
}
