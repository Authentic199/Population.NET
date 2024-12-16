using Population.Extensions;
using Population.Internal;
using Population.Internal.Queries;
using Population.Public.Descriptors;
using System.Collections;
using System.Reflection;
using static Population.Internal.Queries.QueryParams;
using ParamsBag = System.Collections.Generic.IDictionary<string, string>;
using ParamsPair = System.Collections.Generic.KeyValuePair<string, string>;

namespace Population.Builders;

public class BindingBuilder
{
    private const string Descending = "desc";
    private const string Ascending = "asc";

    private readonly Type modelType;
    private readonly ParamsBag? sortParams;
    private readonly ParamsBag? filterParams;
    private readonly ParamsBag? searchParams;
    private readonly ParamsBag? populateParams;
    private readonly ParamsBag? paginationParams;

    internal BindingBuilder(QueryParams queryParams)
    {
        modelType = queryParams.ModelType;
        sortParams = queryParams.SortParams;
        filterParams = queryParams.FilterParams;
        searchParams = queryParams.SearchParams;
        populateParams = queryParams.PopulateParams;
        paginationParams = queryParams.PaginationParams;
    }

    /// <summary>
    /// Binds values to properties of the model object based on specified actions.
    /// </summary>
    /// <returns>
    /// The model object with its properties populated according to the specified actions.
    /// </returns>
    /// <remarks>
    /// This method creates an instance of the model object and iterates through its properties. For each property,
    /// it creates an instance of the property's type and performs the appropriate action on it using the <seealso cref="ResolveAction"/>
    /// method. The resulting value is then set to the corresponding property of the model object. Finally, the populated
    /// model object is returned.
    /// </remarks>
    internal object Bind()
    {
        object modelBinding = Activator.CreateInstance(modelType)!;
        foreach (PropertyInfo propertyInfo in modelType.GetProperties())
        {
            object propertyValue = Activator.CreateInstance(propertyInfo.PropertyType)!;
            ResolveAction(propertyInfo.Name)(propertyValue, propertyInfo);
            propertyInfo.SetValue(modelBinding, propertyValue);
        }

        return modelBinding;
    }

    /// <summary>
    /// Resolves the appropriate action to perform based on the provided input.
    /// </summary>
    /// <param name="input">The input string indicating the action to resolve.</param>
    /// <returns>
    /// An <see cref="Action"/> delegate representing the resolved action to perform on an object and its property.
    /// </returns>
    /// <remarks>
    /// This method determines the <see cref="Action"/> to perform based on the input string.
    /// It uses the <seealso cref="DetectAction"/> method to identify the type of action
    /// and then returns the corresponding action delegate for performing the action on an object and its property. Supported actions include sorting,
    /// filtering, searching, populating, and paginating. If the input does not match any supported action, a <seealso cref="NotSupportedException"/> is thrown.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// Thow if the input does not match any supported action./>
    /// </exception>
    private Action<object, PropertyInfo> ResolveAction(string input)
        => input switch
        {
            string sort when DetectAction(sort, QueryAction.Sort) => SetSortValues,
            string filter when DetectAction(filter, QueryAction.Filter) => SetFilterValues,
            string search when DetectAction(search, QueryAction.Search) => SetSearchValue,
            string populate when DetectAction(populate, QueryAction.Populate) => SetPopulateValue,
            string pagination when DetectAction(pagination, QueryAction.Pagination) => SetPaginationValue,
            _ => throw new NotSupportedException("Binding not supported"),
        };

    /// <summary>
    /// Sets the sort values for a specified property of an object based on <seealso cref="sortParams"/>.
    /// </summary>
    /// <param name="sorts">The object containing sort values.</param>
    /// <param name="sortInfo">The <see cref="PropertyInfo"/> object representing the sort information.</param>
    /// <remarks>
    /// This method sets sort values for a collection specified by <paramref name="sorts"/> based on the provided <paramref name="sortInfo"/>.
    /// It iterates through parameters in the <seealso cref="sortParams"/> list, splitting each value into parts and constructing a <see cref="SortDescriptor"/>
    /// object accordingly. The sort order is resolved based on the value parts, with an optional explicit order specified after a colon (:).
    /// If no explicit order is provided, it defaults to ascending order.
    /// </remarks>
    private void SetSortValues(object sorts, PropertyInfo sortInfo)
    {
        Type sortType = sortInfo.PropertyType;
        if (!sortType.IsCollection() || sortParams.IsNullOrEmpty())
        {
            return;
        }

        IList sortCollection = (IList)sorts;
        foreach (ParamsPair paramsPair in sortParams!)
        {
            string[] valueParts = paramsPair.Value.Split(Colons, TrimSplitOptions);
            if (valueParts.Length > 2 || valueParts.Length < 1 || ResolveOrder() is not SortOrder sortOrder)
            {
                continue;
            }

            sortCollection.Add(new SortDescriptor(valueParts[0], sortOrder));

            SortOrder? ResolveOrder()
            {
                if (valueParts.Length == 1)
                {
                    return SortOrder.Asc;
                }

                if (string.Equals(valueParts[1], Ascending, IgnoreCaseCompare))
                {
                    return SortOrder.Asc;
                }

                if (string.Equals(valueParts[1], Descending, IgnoreCaseCompare))
                {
                    return SortOrder.Desc;
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Sets the filter values for a specified property of an object based on <seealso cref="filterParams"/>.
    /// </summary>
    /// <param name="filters">The object containing filter values.</param>
    /// <param name="filterInfo">The <see cref="PropertyInfo"/> object representing the filter information.</param>
    /// <remarks>
    /// This method sets filter values for a collection specified by <paramref name="filters"/> based on the provided <paramref name="filterInfo"/>.
    /// It constructs a <see cref="FilterRequest"/> object with the <seealso cref="filterParams"/> list and iterates through each parameter pair.
    /// For each parameter pair, it attempts to bind a filter object using the <see cref="FilterRequest.Bind"/> method.
    /// If successful, the filter object is added to the filter collection.
    /// </remarks>
    private void SetFilterValues(object filters, PropertyInfo filterInfo)
    {
        Type filterType = filterInfo.PropertyType;
        if (!filterType.IsCollection() || filterParams.IsNullOrEmpty())
        {
            return;
        }

        IList filterCollection = (IList)filters;
        FilterRequest filterRequest = new(filterParams!);
        foreach (ParamsPair paramsPair in filterParams!)
        {
            if (filterRequest.Bind(paramsPair) is object filter)
            {
                filterCollection.Add(filter);
            }
        }
    }

    /// <summary>
    /// Sets the search value for a specified property of an object based on <seealso cref="searchParams"/>.
    /// </summary>
    /// <param name="search">The object containing search values.</param>
    /// <param name="searchInfo">The <see cref="PropertyInfo"/> object representing the search information.</param>
    /// <remarks>
    /// This method sets search values for the specified <paramref name="search"/> object based on the provided <paramref name="searchInfo"/>.
    /// It iterates through properties of the searchType (defined by the <paramref name="searchInfo"/>), attempting to resolve values for each property
    /// from the <seealso cref="searchParams"/> list. Depending on whether the property is a collection or not, it resolves the property value or
    /// collection value using appropriate methods. If a value is successfully resolved, it is set to the corresponding property
    /// of the search object.
    /// </remarks>
    private void SetSearchValue(object search, PropertyInfo searchInfo)
    {
        Type searchType = searchInfo.PropertyType;
        if (!searchType.IsClass() || searchType.IsCollection() || searchParams.IsNullOrEmpty())
        {
            return;
        }

        foreach (PropertyInfo searchProperty in searchType.GetProperties())
        {
            bool isCollection = searchProperty.PropertyType.IsCollection();

            object? result = isCollection
            ? searchProperty.ResolveCollectionValue(searchParams!, searchInfo.Name)
            : searchProperty.ResolvePropertyValue(searchParams!, searchInfo.Name);

            if (result != null && (!isCollection || ((IList)result).Count > 0))
            {
                searchProperty.SetValue(search, result);
            }
        }
    }

    /// <summary>
    /// Sets the pagination value for a specified property of an object based on <seealso cref="paginationParams"/>.
    /// </summary>
    /// <param name="pagination">The object containing pagination values.</param>
    /// <param name="paginationInfo">The <see cref="PropertyInfo"/> object representing the pagination information.</param>
    /// <remarks>
    /// This method sets pagination values for the specified <paramref name="pagination"/> object based on the provided <paramref name="paginationInfo"/>.
    /// It iterates through properties of the paginationType (defined by the <paramref name="paginationInfo"/>.), skipping properties that are collections.
    /// For each non-collection property, it attempts to resolve a value from the <see cref="paginationParams"/> list using the <see cref="BindingUtilities.ResolvePropertyValue"/>
    /// method. If a value is successfully resolved, it is set to the corresponding property of the pagination object.
    /// </remarks>
    private void SetPaginationValue(object pagination, PropertyInfo paginationInfo)
    {
        Type paginationType = paginationInfo.PropertyType;
        if (!paginationType.IsClass() || paginationType.IsCollection() || paginationParams.IsNullOrEmpty())
        {
            return;
        }

        foreach (PropertyInfo paginationProperty in paginationType.GetProperties())
        {
            if (paginationProperty.PropertyType.IsCollection())
            {
                continue;
            }

            if (paginationProperty.ResolvePropertyValue(paginationParams!, paginationInfo.Name) is object result)
            {
                paginationProperty.SetValue(pagination, result);
            }
        }
    }

    /// <summary>
    /// Sets the populate value for a specified property of an object based on <seealso cref="populateParams"/>.
    /// </summary>
    /// <param name="populate">The object containing populate values.</param>
    /// <param name="populateInfo">The <see cref="PropertyInfo"/> object representing the populate information.</param>
    /// <remarks>
    /// This method sets populate values for the specified <paramref name="populate"/> object based on the provided <paramref name="populateInfo"/>.
    /// It iterates through properties of the populateType (defined by the <paramref name="populateInfo"/>), focusing on properties that are collections of strings.
    /// For each such property, it binds values from the <seealso cref="populateParams"/> list, removes duplicates, and converts them into a list. If no values are
    /// found, it defaults to adding a wildcard value(*). The resulting list is then set to the corresponding property of the populate object.
    /// </remarks>
    private void SetPopulateValue(object populate, PropertyInfo populateInfo)
    {
        Type populateType = populateInfo.PropertyType;
        if (!populateType.IsClass() || populateType.IsCollection() || populateParams.IsNullOrEmpty())
        {
            return;
        }

        foreach (PropertyInfo populateProperty in populateType.GetProperties())
        {
            Type propertyType = populateProperty.PropertyType;
            if (propertyType.IsCollection() && propertyType.GetCollectionElementType() == typeof(string))
            {
                HashSet<string> populateValue = new(populateParams!.Bind(), StringComparer.OrdinalIgnoreCase);
                if (populateValue.Count == 0)
                {
                    populateValue.Add(Asterisk);
                }

                populateProperty.SetValue(populate, populateValue);
            }
        }
    }
}
