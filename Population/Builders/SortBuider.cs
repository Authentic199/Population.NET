using Core.Bases;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Public;
using Infrastructure.Facades.Populates.Visitors;
using Nest;
using Population.Public.Descriptors;
using System.Linq.Expressions;
using static Infrastructure.Facades.Populates.Extensions.MethodExtension;
using ComponentPair = (Nest.Field Field, Nest.SortOrder SortOrder);

namespace Infrastructure.Facades.Populates.Builders;

public static class SortBuilder
{
    internal static SortDescriptor<TInferDocument> BuildSortQuery<TInferDocument>(this ICollection<SortDescriptor>? sorts)
        where TInferDocument : class
    {
        SortDescriptor<TInferDocument> sortDescriptor = new();
        SortComponent<TInferDocument>(sorts ?? []).ToList().ForEach(DoFor);
        return sortDescriptor;

        void DoFor(ComponentPair component) => sortDescriptor.Field(component.Field, component.SortOrder);
    }

    internal static IEnumerable<ISort> BuildSorts<TInferDocument>(this ICollection<SortDescriptor>? sorts)
    {
        foreach (ComponentPair sort in SortComponent<TInferDocument>(sorts ?? []))
        {
            yield return new FieldSort()
            {
                Field = sort.Field,
                Order = sort.SortOrder,
            };
        }
    }

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
                orderByExpression = sort.Type is Population.Public.Descriptors.SortOrder.Asc
                    ? CallSort(nameof(Enumerable.OrderBy), query.Expression, keySelector)
                    : CallSort(nameof(Enumerable.OrderByDescending), query.Expression, keySelector);
            }
            else
            {
                // For subsequent sort descriptors, use ThenBy or ThenByDescending
                orderByExpression = sort.Type is Population.Public.Descriptors.SortOrder.Asc
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

    private static IEnumerable<ComponentPair> SortComponent<TInferDocument>(this ICollection<SortDescriptor> sorts)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TInferDocument), "x");
        foreach (SortDescriptor sort in sorts)
        {
            PathInfo? pathInfo = typeof(TInferDocument).GetPathInfoRecursive(sort.Property);

            if (pathInfo is null
                || pathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
                || !memberType.IsPrimitiveType())
            {
                continue;
            }

            Expression<Func<TInferDocument, object>> memberFunc = Expression.Lambda(pathInfo.PathMap, parameter).ToObject<TInferDocument>();
            yield return sort.Type is Population.Public.Descriptors.SortOrder.Asc
                ? (new Field(memberFunc), Nest.SortOrder.Ascending)
                : (new Field(memberFunc), Nest.SortOrder.Descending);
        }
    }
}