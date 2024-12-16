using AutoMapper;
using Infrastructure.Facades.Common.Extensions;
using Infrastructure.Facades.Populates.Definations;
using Infrastructure.Facades.Populates.Exceptions;
using Infrastructure.Facades.Populates.Internal.Queries;
using Infrastructure.Facades.Populates.Public.Descriptors;
using Microsoft.AspNetCore.Mvc;
using Population.Public.Descriptors;

namespace Infrastructure.Facades.Populates;

[ModelBinder(typeof(QueryBinder))]
public class QueryContext
{
    public PagingDescriptor Pagination { get; set; } = new();

    public List<SortDescriptor>? Sort { get; set; }

    public List<FilterDescriptor>? Filters { get; set; }

    public SearchDescriptor? Search { get; set; }

    public PopulateDescriptor Populate { get; set; } = new();
}

public class QueryContextMapping : Profile
{
    public QueryContextMapping()
    {
        CreateMap<QueryContainer, QueryContext>()
            .IncludeAllDerived()
            .AfterMap((src, des) =>
            {
                des ??= new();
                des.Pagination = new()
                {
                    Page = src.Current,
                    PageSize = src.PageSize,
                };

                des.Search = new()
                {
                    Fields = src.SearchFields?.ToList(),
                    Keyword = src.SearchKeyword,
                };

                des.Sort = src.SortQuery?.Split(',', PopulateOptions.TrimSplitOptions).Select(ToSortDescriptor).ToList();
                des.Filters = src.Filter?.SelectMany(ToFilterDescriptors).ToList();
                des.Populate = new(src.PopulateKeys ?? [PopulateConstant.SpecialCharacter.PoundSign]);
            });
    }

    private static SortDescriptor ToSortDescriptor(string sortQuery)
    {
        string[] parts = sortQuery.Split(' ', PopulateOptions.TrimSplitOptions);
        return new SortDescriptor(parts[0], parts.Length <= 1 || parts[1].StartsWith("asc", StringComparison.OrdinalIgnoreCase) ? SortOrder.Asc : SortOrder.Desc);
    }

    private static IEnumerable<FilterDescriptor> ToFilterDescriptors(KeyValuePair<string, List<string>?> filterPair)
    {
        string key = filterPair.Key;
        if (filterPair.Value != null)
        {
            foreach (string value in filterPair.Value)
            {
                List<string> result = [.. value.Split(":")];
                ResolveNotOperator(result, out bool isFilterPrefixNot);

                if (!QueryExpressionExtension.FilterOperators.ContainsKey(result[0]))
                {
                    continue;
                }

                if (result.Count == 1)
                {
                    if (ParseNullOperator(result, key, isFilterPrefixNot) is FilterDescriptor nullFilter)
                    {
                        yield return nullFilter;
                    }

                    continue;
                }

                foreach (FilterDescriptor filterOperator in ParseOperator([result[0], string.Join(":", result.Skip(1))], key, isFilterPrefixNot))
                {
                    yield return filterOperator;
                }
            }
        }
        else
        {
            yield return new("_", bool.TrueString, NextLogicalOperator.And, CompareOperator.Equal, nameof(QueryContainer));
        }
    }

    private static void ResolveNotOperator(List<string> result, out bool isFilterPrefixNot)
    {
        if (result[0].Equals(FilterPrefix.Not, StringComparison.OrdinalIgnoreCase))
        {
            result.RemoveAt(0);
            isFilterPrefixNot = true;
            return;
        }

        isFilterPrefixNot = false;
    }

    private static FilterDescriptor? ParseNullOperator(List<string> result, string key, bool isFilterPrefixNot)
    {
        if (result[0].Equals(FilterOperator.Null, StringComparison.OrdinalIgnoreCase))
        {
            return new FilterDescriptor(key, string.Empty, NextLogicalOperator.And, isFilterPrefixNot ? CompareOperator.NotNull : CompareOperator.Null, nameof(QueryContainer));
        }

        return default;
    }

    private static List<FilterDescriptor> ParseOperator(List<string> result, string key, bool isFilterPrefixNot)
    {
        string filterOperator = result[0].ToLower();
        const string groupName = nameof(QueryContainer);

        if (string.Equals(filterOperator, FilterOperator.Btw, StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = result[1].Split(PopulateConstant.SpecialCharacter.Comma, PopulateOptions.TrimSplitOptions);
            if (parts.Length != 2)
            {
                throw new PopulateNotHandleException("Invalid between filter value");
            }

            return isFilterPrefixNot
                ?
                    [
                        new(key, parts[0], NextLogicalOperator.Or, CompareOperator.LessThan, FilterOperator.Btw + FilterPrefix.Not),
                        new(key, parts[1], NextLogicalOperator.Or, CompareOperator.GreaterThan, FilterOperator.Btw + FilterPrefix.Not),
                    ]
                :
                    [
                        new(key, parts[0], NextLogicalOperator.And, CompareOperator.GreaterThanOrEqual, FilterOperator.Btw),
                        new(key, parts[1], NextLogicalOperator.And, CompareOperator.LessThanOrEqual, FilterOperator.Btw),
                    ];
        }

        return filterOperator switch
        {
            FilterOperator.Sw => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.NotStartsWith, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.StartsWith, groupName)],

            FilterOperator.Eq => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.NotEqual, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.Equal, groupName)],

            FilterOperator.Gt => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.LessThanOrEqual, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.GreaterThan, groupName)],

            FilterOperator.Gte => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.LessThan, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.GreaterThanOrEqual, groupName)],

            FilterOperator.Lt => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.GreaterThanOrEqual, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.LessThan, groupName)],

            FilterOperator.Lte => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.GreaterThan, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.LessThanOrEqual, groupName)],

            FilterOperator.In => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.NotIn, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.In, groupName)],

            FilterOperator.Ilike => isFilterPrefixNot
                            ? [new(key, result[1], NextLogicalOperator.And, CompareOperator.NotContains, groupName)]
                            : [new(key, result[1], NextLogicalOperator.And, CompareOperator.Contains, groupName)],

            _ => throw new NotSupportedException($"{nameof(ParseOperator)} not supported '{filterOperator}' operator"),
        };
    }
}
