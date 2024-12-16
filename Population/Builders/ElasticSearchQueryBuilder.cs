using Infrastructure.Facades.Populates.Definations;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Internal;
using Infrastructure.Facades.Populates.Public;
using Infrastructure.Facades.Populates.Public.Descriptors;
using Nest;

namespace Infrastructure.Facades.Populates.Builders;

internal static class ElasticSearchQueryBuilder
{
    internal static QueryContainer? ResolveQuery(PathInfo memberPathInfo, string value, CompareOperator compareOperator, Func<PathInfo, string, CompareOperator, QueryContainer?> queryAction)
        => queryAction(memberPathInfo, value, compareOperator);

    internal static QueryContainer? EqualGroupQuery<TTInferDocument>(PathInfo memberPathInfo, string value, CompareOperator compareOperator)
        where TTInferDocument : class
    {
        if (
            !OperatorManager.IsEqualGroup(compareOperator)
            || memberPathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
            || !memberType.IsPrimitiveType()
            || !TypeExtension.IsValidValuesForType(value, memberType)
            )
        {
            return default;
        }

        QueryContainer equalQuery = new QueryContainerDescriptor<TTInferDocument>().Term(t => t.Field(memberPathInfo.PathMap).Value(value));

        if (compareOperator == CompareOperator.NotEqual)
        {
            equalQuery = !equalQuery;
        }

        return equalQuery;
    }

    internal static QueryContainer? InGroupQuery<TTInferDocument>(PathInfo memberPathInfo, string value, CompareOperator compareOperator)
        where TTInferDocument : class
    {
        if (
            !OperatorManager.IsInGroup(compareOperator)
            || memberPathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
            || !memberType.IsPrimitiveType()
            || !TypeExtension.IsValidValuesForType(value, memberType)
            )
        {
            return default;
        }

        QueryContainer inQuery = new QueryContainerDescriptor<TTInferDocument>().Terms(t => t.Field(memberPathInfo.PathMap).Terms(value.Split(PopulateConstant.SpecialCharacter.Comma)));

        if (compareOperator == CompareOperator.NotIn)
        {
            inQuery = !inQuery;
        }

        return inQuery;
    }

    internal static QueryContainer? ContainGroupQuery<TTInferDocument>(PathInfo memberPathInfo, string value, CompareOperator compareOperator)
        where TTInferDocument : class
    {
        if (
            !OperatorManager.IsContainGroup(compareOperator)
            || memberPathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
            || !memberType.IsGenericCollection()
            || !TypeExtension.IsValidValuesForType(value, memberType.GetCollectionElementType())
            )
        {
            return default;
        }

        QueryContainer containQuery = new QueryContainerDescriptor<TTInferDocument>().Term(t => t.Field(memberPathInfo.PathMap).Value(value.Split(PopulateConstant.SpecialCharacter.Comma)));

        if (compareOperator == CompareOperator.NotContains)
        {
            containQuery = !containQuery;
        }

        return containQuery;
    }

    internal static QueryContainer? CompareGroupQuery<TTInferDocument>(PathInfo memberPathInfo, string value, CompareOperator compareOperator)
        where TTInferDocument : class
    {
        if (
            !OperatorManager.IsComparisonGroup(compareOperator)
            || memberPathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
            || !memberType.IsNumericType()
            || !TypeExtension.IsValidValuesForType(value, memberType)
            )
        {
            return default;
        }

        QueryContainerDescriptor<TTInferDocument> descriptor = new();

        return compareOperator switch
        {
            CompareOperator.GreaterThan when memberType.IsLong() => descriptor.LongRange(lr => lr.Field(memberPathInfo.PathMap).GreaterThan(long.Parse(value))),
            CompareOperator.GreaterThan when memberType.IsDateTime() => descriptor.DateRange(dr => dr.Field(memberPathInfo.PathMap).GreaterThan(DateMath.FromString(value))),
            CompareOperator.GreaterThan => descriptor.TermRange(dr => dr.Field(memberPathInfo.PathMap).GreaterThan(value)),
            CompareOperator.GreaterThanOrEqual when memberType.IsLong() => descriptor.LongRange(lr => lr.Field(memberPathInfo.PathMap).GreaterThanOrEquals(long.Parse(value))),
            CompareOperator.GreaterThanOrEqual when memberType.IsDateTime() => descriptor.DateRange(dr => dr.Field(memberPathInfo.PathMap).GreaterThanOrEquals(DateMath.FromString(value))),
            CompareOperator.GreaterThanOrEqual => descriptor.TermRange(dr => dr.Field(memberPathInfo.PathMap).GreaterThanOrEquals(value)),
            CompareOperator.LessThan when memberType.IsLong() => descriptor.LongRange(lr => lr.Field(memberPathInfo.PathMap).LessThan(long.Parse(value))),
            CompareOperator.LessThan when memberType.IsDateTime() => descriptor.DateRange(dr => dr.Field(memberPathInfo.PathMap).LessThan(DateMath.FromString(value))),
            CompareOperator.LessThan => descriptor.TermRange(dr => dr.Field(memberPathInfo.PathMap).LessThan(value)),
            CompareOperator.LessThanOrEqual when memberType.IsLong() => descriptor.LongRange(lr => lr.Field(memberPathInfo.PathMap).LessThanOrEquals(long.Parse(value))),
            CompareOperator.LessThanOrEqual when memberType.IsDateTime() => descriptor.DateRange(dr => dr.Field(memberPathInfo.PathMap).LessThanOrEquals(DateMath.FromString(value))),
            CompareOperator.LessThanOrEqual => descriptor.TermRange(dr => dr.Field(memberPathInfo.PathMap).LessThanOrEquals(value)),
            _ => throw new ArgumentException(nameof(CompareGroupQuery)),
        };
    }

    internal static QueryContainer? NullableGroupQuery<TTInferDocument>(PathInfo memberPathInfo, string _, CompareOperator compareOperator)
        where TTInferDocument : class
    {
        if (
            !OperatorManager.IsNullableGroup(compareOperator)
            || memberPathInfo.DestinationInfo.ResolveMemberType() is not Type memberType
            || !memberType.IsNullableType()
            )
        {
            return default;
        }

        QueryContainer? nullableQuery = new QueryContainerDescriptor<TTInferDocument>().Exists(e => e.Field(memberPathInfo.PathMap));

        return compareOperator == CompareOperator.NotNull ? nullableQuery : !nullableQuery;
    }
}
