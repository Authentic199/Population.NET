using AutoMapper;
using Infrastructure.Facades.Populates.Exceptions;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Public;

namespace Infrastructure.Facades.Populates.Internal.Projection;

internal class ProjectionRequest : IEquatable<ProjectionRequest>
{
    private readonly IConfigurationProvider configurationProvider;
    private readonly PopulateAnalyzer populateAnalyzer;

    internal ProjectionRequest(IConfigurationProvider configurationProvider, Type sourceType, Type destinationType, PopulateAnalyzer populateAnalyzer, MemberPath memberPath)
    {
        this.configurationProvider = configurationProvider;
        DestinationType = destinationType;
        this.populateAnalyzer = populateAnalyzer;
        SourceType = sourceType;
        MemberPath = memberPath;
        TypeMapper = FindTypeMapper();
        AnonymousType = AnonymousTypeGenerator.Generate(populateAnalyzer.GetPropertySelection(memberPath));
    }

    public Type SourceType { get; }

    public Type DestinationType { get; }

    public MemberPath MemberPath { get; set; }

    public Type AnonymousType { get; }

    public TypeMapper TypeMapper { get; }

    public IReadOnlyCollection<PropertyMapper> PropertyMaps => TypeMapper.PropertyMaps;

    public bool Equals(ProjectionRequest? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return configurationProvider == other.configurationProvider
            && SourceType == other.SourceType
            && DestinationType == other.DestinationType
            && MemberPath == other.MemberPath
            && populateAnalyzer.Equals(other.populateAnalyzer);
    }

    public override bool Equals(object? obj) => obj is ProjectionRequest projectionRequest && Equals(projectionRequest);

    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(configurationProvider);
        hashCode.Add(SourceType);
        hashCode.Add(DestinationType);
        hashCode.Add(MemberPath);
        hashCode.Add(populateAnalyzer);
        return hashCode.ToHashCode();
    }

    private TypeMapper FindTypeMapper()
    {
        TypeMap typeMap = configurationProvider.FindTypeMap(SourceType, DestinationType);
        if (typeMap == null && SourceType != DestinationType)
        {
            throw new PopulateNotHandleException($"`{nameof(ProjectionRequest)}`:`{nameof(FindTypeMapper)}` - Unable create or missing map for type {SourceType.Name} to {DestinationType.Name}");
        }

        return typeMap == null ? new TypeMapper(DestinationType, MemberPath) : new(typeMap, MemberPath);
    }
}
