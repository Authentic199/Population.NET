using Core.Bases;

namespace Population.Public.Descriptors;

public class SortDescriptor
{
    internal static readonly SortDescriptor Default = new(nameof(BaseEntity.CreatedAt), SortOrder.Desc);

    public SortDescriptor(string property, SortOrder type)
    {
        Property = property;
        Type = type;
    }

    public string Property { get; set; }

    public SortOrder Type { get; set; }
}

public enum SortOrder
{
    /// <summary>
    /// Represents ascending sort order.
    /// </summary>
    Asc = 1,

    /// <summary>
    /// Represents descending sort order.
    /// </summary>
    Desc = 2,
}