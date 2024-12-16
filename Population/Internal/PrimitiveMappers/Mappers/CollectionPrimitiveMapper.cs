using Populates.Extensions;
using Populates.Internal.Projection;
using System.Linq.Expressions;
using static Populates.Extensions.MethodExtension;

namespace Populates.Internal.PrimitiveMappers.Mappers;

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
