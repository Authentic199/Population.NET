using System.Linq.Expressions;

namespace Population.Visitors;

internal class ExpressionCloneVisitor : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, ParameterExpression> parameterMap = new();

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return parameterMap.TryGetValue(node, out ParameterExpression? mappedParameter) ? mappedParameter : node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return Expression.MakeMemberAccess(Visit(node.Expression), node.Member);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        return Expression.MakeBinary(node.NodeType, Visit(node.Left), Visit(node.Right));
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return Expression.Constant(node.Value, node.Type);
    }
}