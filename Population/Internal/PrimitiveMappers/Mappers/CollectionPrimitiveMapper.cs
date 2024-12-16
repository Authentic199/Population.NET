using Population.Extensions;
using Population.Internal.Projection;
using System.Linq.Expressions;
using static Population.Extensions.MethodExtension;

namespace Population.Internal.PrimitiveMappers.Mappers;

internal class CollectionPrimitiveMapper : IPrimitiveMapper
{
    public bool IsMatch(PropertyMapper mapper)
    {
        Type sourceType = mapper.SourceType;
        Type destinationType = mapper.DestinationType;

        return sourceType.IsCollection()
            && destinationType.IsCollection()
            && destinationType != sourceType
            && !sourceType.IsAssignableTo(destinationType);
    }

    public Expression Map(Expression resolveExpression, PropertyMapper mapper)
        => mapper.DestinationType.IsArray ? CallToArray(resolveExpression) : CallToList(resolveExpression);
}
