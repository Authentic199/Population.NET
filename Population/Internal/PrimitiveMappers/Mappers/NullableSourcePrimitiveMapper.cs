using Populates.Extensions;
using Populates.Internal.Projection;
using System.Linq.Expressions;

namespace Populates.Internal.PrimitiveMappers.Mappers;

internal class NullableSourcePrimitiveMapper : IPrimitiveMapper
{
    public bool IsMatch(PropertyMapper mapper)
        => mapper.SourceType.IsNullableType() && mapper.SourceType.GetUnderlyingType() == mapper.DestinationType;

    public Expression Map(Expression resolveExpression, PropertyMapper mapper) => Expression.Coalesce(resolveExpression, Expression.Default(mapper.DestinationType));
}
