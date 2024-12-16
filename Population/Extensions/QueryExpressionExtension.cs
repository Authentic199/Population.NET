using System.Collections.Immutable;

namespace Infrastructure.Facades.Common.Extensions;

public static class QueryExpressionExtension
{
    public static readonly ImmutableDictionary<string, string> FilterOperators = ImmutableDictionary.CreateRange(
            new Dictionary<string, string>()
            {
                { FilterOperator.Eq, " ([PropName] == [Value])" },
                { FilterOperator.Null, " == null " },
                { FilterOperator.In, "([PropName] == [Value]) or " },
                { FilterOperator.Gt, " ([PropName] > [Value]) " },
                { FilterOperator.Lt, " ([PropName] < [Value]) " },
                { FilterOperator.Lte, " ([PropName] <= [Value]) " },
                { FilterOperator.Gte, " ([PropName] >= [Value]) " },
                { FilterOperator.Btw, " ( [PropName]  >= [First] and [PropName] <= [Last] ) " },
                { FilterOperator.Ilike, " ([PropName].Contains([Value])) " },
                { FilterOperator.Sw, " ([PropName].StartsWith([Value])) " },
            }
        );
}

public static class FilterOperator
{
    public const string Eq = "$eq";
    public const string Null = "$null";
    public const string In = "$in";
    public const string Gt = "$gt";
    public const string Lt = "$lt";
    public const string Lte = "$lte";
    public const string Gte = "$gte";
    public const string Btw = "$btw";
    public const string Ilike = "$ilike";
    public const string Sw = "$sw";
}

public static class FilterPrefix
{
    public const string Not = "$not";
}

public class QueryFilterResult
{
    public string Query { get; set; } = string.Empty;

    public List<object> Params { get; set; } = [];

    public bool IsValid { get; set; } = false;
}