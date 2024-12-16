using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Population.Visitors;

/// <summary>
/// A visitor that modifies lambda expressions to work with a specified enumerable type.
/// </summary>
internal class EnumerableParameterVisitor : ExpressionVisitor
{
    private readonly Type enumerableType;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableParameterVisitor"/> class with the specified enumerable type.
    /// </summary>
    /// <param name="enumerableType">The target type that implements <see cref="IEnumerable"/> to which lambda expressions will be adjusted.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided <paramref name="enumerableType"/> is null or does not implement <see cref="IEnumerable"/>.
    /// </exception>
    internal EnumerableParameterVisitor(Type enumerableType)
    {
        if (enumerableType == null || !typeof(IEnumerable).IsAssignableFrom(enumerableType))
        {
            throw new ArgumentException("enumerableType must be a valid type implementing IEnumerable.", nameof(enumerableType));
        }

        this.enumerableType = enumerableType;
    }

    /// <summary>
    /// Visits the specified expression and modifies it if necessary to match the target enumerable type.
    /// </summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>
    /// The modified expression if the input is a lambda expression with a return type that does not match the target enumerable type;
    /// otherwise, returns the original expression.
    /// </returns>
    /// <remarks>
    /// If the node is a <see cref="LambdaExpression"/> and its return type does not match the target enumerable type,
    /// this method adjusts the lambda expression to use the specified enumerable type.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node is LambdaExpression lambdaExpression && lambdaExpression.ReturnType != enumerableType)
        {
            return ChangeTypeLambda(lambdaExpression);
        }

        return base.Visit(node);
    }

    /// <summary>
    /// Changes the return type of a lambda expression to match the target enumerable type.
    /// </summary>
    /// <param name="lambda">The lambda expression to modify.</param>
    /// <returns>A new <see cref="LambdaExpression"/> with its return type adjusted to the target enumerable type.</returns>
    /// <remarks>
    /// This method creates a new lambda expression with the same parameters and body as the input,
    /// but modifies its delegate type to include the target enumerable type.
    /// </remarks>
    private LambdaExpression ChangeTypeLambda(LambdaExpression lambda)
    {
        ParameterExpression parameter = lambda.Parameters[0];
        Type delegateType = Expression.GetFuncType(parameter.Type, enumerableType);
        return Expression.Lambda(delegateType, lambda.Body, parameter);
    }
}
