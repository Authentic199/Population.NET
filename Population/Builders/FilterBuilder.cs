using Infrastructure.Facades.Populates.Exceptions;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Public;
using Infrastructure.Facades.Populates.Public.Descriptors;
using Infrastructure.Facades.Populates.Visitors;
using Nest;
using Population.Extensions;
using System.Linq.Expressions;
using static Infrastructure.Facades.Populates.Builders.ElasticSearchQueryBuilder;
using FilterPair = (Nest.QueryContainer Query, Infrastructure.Facades.Populates.Public.Descriptors.NextLogicalOperator NextOperator);

namespace Infrastructure.Facades.Populates.Builders;

internal static class FilterBuilder
{
    internal static LambdaExpression? CreateFilterExpression(this IMetaPathBag projections, IEnumerable<FilterDescriptor> filters, ParameterExpression rootParamater, bool validateValueType = true)
    {
        if (projections.Count == 0)
        {
            return null;
        }

        List<(Expression Expression, NextLogicalOperator LogicalOperator)> filterExpressions = [];

        foreach (FilterDescriptor filter in filters)
        {
            if (
                !projections.TryGetValue(MemberPath.InitEmptyRoot(filter.Path), out PathInfo? pathInfo)
                || (validateValueType && !TypeExtension.IsValidValuesForType(filter.Value, pathInfo.DestinationInfo.ResolveMemberType()))
                || !TypeExtension.IsValidCompareOperatorForType(filter.CompareOperator, pathInfo.DestinationInfo.ResolveMemberType())
            )
            {
                continue;
            }

            Expression? comparison;
            try
            {
                comparison = new CompareOperatorVisitor(filter.CompareOperator, filter.Value).Visit(pathInfo.PathMap);
            }
            catch (QueryBuilderException ex)
            {
                Console.WriteLine(ex);
                comparison = null;
            }

            if (comparison != null)
            {
                filterExpressions.Add((comparison, filter.LogicalOperator));
            }
        }

        Expression combinedFilter = CombineFilterExpressions(filterExpressions);
        return Expression.Lambda(combinedFilter, rootParamater);
    }

    internal static QueryContainer BuildFilterQuery<TInferDocument>(this ICollection<FilterDescriptor>? filters)
        where TInferDocument : class
        => filters.IsNullOrEmpty() ? new() : FilterComponent<TInferDocument>(filters!).CombineFilterQueries();

    internal static QueryContainer CombineFilterQueries(this IEnumerable<FilterPair> filterQueries)
    {
        QueryContainer? orQuery = default;
        List<FilterPair> orFilterPairs = [.. filterQueries.Where(x => x.NextOperator is NextLogicalOperator.Or)];
        if (orFilterPairs.Count > 0)
        {
            orQuery = orFilterPairs[0].Query;

            for (int i = 1; i < orFilterPairs.Count; i++)
            {
                orQuery |= orFilterPairs[i].Query;
            }
        }

        orQuery ??= new();
        foreach (FilterPair and in filterQueries.Where(x => x.NextOperator is NextLogicalOperator.And))
        {
            orQuery &= and.Query;
        }

        return orQuery;
    }

    private static IEnumerable<FilterPair> FilterComponent<TInferDocument>(ICollection<FilterDescriptor> filters)
        where TInferDocument : class
    {
        foreach (FilterDescriptor filter in filters!)
        {
            if (typeof(TInferDocument).GetPathInfoRecursive(filter.Path) is not PathInfo pathInfo ||
                CreateFilterQuery<TInferDocument>(pathInfo, filter.Value, filter.CompareOperator) is not QueryContainer nestQuery)
            {
                continue;
            }

            yield return (nestQuery, filter.LogicalOperator);
        }
    }

    private static QueryContainer? CreateFilterQuery<TInferDocument>(PathInfo memberPathInfo, string value, CompareOperator compareOperator)
        where TInferDocument : class
        => compareOperator switch
        {
            CompareOperator.In or CompareOperator.NotIn
                => ResolveQuery(memberPathInfo, value, compareOperator, InGroupQuery<TInferDocument>),

            CompareOperator.Equal or CompareOperator.NotEqual
                => ResolveQuery(memberPathInfo, value, compareOperator, EqualGroupQuery<TInferDocument>),

            CompareOperator.Null or CompareOperator.NotNull
                => ResolveQuery(memberPathInfo, value, compareOperator, NullableGroupQuery<TInferDocument>),

            CompareOperator.Contains or CompareOperator.NotContains
                => ResolveQuery(memberPathInfo, value, compareOperator, ContainGroupQuery<TInferDocument>),

            CompareOperator.LessThan or CompareOperator.LessThanOrEqual or
            CompareOperator.GreaterThan or CompareOperator.GreaterThanOrEqual
                => ResolveQuery(memberPathInfo, value, compareOperator, CompareGroupQuery<TInferDocument>),

            _ => default,
        };

    private static Expression CombineFilterExpressions(List<(Expression Expression, NextLogicalOperator LogicalOperator)> filterExpressions)
    {
        if (filterExpressions.Count == 0)
        {
            return Expression.Constant(true); // No filters, so return a constant true expression
        }

        Expression combinedFilter = filterExpressions[0].Expression;
        int loop = filterExpressions.Count;
        for (int i = 1; i < loop; i++)
        {
            switch (filterExpressions[i].LogicalOperator)
            {
                case NextLogicalOperator.And:
                    combinedFilter = Expression.AndAlso(combinedFilter, filterExpressions[i].Expression);
                    break;

                case NextLogicalOperator.Or:
                    combinedFilter = Expression.OrElse(combinedFilter, filterExpressions[i].Expression);
                    break;

                case NextLogicalOperator.None:
                    break;
            }
        }

        return combinedFilter;
    }
}