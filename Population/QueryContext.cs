using Populates.Internal.Queries;
using Populates.Public.Descriptors;
using Microsoft.AspNetCore.Mvc;
using Population.Public.Descriptors;

namespace Populates;

[ModelBinder(typeof(QueryBinder))]
public class QueryContext
{
    public PagingDescriptor Pagination { get; set; } = new();

    public List<SortDescriptor>? Sort { get; set; }

    public List<FilterDescriptor>? Filters { get; set; }

    public SearchDescriptor? Search { get; set; }

    public PopulateDescriptor Populate { get; set; } = new();
}
