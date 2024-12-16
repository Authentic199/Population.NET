using Populates.Extensions;
using Populates.Internal.PrimitiveMappers.Mappers;
using Populates.Internal.Projection;
using System.Linq.Expressions;

namespace Populates.Internal.PrimitiveMappers;

internal interface IPrimitiveMapper
{
    internal static readonly IReadOnlyCollection<IPrimitiveMapper> DefaultMapper =
    [
        new StringPrimitiveMapper(),
        new NullableSourcePrimitiveMapper(),
        new CollectionPrimitiveMapper(),
    ];

    Expression Map(Expression resolveExpression, PropertyMapper mapper);

    bool IsMatch(PropertyMapper mapper);

    internal static Expression TryMap(Expression expression, PropertyMapper propertyMapper)
    {
        if (!DefaultMapper.TryFirst(x => x.IsMatch(propertyMapper), out IPrimitiveMapper? primitiveMapper)
            || (
                primitiveMapper is not CollectionPrimitiveMapper
                && expression.NodeType is not ExpressionType.MemberAccess
                )
            )
        {
            return expression;
        }

        return primitiveMapper!.Map(expression, propertyMapper);
    }
}