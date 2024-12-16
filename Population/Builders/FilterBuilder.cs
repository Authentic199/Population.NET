using Populates.Exceptions;
using Populates.Extensions;
using Populates.Public;
using Populates.Public.Descriptors;
using Populates.Visitors;
using System.Linq.Expressions;

namespace Populates.Builders;

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