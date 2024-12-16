using Infrastructure.Facades.Populates.Builders;
using Infrastructure.Facades.Populates.Public.Descriptors;
using Nest;
using Population.Public.Descriptors;

namespace Infrastructure.Facades.Populates;

public static class DataManipulationExtension
{
    public static SearchDescriptor<TInferDocument> ApplyFilter<TInferDocument>(this SearchDescriptor<TInferDocument> baseDescriptor, ICollection<FilterDescriptor>? filters)
        where TInferDocument : class
        => baseDescriptor.Query(_ => filters.BuildFilterQuery<TInferDocument>());

    public static ISearchRequest ApplyFilter<TInferDocument>(this ISearchRequest searchRequest, ICollection<FilterDescriptor>? filters)
        where TInferDocument : class
    {
        searchRequest.Query &= filters.BuildFilterQuery<TInferDocument>();
        return searchRequest;
    }

    public static SearchDescriptor<TInferDocument> ApplySort<TInferDocument>(this SearchDescriptor<TInferDocument> baseDescriptor, ICollection<SortDescriptor>? sorts)
        where TInferDocument : class
        => baseDescriptor.Sort(_ => sorts.BuildSortQuery<TInferDocument>());

    public static ISearchRequest ApplySort<TInferDocument>(this ISearchRequest searchRequest, ICollection<SortDescriptor>? sorts)
        where TInferDocument : class
    {
        searchRequest.Sort ??= [];
        foreach (ISort sort in sorts.BuildSorts<TInferDocument>())
        {
            searchRequest.Sort.Add(sort);
        }

        return searchRequest;
    }

    public static SearchDescriptor<TInferDocument> ApplySearch<TInferDocument>(this SearchDescriptor<TInferDocument> baseDescriptor, SearchDescriptor? search)
        where TInferDocument : class
        => baseDescriptor.Query(_ => search.BuildSearchQuery<TInferDocument>());

    public static ISearchRequest ApplySearch<TInferDocument>(this ISearchRequest searchRequest, SearchDescriptor? search)
        where TInferDocument : class
    {
        searchRequest.Query &= search.BuildSearchQuery<TInferDocument>();
        return searchRequest;
    }
}
