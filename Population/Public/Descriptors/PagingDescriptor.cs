namespace Population.Public.Descriptors;

public class PagingDescriptor
{
    /// <summary>
    /// Number elements on a page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Pages number to take out of the total pages.
    /// </summary>
    public int Page { get; set; } = 1;
}
