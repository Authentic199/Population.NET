using Infrastructure.Facades.Populates.Exceptions;
using Infrastructure.Facades.Populates.Extensions;
using System.Linq.Expressions;
using System.Reflection;
using static Infrastructure.Facades.Populates.Definations.PopulateConstant;
using static Infrastructure.Facades.Populates.Extensions.MethodExtension;

namespace Infrastructure.Facades.Populates.Visitors;

internal class StringConvertVisitor(MemberInfo baseMember) : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.Name == baseMember.Name
            && node.Member.ResolveMemberType().GetUnderlyingType() == baseMember.ResolveMemberType().GetUnderlyingType()
            && CallToString(node) is MethodCallExpression expression)
        {
            return expression;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Type nodeType = node.Type.GetUnderlyingType();

        if (nodeType != typeof(string)
            && nodeType == baseMember.ResolveMemberType().GetUnderlyingType()
            && !nodeType.IsCollection()
            && CallToString(node) is MethodCallExpression expression)
        {
            return expression;
        }

        return base.VisitConditional(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Type nodeType = node.Type.GetUnderlyingType();

        if (nodeType != typeof(string)
            && nodeType == baseMember.ResolveMemberType().GetUnderlyingType()
            && !nodeType.IsCollection()
            && CallToString(node) is MethodCallExpression expression)
        {
            return expression;
        }

        return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        return node.NodeType == ExpressionType.Convert
            ? TransformSelector(node.Operand)
            : base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == MethodAlias.Select && node.Method.DeclaringType == typeof(Enumerable))
        {
            Expression sourceMember = node.Arguments[0];
            LambdaExpression selector = (LambdaExpression)node.Arguments[^1];
            Expression transformedSelector = TransformSelector(selector.Body);

            return CallSelect(sourceMember, Expression.Lambda(transformedSelector, selector.Parameters[0]));
        }

        if (node.Type.IsPrimitiveType() && node.Type != typeof(string))
        {
            return CallToString(node)
                ?? throw new PopulateNotHandleException($"{nameof(StringConvertVisitor)}:{nameof(VisitMethodCall)} an error occur while {nameof(CallToString)} of primitive type process");
        }

        return base.VisitMethodCall(node);
    }

    private static MethodCallExpression? CallToString(Expression node)
    {
        MethodInfo? toStringMethod = node.Type.GetMethod(MethodAlias.ToStringAlias, []);

        if (toStringMethod != null)
        {
            return Expression.Call(node, toStringMethod);
        }

        return null;
    }

    private Expression TransformSelector(Expression selector)
        => selector switch
        {
            UnaryExpression unary => VisitUnary(unary),
            MemberExpression member => VisitMember(member),
            BinaryExpression binary => VisitBinary(binary),
            ConditionalExpression condition => VisitConditional(condition),
            MethodCallExpression methodCall => VisitMethodCall(methodCall),
            _ => throw new PopulateNotHandleException($"{nameof(CompareOperatorVisitor)}:{nameof(VisitMethodCall)} not supported for this expression type while {nameof(TransformSelector)} process"),
        };
}