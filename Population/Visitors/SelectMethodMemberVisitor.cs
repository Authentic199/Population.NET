using Infrastructure.Facades.Populates.Definations;
using Infrastructure.Facades.Populates.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Facades.Populates.Visitors;

/// <summary>
/// A visitor that modifies "Select" and "SelectMany" method calls in an expression tree to replace the accessed member with a specified expression.
/// </summary>
internal class SelectMethodMemberVisitor : ExpressionVisitor
{
    private readonly Expression replaceExpression;

    internal SelectMethodMemberVisitor(Expression replaceExpression)
    {
        this.replaceExpression = replaceExpression;
    }

    /// <summary>
    /// Visits a method call expression and replaces member access if the method is "Select" or "SelectMany".
    /// </summary>
    /// <param name="node">The <see cref="MethodCallExpression"/> to visit.</param>
    /// <returns>
    /// The modified <see cref="Expression"/> if member access is replaced; otherwise, the result of the base visit method.
    /// </returns>
    /// <remarks>
    /// This method checks if the method in the node is "Select" or "SelectMany". If the lambda expression within the method
    /// call contains a member access, it delegates the replacement logic to <see cref="ReplaceMemberAccess"/>.
    /// </remarks>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        MethodInfo method = node.Method;
        if (!method.Name.EqualSelect() && !method.Name.EqualSelectMany())
        {
            return base.VisitMethodCall(node);
        }

        LambdaExpression oldMemberLambda = (LambdaExpression)node.Arguments[1];

        if (oldMemberLambda.Body is MemberExpression)
        {
            return ReplaceMemberAccess(node.Arguments[0], oldMemberLambda, method);
        }

        return base.VisitMethodCall(node);
    }

    /// <summary>
    /// Replaces member access in a "Select" or "SelectMany" method call with a custom expression.
    /// </summary>
    /// <param name="rootMember">The root member expression representing the source of the method call.</param>
    /// <param name="oldMemberLambda">The original lambda expression containing the member access.</param>
    /// <param name="method">The <see cref="MethodInfo"/> representing the method being called ("Select" or "SelectMany").</param>
    /// <returns>
    /// A new <see cref="MethodCallExpression"/> with the member access replaced by the custom expression.
    /// </returns>
    /// <remarks>
    /// This method generates a new lambda expression that replaces the original member access with a predefined expression. 
    /// Depending on the method type ("Select" or "SelectMany") and whether the replacement is a collection, it invokes
    /// the appropriate method extension for the transformation.
    /// </remarks>
    private MethodCallExpression ReplaceMemberAccess(Expression rootMember, LambdaExpression oldMemberLambda, MethodInfo method)
    {
        ParameterExpression memberParameter = oldMemberLambda.Parameters[0];
        LambdaExpression replaceLambda = Expression.Lambda(replaceExpression, memberParameter);

        return method.Name.EqualSelectMany() || replaceExpression.Type.IsCollection()
            ? MethodExtension.CallSelectMany(rootMember, replaceLambda)
            : MethodExtension.CallSelect(rootMember, replaceLambda);
    }
}
