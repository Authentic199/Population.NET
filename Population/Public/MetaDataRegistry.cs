using Population.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace Population.Public;

public interface IMetaPathBag : IDictionary<MemberPath, PathInfo>
{
    IMetaPathBag Filters();
}

public sealed class MetaPathBag : Dictionary<MemberPath, PathInfo>, IMetaPathBag
{
    public MetaPathBag()
    {
    }

    public MetaPathBag(Dictionary<MemberPath, PathInfo> dictionary)
        : base(dictionary)
    {
    }

    /// <summary>
    /// Filters the paths in the specified <see cref="IMetaPathBag"/> based on certain criteria.
    /// </summary>
    /// <returns>A new <see cref="MetaPathBag"/> containing only the paths that meet the specified criteria.</returns>
    /// <remarks>
    /// This method filters the paths in the provided <see cref="IMetaPathBag"/>, retaining only those paths where the corresponding
    /// types are not classes and not generic collections.
    /// </remarks>
    public IMetaPathBag Filters()
        => new MetaPathBag(
                this
                .Where(x => !x.Value.PathMap.Type.IsClass() && !x.Value.PathMap.Type.IsGenericCollection())
                .ToDictionary(x => x.Key, o => o.Value)
            );
}

public sealed class MetaPropertyBag : Dictionary<MemberPath, List<MetaProperty>>
{
}

public sealed record MetaProperty(PropertyInfo Property, string PropertyPath, bool IsReferences);

public sealed record PathInfo(Expression PathMap, MemberInfo DestinationInfo);
