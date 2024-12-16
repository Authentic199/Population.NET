using Core.Bases;
using Populates.Public;
using Populates.Visitors;
using Population.Public.Descriptors;
using System.Linq.Expressions;
using static Populates.Extensions.MethodExtension;

namespace Population.Builders;

public static class SortBuilder
{
    internal static IQueryable ApplySort(this IQueryable query, IMetaPathBag projections, ICollection<SortDescriptor> sorts, ParameterExpression rootParameter)
    {
        Expression? orderByExpression = null;

        foreach (SortDescriptor sort in sorts)
        {
            if (!projections.TryGetValue(MemberPath.InitEmptyRoot(sort.Property), out PathInfo? projection))
            {
                continue;
            }

            Expression propertyAccess = projection.PathMap;

            if (propertyAccess is MethodCallExpression methodCallExpression)
            {
                propertyAccess = new SortBuilderVisitor().Visit(methodCallExpression);
            }

            // Create a Lambda Expression to represent the sorting logic
            LambdaExpression keySelector = Expression.Lambda(propertyAccess, rootParameter);

            if (orderByExpression == null)
            {
                // If it's the first sort descriptor, create the initial OrderBy or OrderByDescending expression
                orderByExpression = sort.Type is SortOrder.Asc
                    ? CallSort(nameof(Enumerable.OrderBy), query.Expression, keySelector)
                    : CallSort(nameof(Enumerable.OrderByDescending), query.Expression, keySelector);
            }
            else
            {
                // For subsequent sort descriptors, use ThenBy or ThenByDescending
                orderByExpression = sort.Type is SortOrder.Asc
                    ? CallSort(nameof(Enumerable.ThenBy), orderByExpression, keySelector)
                    : CallSort(nameof(Enumerable.ThenByDescending), orderByExpression, keySelector);
            }
        }

        if (orderByExpression != null)
        {
            query = query.Provider.CreateQuery(orderByExpression);
        }

        return query;
    }

    internal static ICollection<SortDescriptor>? HandleEmpty<TSource>(ICollection<SortDescriptor>? sortDescriptors)
        => HandleEmpty(sortDescriptors, typeof(TSource));

    internal static ICollection<SortDescriptor>? HandleEmpty(ICollection<SortDescriptor>? sortDescriptors, Type sourceType)
    {
        if (sortDescriptors?.Count > 0)
        {
            return sortDescriptors;
        }

        if (sourceType.IsAssignableTo(typeof(BaseEntity)))
        {
            return [SortDescriptor.Default];
        }

        return sortDescriptors;
    }
}