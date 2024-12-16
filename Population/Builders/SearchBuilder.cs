using Populates.Extensions;
using Populates.Public;
using Populates.Public.Descriptors;

namespace Populates.Builders;

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
}
