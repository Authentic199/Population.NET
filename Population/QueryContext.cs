using Microsoft.AspNetCore.Mvc;
using Population.Internal.Queries;
using Population.Public.Descriptors;
using Population.Public.Descriptors;

namespace Population;

[ModelBinder(typeof(QueryBinder))]
public class QueryContext
{
    public PagingDescriptor Pagination { get; set; } = new();

    public List<SortDescriptor>? Sort { get; set; }

    public List<FilterDescriptor>? Filters { get; set; }

    public SearchDescriptor? Search { get; set; }

    public PopulateDescriptor Populate { get; set; } = new();
}
