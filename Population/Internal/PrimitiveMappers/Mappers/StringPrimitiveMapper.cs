using Population.Internal.Projection;
using System.Linq.Expressions;

namespace Population.Internal.PrimitiveMappers.Mappers;

internal class StringPrimitiveMapper : IPrimitiveMapper
{
    public bool IsMatch(PropertyMapper mapper) => mapper.DestinationType == typeof(string);

    public Expression Map(Expression resolveExpression, PropertyMapper mapper) => resolveExpression;
}
