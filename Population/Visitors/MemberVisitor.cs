using Infrastructure.Facades.Populates.Exceptions;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Internal.Projection;
using System.Linq.Expressions;
using static Infrastructure.Facades.Populates.Extensions.MethodExtension;

namespace Infrastructure.Facades.Populates.Visitors;

internal class MemberVisitor : ExpressionVisitor
{
    private readonly ProjectionRequest request;
    private readonly Func<ProjectionRequest, Expression, MemberInitExpression> memberInit;
    private readonly Type anonymousType;
    private readonly Type sourceType;
    private int parameterCount;

    internal MemberVisitor(ProjectionRequest request, Func<ProjectionRequest, Expression, MemberInitExpression> memberInit)
    {
        sourceType = request.SourceType;
        anonymousType = request.AnonymousType;
        this.request = request;
        this.memberInit = memberInit;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Arguments.Count == 2 && node.Method.IsGenericMethod)
        {
            /* Example:
                -> SourceTypes.Orderby(x => x) -> SourceTypes.Orderby(x => x).Select(x => new AnonymousType {}).ToList()
                -> SourceTypes.Take(1) -> SourceTypes.Take(1).Select(x => new AnonymousType {}).ToList()
            */
            if (node.Type.IsGenericCollectionOfType(sourceType))
            {
                return CallToListSelect(node, CreateInitExpression(node));
            }

            Expression memberAccess = node.Arguments[0];
            Expression selector = node.Arguments[1];

            if (memberAccess.Type.IsGenericCollectionOfType(sourceType))
            {
                /* Example:
                    -> SourceTypes.LastOrDefault(x => conditionExpression)   -> SourceTypes.Where(x => conditionExpression).Select(x => new AnonymousType {}).LastOrDefault()
                    -> SourceTypes.FirstOrDefault(x => conditionExpression)  -> SourceTypes.Where(x => conditionExpression).Select(x => new AnonymousType {}).FirstOrDefault()
                    -> SourceTypes.SingleOrDefault(x => conditionExpression) -> SourceTypes.Where(x => conditionExpression).Select(x => new AnonymousType {}).SingleOrDefault()
                */
                if (selector is LambdaExpression conditionExpression && conditionExpression.ReturnType == typeof(bool))
                {
                    return CallCore(
                        EnumerableMethod(node.Method.Name, 1, 1),
                        CallWhereSelect(memberAccess, conditionExpression, CreateInitExpression(memberAccess)));
                }

                /* Example:
                    -> SourceTypes.ElementAt(0) -> SourceTypes.Select(x => new AnonymousType {}).ElementAt(0)
                */
                if (selector is ConstantExpression constantExpression)
                {
                    return CallCore(
                        EnumerableMethod(node.Method),
                        CallSelect(memberAccess, CreateInitExpression(memberAccess)),
                        constantExpression);
                }
            }

            /* Dô cái này chắc chắn could not be translate -> đã test nếu chưa chỉnh luôn rồi
               Chỉnh để hàm này không bị lỗi thôi
                Ex:
                -> SourceTypes.Min(x => x) -> SourceTypes.Min(x => new AnonymousType {})
                -> SourceTypes.Max(x => x) -> SourceTypes.Max(x => new AnonymousType {})
            */
            if (selector is LambdaExpression accessLambdaExpression && accessLambdaExpression.ReturnType == sourceType)
            {
                return CallCore(
                    EnumerableMethod(node.Method),
                    memberAccess,
                    VisitLambdaExpression(accessLambdaExpression));
            }
        }

        Expression argumentExpression = node.Arguments[0];

        /* Example:
            -> SourceTypes.ToList()          -> SourceTypes.Select(x => new AnonymousType {}).ToList()
            -> SourceTypes.FirstOrDefault()  -> SourceTypes.Select(x => new AnonymousType {}).FirstOrDefault()
        */
        if (node.Arguments.Count == 1 &&
            node.Method.IsGenericMethod &&
            argumentExpression.Type.IsGenericCollectionOfType(sourceType))
        {
            return CallCore(
                EnumerableMethod(node.Method),
                CallSelect(argumentExpression, CreateInitExpression(argumentExpression)));
        }

        throw new PopulateNotHandleException($"`{nameof(MemberVisitor)}`:`{nameof(VisitMethodCall)}` cannot handle this method yet");
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node.Type == sourceType
            ? memberInit(request, node)
            : base.VisitParameter(node);

    protected override Expression VisitBinary(BinaryExpression node)
        => Expression.MakeBinary(node.NodeType, Visit(node.Left), Visit(node.Right), anonymousType.IsNullableType(), node.Method);

    protected override Expression VisitConditional(ConditionalExpression node)
        => Expression.Condition(node.Test, Visit(node.IfTrue), Visit(node.IfFalse));

    protected override Expression VisitMember(MemberExpression node)
        => node.Type == sourceType
            ? memberInit(request, node)
            : base.VisitMember(node);

    private LambdaExpression CreateInitExpression(Expression expression)
    {
        Parametor(expression.GetGenericArgument(), out ParameterExpression parameter);
        return Expression.Lambda(memberInit(request, parameter), parameter);
    }

    private LambdaExpression VisitLambdaExpression(LambdaExpression lambda)
        => lambda.ReturnType == sourceType ? Expression.Lambda(Visit(lambda.Body), lambda.Parameters[0]) : lambda;

    private void Parametor(Type type, out ParameterExpression parameterExpression)
    {
        string parameter = ProjectionUtilities.IncrementParameter("o", ref parameterCount);
        parameterExpression = Expression.Parameter(type, parameter);
    }
}
