using AutoMapper;
using Population.Public;
using System.Linq.Expressions;
using System.Reflection;

namespace Population.Internal.Projection;

internal sealed class PropertyMapper
{
    internal PropertyMapper(
        TypeMapper typeMapper,
        PropertyInfo propertyInfo,
        MemberPath rootPath)
        : this(
              rootPath,
              typeMapper,
              propertyInfo,
              propertyInfo.PropertyType,
              propertyInfo.PropertyType,
              propertyInfo.Name,
              MemberPath.Init(rootPath.Value, propertyInfo.Name),
              propertyInfo.AllowedNull()
            )
    {
    }

    internal PropertyMapper(
        TypeMapper typeMapper,
        PropertyMap propertyMap,
        MemberPath rootPath)
        : this(
              rootPath,
              typeMapper,
              propertyMap.DestinationMember,
              propertyMap.SourceType,
              propertyMap.DestinationType,
              propertyMap.DestinationName,
              MemberPath.Init(rootPath.Value, propertyMap.DestinationName),
              propertyMap.AllowNull ?? propertyMap.AllowsNullDestinationValues,
              propertyMap.Ignored,
              propertyMap.CustomMapExpression,
              propertyMap.IncludedMember
            )
    {
    }

    private PropertyMapper(
        MemberPath rootPath,
        TypeMapper typeMapper,
        MemberInfo destinationMember,
        Type sourceType,
        Type destinationType,
        string destinationName,
        MemberPath memberPath,
        bool allowNull,
        bool ignored = false,
        LambdaExpression? customMapExpression = default,
        IncludedMember? includedMember = null)
    {
        TypeMapper = typeMapper;
        DestinationMember = destinationMember;
        SourceType = sourceType;
        DestinationType = destinationType;
        DestinationName = destinationName;
        Ignored = ignored;
        MemberPath = memberPath;
        RootPath = rootPath;
        CustomMapExpression = customMapExpression;
        AllowNull = allowNull;
        IncludedMember = includedMember;
    }

    public TypeMapper TypeMapper { get; }

    public MemberInfo DestinationMember { get; }

    public Type SourceType { get; }

    public Type DestinationType { get; }

    public string DestinationName { get; }

    public bool Ignored { get; }

    public MemberPath MemberPath { get; }

    public MemberPath RootPath { get; }

    public LambdaExpression? CustomMapExpression { get; }

    public IncludedMember? IncludedMember { get; set; }

    public bool AllowNull { get; set; }
}
