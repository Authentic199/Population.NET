namespace Infrastructure.Facades.Populates.Internal.Queries;

public enum QueryAction
{
    /// <summary>
    /// Sort the results based on specified criteria.
    /// </summary>
    Sort = 1,

    /// <summary>
    /// Search for specific items using specified parameters.
    /// </summary>
    Search = 2,

    /// <summary>
    /// Filter the results based on specified conditions.
    /// </summary>
    Filter = 3,

    /// <summary>
    /// Perform pagination to retrieve a subset of results.
    /// </summary>
    Pagination = 4,

    /// <summary>
    /// Populate related entities or properties in the query result.
    /// </summary>
    Populate = 5,
}
