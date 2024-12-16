﻿using Infrastructure.Facades.Populates.Internal.Projection;
using System.Linq.Expressions;

namespace Infrastructure.Facades.Populates.Internal.PrimitiveMappers.Mappers;

internal class StringPrimitiveMapper : IPrimitiveMapper
{
    public bool IsMatch(PropertyMapper mapper) => mapper.DestinationType == typeof(string);

    public Expression Map(Expression resolveExpression, PropertyMapper mapper) => resolveExpression;
}