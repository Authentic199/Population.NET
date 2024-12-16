using Population.Builders;
using System.Linq.Expressions;
using System.Reflection;

namespace Population.Visitors;

public class SortBuilderVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == MethodAlias.Select && node.Method.DeclaringType == typeof(Enumerable))
        {
            Expression source = node.Arguments[0];
            LambdaExpression selector = (LambdaExpression)node.Arguments[^1];
            if (
               selector.Body is MemberExpression memberExpression
               && memberExpression.Member is PropertyInfo property
            )
            {
                selector = property.PropertyType == typeof(Guid)
                    ? Expression.Lambda(selector.Body.ConvertStringType(property), selector.Parameters[0])
                    : selector;

                return CallCore(EnumerableMethod(Max, 2, 2), source, selector);
            }
        }

        return base.VisitMethodCall(node);
    }
}