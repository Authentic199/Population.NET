using Population.Extensions;
using Population.Public.Descriptors;
using System.Text.RegularExpressions;
using static Population.Definations.PopulateConstant;
using static Population.Definations.PopulateOptions;
using static Population.Extensions.RegexExtension;
using ParamsBag = System.Collections.Generic.IDictionary<string, string>;
using ParamsPair = System.Collections.Generic.KeyValuePair<string, string>;

namespace Population.Internal;

public class FilterRequest
{
    private readonly List<string> exceptKey = new();
    private readonly ParamsBag filterBag;
    private string filterKey = string.Empty;
    private string filterValue = string.Empty;

    public FilterRequest(ParamsBag filterBag)
    {
        this.filterBag = filterBag;
    }

    /// <summary>
    /// Binds the filter key-value pair to a <see cref="FilterDescriptor"/> object.
    /// </summary>
    /// <param name="paramsPair">The key-value pair representing the filter.</param>
    /// <returns>
    /// Returns a <see cref="FilterDescriptor"/> object if the binding is successful; otherwise, null.
    /// </returns>
    /// <remarks>
    /// This method binds the filter key-value pair to a <see cref="FilterDescriptor"/> object.
    /// It sets the filter key and value from the provided params pair and attempts to construct
    /// a <see cref="FilterDescriptor"/> using the <see cref="TrySetFilterValue"/> method. If the filter key
    /// is in the exceptKey collection or if the construction of the <see cref="FilterDescriptor"/> fails,
    /// the method returns null. Otherwise, it returns the constructed <see cref="FilterDescriptor"/> object.
    /// </remarks>
    public object? Bind(ParamsPair paramsPair)
    {
        filterKey = paramsPair.Key;
        filterValue = paramsPair.Value;
        if (exceptKey.Contains(filterKey) || !TrySetFilterValue(out FilterDescriptor? filterDescriptor))
        {
            return default;
        }

        return filterDescriptor;
    }

    /// <summary>
    /// Tries to set the filter value based on the information extracted from the filter key.
    /// </summary>
    /// <param name="filterDescriptor">The filter descriptor object representing the constructed filter, if successful.</param>
    /// <returns>
    /// Returns true if the filter value is successfully set; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method attempts to set the filter value based on the information extracted from the filter key.
    /// It first tries to retrieve the <see cref="CompareOperator"/> and the <see cref="NextLogicalOperator"/> from the filter key.
    /// If either operation fails, indicating that the filter key does not conform to the expected format,
    /// the method returns false. Otherwise, it constructs a <see cref="FilterDescriptor"/> object using the raw property,
    /// the filter value (or the "IN" operator value if applicable), the <see cref="NextLogicalOperator"/>, the <see cref="CompareOperator"/>,
    /// and the string representation of the <see cref="NextLogicalOperator"/>. If successful, the method assigns the constructed
    /// <see cref="FilterDescriptor"/> to <paramref name="filterDescriptor"/> and returns true.
    /// </remarks>
    private bool TrySetFilterValue(out FilterDescriptor? filterDescriptor)
    {
        NextLogicalOperator nextLogicalOperator = NextLogicalOperator.And;

        if (!TryGetCompareOperator(out CompareOperator compareOperator) ||
            !TryGetIfHasNextLogicalOperator(ref nextLogicalOperator))
        {
            filterDescriptor = null;
            return false;
        }

        filterDescriptor = new(
            GetRawProperty().ReplaceBracketToDot(),
            OperatorManager.IsInGroup(compareOperator) ? SetInOperatorValue() : filterValue,
            nextLogicalOperator,
            compareOperator,
            nextLogicalOperator.ToString());
        return true;
    }

    /// <summary>
    /// Tries to retrieve the <see cref="NextLogicalOperator"/> from the filter key.
    /// </summary>
    /// <param name="nextLogicalOperator">The enum representation of the <see cref="NextLogicalOperator"/>, if found.</param>
    /// <returns>
    /// Returns true if the <see cref="NextLogicalOperator"/> is successfully retrieved or if no <see cref="NextLogicalOperator"/> is present; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method attempts to retrieve the <see cref="NextLogicalOperator"/> from the filter key.
    /// If the filter key does not contain any <see cref="NextLogicalOperator"/> pattern, the method returns true.
    /// Otherwise, it tries to extract a single <see cref="NextLogicalOperator"/> string, ensuring it matches the next logical index property pattern.
    /// If successful, it attempts to map the extracted <see cref="NextLogicalOperator"/> string to an enum representation.
    /// If all conditions are met, the method assigns the mapped <see cref="NextLogicalOperator"/> to <paramref name="nextLogicalOperator"/> and returns true.
    /// Otherwise, it returns false.
    /// </remarks>
    private bool TryGetIfHasNextLogicalOperator(ref NextLogicalOperator nextLogicalOperator)
    {
        if (!NextLogicalOperatorPatternRegex.IsMatch(filterKey))
        {
            return true;
        }

        return TryGetSingleNextLogical(out string nextLogicalString) &&
            NextLogicalIndexPropertyPatternRegex.IsMatch(filterKey) &&
            nextLogicalString.TryExtractBracketedValue(out string nextLogicalOperatorString) &&
            OperatorManager.NextLogicalMapping.TryGetValue(nextLogicalOperatorString, out nextLogicalOperator);
    }

    /// <summary>
    /// Retrieves the raw property from the filter key.
    /// </summary>
    /// <returns>
    /// Returns the raw property extracted from the filter key.
    /// </returns>
    /// <remarks>
    /// This method retrieves the raw property from the filter key by using a regular expression pattern
    /// to match the special bracketed pattern in the key. It then removes any occurrences of the "IN" operator
    /// pattern, next logical operator pattern, and general operator pattern from the matched value.
    /// The resulting string represents the raw property extracted from the filter key.
    /// </remarks>
    private string GetRawProperty()
        => SpecialBracketedPatternRegex.Match(filterKey).Value
            .RegexReplace(InOperatorIndexPattern, string.Empty)
            .RegexReplace(NextLogicalIndexPattern, string.Empty)
            .RegexReplace(OperatorPattern, string.Empty);

    /// <summary>
    /// Tries to retrieve the <see cref="CompareOperator"/> from the filter key.
    /// </summary>
    /// <param name="compareOperator">The enum representation of the <see cref="CompareOperator"/>, if found.</param>
    /// <returns>
    /// Returns true if the <see cref="CompareOperator"/> is successfully retrieved; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method attempts to retrieve the <see cref="CompareOperator"/> from the filter key.
    /// It first checks if the filter key contains a single operator. If not, or if the operator string
    /// cannot be extracted from bracketed values, or if the extracted operator string cannot be mapped to
    /// a <see cref="CompareOperator"/> enum, or if the mapped <see cref="CompareOperator"/> is an "IN" operator and the filter key
    /// does not match the "IN" operator pattern, the method returns false.
    /// Otherwise, it assigns the mapped <see cref="CompareOperator"/> to <paramref name="compareOperator"/> and returns true.
    /// </remarks>
    private bool TryGetCompareOperator(out CompareOperator compareOperator)
    {
        if (!HasSingleOperator(out string operatorString) ||
            !operatorString.TryExtractBracketedValue(out string compareOperatorString) ||
            !OperatorManager.CompareOperatorMapping.TryGetValue(compareOperatorString, out CompareOperator compareOperatorEnum) ||
            (OperatorManager.IsInGroup(compareOperatorEnum) && !InOperatorIndexPatternRegex.Match(filterKey).Success))
        {
            compareOperator = default;
            return false;
        }

        compareOperator = compareOperatorEnum;
        return true;
    }

    /// <summary>
    /// Checks if the filter key contains a single operator.
    /// </summary>
    /// <param name="operatorString">The string representation of the operator, if found.</param>
    /// <returns>
    /// Returns true if the filter key contains a single operator; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method checks if the filter key contains a single operator by using a regular expression pattern
    /// to match operators. If exactly one match is found, the method assigns the matched value to
    /// <paramref name="operatorString"/> and returns true. Otherwise, it sets <paramref name="operatorString"/>
    /// to an empty string and returns false.
    /// </remarks>
    private bool HasSingleOperator(out string operatorString)
    {
        MatchCollection operators = OperatorPatternRegex.Matches(filterKey);
        if (operators.Count != 1)
        {
            operatorString = string.Empty;
            return false;
        }

        operatorString = operators[0].Value;
        return true;
    }

    /// <summary>
    /// Tries to retrieve a single <see cref="NextLogicalOperator"/> from the filter key.
    /// </summary>
    /// <param name="nextLogicalString">The string representation of the <see cref="NextLogicalOperator"/>, if found.</param>
    /// <returns>
    /// Returns true if a single <see cref="NextLogicalOperator"/> is found; otherwise, false.
    /// </returns>
    /// <remarks>
    /// This method attempts to retrieve a single <see cref="NextLogicalOperator"/> from the filter key.
    /// It uses a regular expression pattern to match <see cref="NextLogicalOperator"/> in the filter key.
    /// If exactly one match is found, the method assigns the matched value to <paramref name="nextLogicalString"/>
    /// and returns true. Otherwise, it sets <paramref name="nextLogicalString"/> to an empty string and returns false.
    /// </remarks>
    private bool TryGetSingleNextLogical(out string nextLogicalString)
    {
        MatchCollection nextLogicals = NextLogicalOperatorPatternRegex.Matches(filterKey);

        if (nextLogicals.Count != 1)
        {
            nextLogicalString = string.Empty;
            return false;
        }

        nextLogicalString = nextLogicals[0].Value;
        return true;
    }

    /// <summary>
    /// Sets the value for the "IN" operator.
    /// </summary>
    /// <returns>
    /// Returns the value for the "IN" operator.
    /// </returns>
    /// <remarks>
    /// This method sets the value for the "IN" operator used in filter operations. It extracts the property name from the filter key
    /// and retrieves a group of parameters matching the "IN" operator pattern. The keys from this group are added to the exceptKey collection,
    /// and the corresponding values are concatenated to form the final value for the "IN" operator. The method also defines a helper function,
    /// GroupInMatch, to check if a given input matches the "IN" operator pattern and contains the extracted property name.
    /// </remarks>
    private string SetInOperatorValue()
    {
        string propertyInOperator = filterKey.RegexReplace(EndIndexPattern, string.Empty);
        ParamsBag groupInOperator = filterBag.Where(x => GroupInMatch(x.Key)).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        exceptKey.AddRange(groupInOperator.Keys);
        return string.Join(SpecialCharacter.Comma, groupInOperator.Values);

        bool GroupInMatch(string input)
            => InOperatorIndexPatternRegex.IsMatch(input) &&
            input.Contains(propertyInOperator, IgnoreCaseCompare);
    }
}
