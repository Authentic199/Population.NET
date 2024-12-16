using Populates.Builders;
using Populates.Exceptions;
using Populates.Extensions;
using Populates.Internal;
using Populates.Public.Descriptors;
using System.Linq.Expressions;
using System.Reflection;
using static Populates.Definations.PopulateConstant;
using static Populates.Definations.PopulateConstant.MethodAlias;
using static Populates.Definations.PopulateOptions;
using static Populates.Extensions.MethodExtension;

namespace Populates.Visitors;

public class CompareOperatorVisitor(CompareOperator compareOperator, string compareValue) : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is PropertyInfo
            && TypeExtension.IsValidValuesForType(compareValue, node.Type))
        {
            return CreateCompare(node);
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        if (TypeExtension.IsValidValuesForType(compareValue, node.Type))
        {
            return CreateCompare(node);
        }

        return base.VisitConditional(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (TypeExtension.IsValidValuesForType(compareValue, node.Type))
        {
            return CreateCompare(node);
        }

        return base.VisitUnary(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (TypeExtension.IsValidValuesForType(compareValue, node.Type))
        {
            return CreateCompare(node);
        }

        return base.VisitBinary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == MethodAlias.Select && node.Method.DeclaringType == typeof(Enumerable))
        {
            Expression source = node.Arguments[0];
            LambdaExpression selector = (LambdaExpression)node.Arguments[^1];
            Expression transformedCondition = TransformCondition(selector.Body);

            return CallCore(EnumerableMethod(Any, 2, 1), source, (Expression)Expression.Lambda(transformedCondition, selector.Parameters[0]));
        }

        if (node.Method.Name == ToStringAlias)
        {
            return CreateCompare(node);
        }

        return base.VisitMethodCall(node);
    }

    private static MethodCallExpression CreateMethodCall(Expression node, string methodName, Expression constant)
    {
        MethodInfo methodInfo = node.Type.GetMethod(methodName, [node.Type])
            ?? throw new QueryBuilderException($"Method '{methodName}' not found on type '{node.Type}'");

        if (node.Type == typeof(string))
        {
            MethodInfo toLower = typeof(string).GetMethod(nameof(string.ToLower), [])
                ?? throw new QueryBuilderException($"Method '{methodName}' not found on type '{node.Type}'");
            return Expression.Call(
                Expression.Call(node, toLower),
                methodInfo,
                Expression.Call(constant, toLower)
            );
        }
        else
        {
            return Expression.Call(node, methodInfo, constant);
        }
    }

    private static object? ConvertValue(string text, Type type)
    {
        if (type.TryChangeType(text, out object? result))
        {
            return result;
        }
        else
        {
            throw new QueryBuilderException($"can not convert {text} to {type.FullName}");
        }
    }

    private Expression CreateCompare(Expression node)
    {
        Type targetType = node.Type.GetUnderlyingType();
        if (targetType != node.Type)
        {
            node = node.ToType(targetType!);
        }

        Expression constant = GetConstantExpression(targetType!);

        switch (compareOperator)
        {
            case CompareOperator.In:
            case CompareOperator.NotIn:
                MethodInfo? containsMethod = EnumerableMethod(Contains, 2, 1).MakeGenericMethod(targetType!);

                if (containsMethod == null)
                {
                    throw new InvalidOperationException($"Method '{containsMethod}' not found on collection of type '{targetType}'");
                }

                return compareOperator == CompareOperator.In
                    ? Expression.Call(null, containsMethod, constant, node)
                    : Expression.Not(Expression.Call(null, containsMethod, constant, node));

            case CompareOperator.Equal:
                return Expression.Equal(node, constant);

            case CompareOperator.NotEqual:
                return Expression.NotEqual(node, constant);

            case CompareOperator.LessThan:
                return Expression.LessThan(node, constant);

            case CompareOperator.GreaterThan:
                return Expression.GreaterThan(node, constant);

            case CompareOperator.GreaterThanOrEqual:
                return Expression.GreaterThanOrEqual(node, constant);

            case CompareOperator.LessThanOrEqual:
                return Expression.LessThanOrEqual(node, constant);

            case CompareOperator.Contains:
                return CreateMethodCall(node, Contains, constant);

            case CompareOperator.NotContains:
                return Expression.Not(CreateMethodCall(node, Contains, constant));

            case CompareOperator.StartsWith:
                return CreateMethodCall(node, StartsWith, constant);

            case CompareOperator.NotStartsWith:
                return Expression.Not(CreateMethodCall(node, StartsWith, constant));

            case CompareOperator.EndsWith:
                return CreateMethodCall(node, EndsWith, constant);

            case CompareOperator.NotEndsWith:
                return Expression.Not(CreateMethodCall(node, EndsWith, constant));

            default:
                return Expression.Constant(true);
        }
    }

    private ConstantExpression GetConstantExpression(Type targetType)
    {
        if (OperatorManager.IsInGroup(compareOperator))
        {
            string[] splitValues = compareValue.Split([SpecialCharacter.Comma], TrimSplitOptions);
            int arraySize = splitValues.Length;
            Array dynamicArray = Array.CreateInstance(targetType, arraySize);

            for (int i = 0; i < arraySize; i++)
            {
                dynamicArray.SetValue(ConvertValue(splitValues[i], targetType), i);
            }

            return Expression.Constant(dynamicArray);
        }
        else
        {
            return Expression.Constant(ConvertValue(compareValue, targetType));
        }
    }

    private Expression TransformCondition(Expression condition)
        => condition switch
        {
            MemberExpression or MethodCallExpression => CreateCompare(condition),
            ConditionalExpression conditional => VisitConditional(conditional),
            BinaryExpression binary => VisitBinary(binary),
            UnaryExpression unary => VisitUnary(unary),
            _ => throw new PopulateNotHandleException($"{nameof(CompareOperatorVisitor)}:{nameof(VisitMethodCall)} not supported for this expression type while {nameof(CreateCompare)} process"),
        };
}