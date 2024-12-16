using System.Linq.Expressions;

namespace Population.Visitors;

public class ReplaceParameterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression oldNode;
    private readonly Expression newNode;

    public ReplaceParameterVisitor(ParameterExpression oldNode, Expression newNode)
    {
        this.oldNode = oldNode;
        this.newNode = newNode;
    }

    protected override Expression VisitParameter(ParameterExpression node) => node == oldNode && newNode is ParameterExpression ? newNode : base.VisitParameter(node);

    protected override Expression VisitMember(MemberExpression node) => node.Expression == oldNode ? Expression.Property(newNode, node.Member.Name) : base.VisitMember(node);
}