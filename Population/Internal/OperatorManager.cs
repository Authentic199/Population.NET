using Population.Public.Descriptors;
using System.Collections.Immutable;

namespace Population.Internal;

public static class OperatorManager
{
    public static bool IsEqualGroup(CompareOperator compareOperator) => compareOperator is CompareOperator.Equal or CompareOperator.NotEqual;

    public static bool IsNullableGroup(CompareOperator compareOperator) => compareOperator is CompareOperator.Null or CompareOperator.NotNull;

    public static bool IsInGroup(CompareOperator compareOperator) => compareOperator is CompareOperator.In or CompareOperator.NotIn;

    public static bool IsContainGroup(CompareOperator compareOperator) => compareOperator is CompareOperator.Contains or CompareOperator.NotContains;

    public static bool IsComparisonGroup(CompareOperator compareOperator)
        => compareOperator
        is CompareOperator.LessThan
        or CompareOperator.LessThanOrEqual
        or CompareOperator.GreaterThan
        or CompareOperator.GreaterThanOrEqual;

    public static readonly ImmutableDictionary<string, NextLogicalOperator> NextLogicalMapping = ImmutableDictionary.CreateRange(new Dictionary<string, NextLogicalOperator>()
    {
        { "$or", NextLogicalOperator.Or },
        { "$and", NextLogicalOperator.And },
    });

    public static readonly ImmutableDictionary<string, CompareOperator> CompareOperatorMapping = ImmutableDictionary.CreateRange(new Dictionary<string, CompareOperator>()
    {
        { "$in", CompareOperator.In },
        { "$notIn", CompareOperator.NotIn },
        { "$eq", CompareOperator.Equal },
        { "$ne", CompareOperator.NotEqual },
        { "$lt", CompareOperator.LessThan },
        { "$lte", CompareOperator.LessThanOrEqual },
        { "$gt", CompareOperator.GreaterThan },
        { "$gte", CompareOperator.GreaterThanOrEqual },
        { "$contains", CompareOperator.Contains },
        { "$notContains", CompareOperator.NotContains },
        { "$startsWith", CompareOperator.StartsWith },
        { "$notstartsWith", CompareOperator.NotStartsWith },
        { "$endsWith", CompareOperator.EndsWith },
        { "$notendsWith", CompareOperator.NotEndsWith },
        { "$null", CompareOperator.Null },
        { "$notNull", CompareOperator.NotNull },
    });
}
