using AutoMapper;
using Population.Public;

namespace Population.Internal.Projection;

internal sealed class TypeMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMapper"/> class.
    /// Trường hợp source type bằng destination type -> truyền entity không truyền response
    /// </summary>
    internal TypeMapper(Type objectType, MemberPath memberPath)
    {
        MemberPath = memberPath;
        SourceType = objectType;
        DestinationType = objectType;
        PropertyMaps = InitPropertyMapper(objectType);
    }

    internal TypeMapper(TypeMap typeMap, MemberPath memberPath)
    {
        MemberPath = memberPath;
        SourceType = typeMap.SourceType;
        DestinationType = typeMap.DestinationType;
        PropertyMaps = InitPropertyMapper(typeMap);
    }

    public Type SourceType { get; }

    public Type DestinationType { get; }

    public IReadOnlyCollection<PropertyMapper> PropertyMaps { get; }

    public MemberPath MemberPath { get; }

    private List<PropertyMapper> InitPropertyMapper(Type objectType)
        => objectType.GetProperties().Select(x => new PropertyMapper(this, x, MemberPath)).ToList();

    private List<PropertyMapper> InitPropertyMapper(TypeMap typeMap)
        => typeMap.PropertyMaps.Select(x => new PropertyMapper(this, x, MemberPath)).ToList();
}