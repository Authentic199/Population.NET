using AutoMapper.Internal;
using Infrastructure.Facades.Populates.Builders;
using Infrastructure.Facades.Populates.Extensions;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using DictionaryParameters = System.Collections.Generic.IDictionary<string, object>;

namespace Infrastructure.Facades.Populates.Visitors;

internal abstract class QueryParameterVisitor : ExpressionVisitor
{
    protected abstract Expression? GetValue(string parameterName);

    protected override Expression VisitMember(MemberExpression node)
    {
        if (!node.Member.DeclaringType.Has<CompilerGeneratedAttribute>())
        {
            return base.VisitMember(node);
        }

        Expression? parameterValue = GetValue(node.Member.Name);
        return parameterValue == null ? base.VisitMember(node) : parameterValue.ToType(node.Member.ResolveMemberType());
    }
}

internal class ObjectQueryParameterVisitor : QueryParameterVisitor
{
    private readonly object queryParameters;

    internal ObjectQueryParameterVisitor(object queryParameters) => this.queryParameters = queryParameters;

    protected override Expression? GetValue(string parameterName)
        => queryParameters.GetType().GetProperty(parameterName) is { } matchingProperty ? Expression.Property(Expression.Constant(queryParameters), matchingProperty) : default;
}

internal class DictionaryQueryParameterVisitor : QueryParameterVisitor
{
    private readonly DictionaryParameters dictionaryQueryParameter;

    internal DictionaryQueryParameterVisitor(DictionaryParameters dictionaryQueryParameter) => this.dictionaryQueryParameter = dictionaryQueryParameter;

    protected override Expression? GetValue(string parameterName) => dictionaryQueryParameter.TryGetValue(parameterName, out object? queryParameter) ? Expression.Constant(queryParameter) : default;
}