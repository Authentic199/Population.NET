namespace Infrastructure.Facades.Populates.Public.Queries;

public class QueryOptions
{
    public static QueryOptions EnableAsSplitQuery() => new() { AsSplitQuery = true };

    /// <summary>
    /// Parameter use when mapping
    /// </summary>
    public object? Parameters { get; set; }

    public bool AsSplitQuery { get; set; }
}