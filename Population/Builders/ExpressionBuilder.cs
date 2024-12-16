using Population.Definations;
using Population.Exceptions;
using Population.Extensions;
using Population.Internal.Projection;
using Population.Visitors;
using System.Linq.Expressions;
using System.Reflection;
using DictionaryParameters = System.Collections.Generic.IDictionary<string, object>;

namespace Population.Builders;

internal static class ExpressionBuilder
{
    internal static readonly Expression Null = Expression.Default(typeof(object));

    /// <summary>
    /// Converts the type of specified expression to the specified type.
    /// </summary>
    /// <param name="originalExpression">The expression to be converted.</param>
    /// <param name="type">The type to which the expression should be converted.</param>
    /// <returns>
    /// Returns the <see cref="Expression"/> converted to the specified type, or the original expression if it already matches the type.
    /// </returns>
    /// <remarks>
    /// This method checks if the type of the provided expression matches the specified type. If they match, it returns
    /// the original expression. Otherwise, it converts the expression to the specified type using <see cref="Expression.Convert(Expression, Type)"/>.
    /// </remarks>
    internal static Expression ToType(this Expression originalExpression, Type type)
        => originalExpression.Type == type || originalExpression.Type.IsCollection()
            ? originalExpression
            : Expression.Convert(originalExpression, type);

    /// <summary>
    /// Converts a <seealso cref="LambdaExpression"/> returning a specific type to a lambda expression returning an object.
    /// </summary>
    /// <typeparam name="TInput">The input type of the lambda expression.</typeparam>
    /// <param name="lambdaExpression">The lambda expression to be converted.</param>
    /// <returns>
    /// Returns a <seealso cref="Expression{Func}"/> that takes an input of type <typeparamref name="TInput"/> and returns an object.
    /// </returns>
    /// <remarks>
    /// This method takes a lambda expression as input and creates a new lambda expression that returns an object.
    /// It achieves this by converting the body of the input lambda expression to an object type using
    /// <see cref="Expression.Convert(Expression, Type)"/> and creating a new lambda expression with the converted body.
    /// </remarks>
    internal static Expression<Func<TInput, object>> ToObject<TInput>(this LambdaExpression lambdaExpression)
        => Expression.Lambda<Func<TInput, object>>(Expression.Convert(lambdaExpression.Body, typeof(object)), lambdaExpression.Parameters);

    /// <summary>
    /// Converts a <seealso cref="LambdaExpression"/> to work with a specified <paramref name="enumerableType"/>.
    /// </summary>
    /// <param name="expression">The original lambda expression to be converted.</param>
    /// <param name="enumerableType">The type to which the lambda expression's parameters should be converted.</param>
    /// <returns>
    /// Returns a new <seealso cref="LambdaExpression"/> modified to work with the specified enumerable type.
    /// </returns>
    /// <exception cref="PopulateNotHandleException">
    /// Thrown when an error occurs while attempting to convert the <paramref name="expression"/> to the specified <paramref name="enumerableType"/>.
    /// </exception>
    /// <remarks>
    /// This method adjusts the parameters of the input <seealso cref="LambdaExpression"/> to work with the specified <paramref name="enumerableType"/>.
    /// It achieves this by visiting the expression with an <see cref="EnumerableParameterVisitor"/>, which updates
    /// the lambda expression's parameters accordingly to match the specified enumerable type.
    /// </remarks>
    internal static LambdaExpression ToEnumerableType(this LambdaExpression expression, Type enumerableType)
    {
        try
        {
            return (LambdaExpression)new EnumerableParameterVisitor(enumerableType).Visit(expression);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new PopulateNotHandleException(VisitErrorMessage(nameof(ToEnumerableType)), ex);
        }
    }

    /// <summary>
    /// Replaces the parameter of a <seealso cref="LambdaExpression"/> with a new expression.
    /// </summary>
    /// <param name="lambdaExpression">The original lambda expression whose parameter needs to be replaced.</param>
    /// <param name="newParameter">The new expression to be used as the replacement parameter.</param>
    /// <returns>
    /// Returns a new <seealso cref="LambdaExpression"/> with the parameter replaced by the specified expression.
    /// </returns>
    /// <remarks>
    /// This method replaces the parameter of the input <seealso cref="LambdaExpression"/> with the specified new expression.
    /// It achieves this by visiting the <seealso cref="LambdaExpression"/> with a <see cref="ReplaceParameterVisitor"/>, which replaces
    /// the original parameter with the new expression. The resulting <seealso cref="LambdaExpression"/> contains the updated parameter.
    /// </remarks>
    internal static LambdaExpression ReplaceParameter(this LambdaExpression lambdaExpression, Expression newParameter)
    {
        try
        {
            return (LambdaExpression)new ReplaceParameterVisitor(lambdaExpression.Parameters[0], newParameter).Visit(lambdaExpression);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new PopulateNotHandleException(VisitErrorMessage(nameof(ReplaceParameter)), ex);
        }
    }
    /// <summary>
    /// Replaces the member accessed within a "Select" or "SelectMany" method call in the provided expression.
    /// </summary>
    /// <param name="expression">The original expression to process.</param>
    /// <param name="replaceMember">The expression representing the replacement member to use within the "Select" or "SelectMany" method.</param>
    /// <returns>
    /// Returns a new <seealso cref="Expression"/> where the member accessed in "Select" or "SelectMany" calls has been replaced
    /// with the specified <paramref name="replaceMember"/>. If the input expression does not contain a "Select" or "SelectMany" method call, 
    /// the original expression is returned unchanged.
    /// </returns>
    /// <exception cref="PopulateNotHandleException">
    /// Thrown when an error occurs while processing the <paramref name="expression"/> to replace the member in the method call.
    /// </exception>
    /// <remarks>
    /// This method identifies "Select" and "SelectMany" method calls in the provided <paramref name="expression"/> and uses 
    /// a <see cref="SelectMethodMemberVisitor"/> to replace the member being accessed with the specified <paramref name="replaceMember"/>.
    /// </remarks>
    internal static Expression ReplaceSelectMethodMember(this Expression expression, Expression replaceMember)
    {
        try
        {
            if (expression is not MethodCallExpression methodCallExpression
            || (
                !methodCallExpression.Method.Name.EqualSelect()
                && !methodCallExpression.Method.Name.EqualSelectMany()
               )
            )
            {
                return expression;
            }

            return new SelectMethodMemberVisitor(replaceMember).Visit(methodCallExpression);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new PopulateNotHandleException(VisitErrorMessage(nameof(ReplaceSelectMethodMember)), ex);
        }
    }

    /// <summary>
    /// Sets parameters for a <seealso cref="LambdaExpression"/> based on the provided query parameters.
    /// </summary>
    /// <param name="lambdaExpression">The original <seealso cref="LambdaExpression"/> whose parameters are to be set.</param>
    /// <param name="queryParameters">The query parameters to be used for setting the <seealso cref="LambdaExpression"/>'s parameters.</param>
    /// <returns>
    /// Returns a new <seealso cref="LambdaExpression"/> with parameters set according to the provided query parameters.
    /// </returns>
    /// <remarks>
    /// This method sets parameters for the input <seealso cref="LambdaExpression"/> based on the provided query parameters.
    /// It determines the type of query parameters (whether dictionary-based or object-based) and utilizes
    /// the appropriate visitor to set the <seealso cref="LambdaExpression"/>'s parameters accordingly. The resulting lambda
    /// expression contains parameters set according to the provided query parameters.
    /// </remarks>
    internal static LambdaExpression SetParameters(this LambdaExpression lambdaExpression, object queryParameters)
    {
        QueryParameterVisitor visitor = queryParameters is DictionaryParameters dictionary
            ? new DictionaryQueryParameterVisitor(dictionary)
            : new ObjectQueryParameterVisitor(queryParameters);

        return (LambdaExpression)visitor.Visit(lambdaExpression);
    }

    /// <summary>
    /// Visits and processes a member access expression within a <see cref="ProjectionRequest"/>.
    /// </summary>
    /// <param name="memberAccess">The member access expression to be visited.</param>
    /// <param name="projectionRequest">The <see cref="ProjectionRequest"/> containing information about the projection.</param>
    /// <param name="memberInit">A function to initialize a member of a type in the member access.</param>
    /// <returns>
    /// Returns the visited expression after processing.
    /// </returns>
    /// <remarks>
    /// This method visits and processes a member access expression within the context of a <see cref="ProjectionRequest"/>.
    /// It utilizes a <see cref="MemberVisitor"/> to perform the visitation and processing. If an error occurs
    /// during the process, it wraps the exception in a <see cref="PopulateNotHandleException"/> and throws it.
    /// </remarks>
    internal static Expression VisitMember(this Expression memberAccess, ProjectionRequest projectionRequest, Func<ProjectionRequest, Expression, MemberInitExpression> memberInit)
    {
        try
        {
            return new MemberVisitor(projectionRequest, memberInit).Visit(memberAccess);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new PopulateNotHandleException(VisitErrorMessage(nameof(VisitMember)), ex);
        }
    }

    /// <summary>
    /// Converts the type of an expression to <see cref="string"/> if applicable, based on the provided base member's type.
    /// </summary>
    /// <param name="expression">The <see cref="Expression"/> to be converted.</param>
    /// <param name="baseMember">The <see cref="MemberInfo"/> representing the base member used to determine the target type.</param>
    /// <returns>
    /// A new <see cref="Expression"/> where the type is converted to <see cref="string"/> if required, 
    /// or the original expression if no conversion is necessary.
    /// </returns>
    /// <exception cref="PopulateNotHandleException">
    /// Thrown if an error occurs during the conversion process.
    /// </exception>
    /// <remarks>
    /// This method applies specific rules to determine whether the expression should be converted to <see cref="string"/>:
    /// <list type="bullet">
    /// <item>If the base member resolves to a type that is already <see cref="string"/>, no conversion is performed.</item>
    /// <item>If the expression is a <see cref="MemberExpression"/> for a property that is not a string or collection type, 
    /// it uses <see cref="StringConvertVisitor"/> to perform the conversion.</item>
    /// <item>If the expression is a unary, binary, or conditional expression, the conversion is delegated to <see cref="StringConvertVisitor"/>.</item>
    /// <item>If the expression is a method call representing a "Select" operation, it inspects the lambda body for conversion 
    /// using the inner member's type.</item>
    /// </list>
    /// The method ensures robust error handling and logs exceptions before throwing a custom <see cref="PopulateNotHandleException"/>.
    /// </remarks>
    internal static Expression ConvertStringType(this Expression expression, MemberInfo baseMember)
    {
        if (baseMember.ResolveMemberType() == typeof(string))
        {
            return expression;
        }

        try
        {
            if (expression is MemberExpression memberExpression
                && memberExpression.Member is PropertyInfo info
                && info.PropertyType != typeof(string)
                && !info.PropertyType.IsCollection())
            {
                return new StringConvertVisitor(info).Visit(expression);
            }

            if (expression is UnaryExpression or BinaryExpression or ConditionalExpression)
            {
                return new StringConvertVisitor(baseMember).Visit(expression);
            }

            if (expression is MethodCallExpression methodCallExpression
                && methodCallExpression.Method.Name == MethodAlias.Select
                && methodCallExpression.Arguments.LastOrDefault() is LambdaExpression innerLambdaExpression)
            {
                return innerLambdaExpression.Body is MemberExpression innerMemberExpression
                    ? new StringConvertVisitor(innerMemberExpression.Member).Visit(methodCallExpression)
                    : new StringConvertVisitor(baseMember).Visit(methodCallExpression);
            }

            return expression;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new PopulateNotHandleException(VisitErrorMessage(nameof(ConvertStringType)), ex);
        }
    }

    /// <summary>
    /// Resolves a member access expression based on the provided parameters.
    /// </summary>
    /// <param name="parameter">The parameter expression representing the object containing the member.</param>
    /// <param name="expression">The lambda expression defining the member access, if available.</param>
    /// <param name="propertyName">The name of the property to access.</param>
    /// <returns>
    /// Returns an <see cref="Expression"/> representing the member access.
    /// </returns>
    /// <remarks>
    /// This method resolves a member access expression based on the provided parameters.
    /// If the <paramref name="expression"/> is null, it constructs a member access expression
    /// using the provided <paramref name="parameter"/> and <paramref name="propertyName"/>.
    /// If the <paramref name="expression"/> is not null, it replaces the parameter in the expression
    /// with the provided <paramref name="parameter"/> and returns the body of the modified expression.
    /// </remarks>
    internal static Expression ResolveMemberAccess(Expression parameter, LambdaExpression? expression, string propertyName)
        => expression is null
        ? Expression.PropertyOrField(parameter, propertyName)
        : expression.ReplaceParameter(parameter).Body;

    /// <summary>
    /// Creates a conditional expression that checks if the provided expression is null and returns one of two expressions based on the result.
    /// </summary>
    /// <param name="expression">The expression to check for null.</param>
    /// <param name="ifTrue">The expression to return if the provided expression is not null.</param>
    /// <param name="orElse">The expression to return if the provided expression is null.</param>
    /// <returns>
    /// A conditional <see cref="Expression"/> that returns <paramref name="ifTrue"/> if the provided expression is not null,
    /// and <paramref name="orElse"/> if the provided expression is null.
    /// </returns>
    /// <remarks>
    /// This method handles both value types and reference types. For value types, it further checks if the type is nullable.
    /// If it is, it checks the <c>HasValue</c> property to determine if the value is null. For reference types, it uses
    /// <see cref="Expression.ReferenceEqual"/> to check for null. Note that for nullable value types, if the expression is null,
    /// it returns <paramref name="orElse"/>; otherwise, it returns <paramref name="ifTrue"/>.
    /// </remarks>
    internal static Expression IfNullElse(this Expression expression, Expression ifTrue, Expression orElse)
    {
        if (expression.Type.IsValueType)
        {
            return expression.Type.IsNullableType()
                ? Expression.Condition(
                        Expression.Property(expression, PopulateConstant.HasValue), // Example: IntValue.HasValue, DateTimeValue.HasValue
                        orElse,
                        ifTrue
                    )
                : orElse;
        }

        return Expression.Condition(Expression.ReferenceEqual(expression, Null), ifTrue, orElse);
    }

    private static string VisitErrorMessage(string methodName)
        => $"`{nameof(ExpressionBuilder)}`:`{methodName}` an error occur in Visit expression process";
}
