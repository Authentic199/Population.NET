using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Public;
using Infrastructure.Facades.Populates.Public.Descriptors;
using Nest;
using Population.Public.Attributes;
using System.Linq.Expressions;

namespace Infrastructure.Facades.Populates.Builders;

internal static class SearchBuilder
{
    /// <summary>
    /// Builds an enumerable collection of <see cref="FilterDescriptor"/> based on the specified search property accesses, keyword, and optional fields.
    /// </summary>
    /// <param name="searchPropertyAccesses">The <see cref="IMetaPathBag"/> containing information about search property accesses.</param>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="fields">Optional fields to restrict the search.</param>
    /// <param name="ignores">Optional types to ignore the search.</param>
    /// <returns>
    /// Returns an enumerable collection of <see cref="FilterDescriptor"/> constructed based on the provided inputs.
    /// </returns>
    /// <remarks>
    /// This method iterates through the provided search property accesses and constructs <see cref="FilterDescriptor"/>
    /// for each property path. It checks if the destination type of each property is an enum and if the property
    /// path matches any of the specified fields (if provided). If the conditions are met, it creates a <see cref="FilterDescriptor"/>
    /// for the property path with the specified keyword and logical operator.
    /// The resulting <see cref="FilterDescriptor"/> are returned as an enumerable collection.
    /// </remarks>
    internal static IEnumerable<FilterDescriptor> BuildDescriptor(this IMetaPathBag searchPropertyAccesses, string keyword, IEnumerable<string>? fields, params Type[] ignores)
    {
        foreach ((MemberPath memberPath, PathInfo pathInfo) in searchPropertyAccesses)
        {
            Type memberType = pathInfo.DestinationInfo.ResolveMemberType().GetUnderlyingType();

            if (
                 memberType.IsIgnoreSearch() ||
                 (ignores?.Length > 0 && ignores.Contains(memberType)) ||
                 (fields?.Any() == true && !fields.Contains(memberPath.Value, StringComparer.OrdinalIgnoreCase))
                )
            {
                continue;
            }

            yield return new FilterDescriptor(memberPath.Value, keyword, NextLogicalOperator.Or, CompareOperator.Contains, nameof(QueryContext.Search));
        }
    }

    internal static QueryContainer BuildSearchQuery<TInferDocument>(this SearchDescriptor? search)
        where TInferDocument : class
    {
        QueryContainerDescriptor<TInferDocument> searchQuery = new();
        if (search is null || search.Keyword is null)
        {
            return searchQuery;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(TInferDocument), "x");
        search.Fields ??= [.. typeof(TInferDocument).GetPropertyRecursiveWithDeep(1, typeof(NotSearchAttribute))];

        return searchQuery.QueryString(
            qs =>
                qs.Fields(
                    fs =>
                        fs.Fields(SearchFields<TInferDocument>(search.Fields, parameter))
                    )
                  .Query($"*{search.Keyword.RegexReplace(RegexExtension.SpecialCharacterPattern, "\\$0")}*")
                );
    }

    private static IEnumerable<Field> SearchFields<TInferDocument>(IEnumerable<string> fields, ParameterExpression parameter)
    {
        foreach (string searchField in fields)
        {
            if (typeof(TInferDocument).GetPathInfoRecursive(searchField) is not PathInfo pathInfo
                || pathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
                || (!pathInfo.DestinationInfo.HasAttribute<KeywordAttribute>() && memberType != typeof(string) && memberType.IsGuid())
                )
            {
                continue;
            }

            yield return new Field(Expression.Lambda(pathInfo.PathMap, parameter).ToObject<TInferDocument>());
        }
    }
}
