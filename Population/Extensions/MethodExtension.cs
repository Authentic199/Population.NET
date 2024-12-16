using Population.Builders;
using Population.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

namespace Population.Extensions;

internal static class MethodExtension
{
    internal static readonly MethodInfo SelectEnumerable = EnumerableMethod(MethodAlias.Select, parameterCount: 2, genericArgumentCount: 2);
    internal static readonly MethodInfo SelectManyEnumerable = EnumerableMethod(SelectMany, parameterCount: 2, genericArgumentCount: 2);

    private static readonly MethodInfo WhereQueryable = QueryableMethod(MethodAlias.Where, 2, 1);
    private static readonly MethodInfo SelectQueryable = typeof(Queryable).StaticGenericMethod(MethodAlias.Select, parameterCount: 2, genericArgumentCount: 2);

    /// <summary>
    /// Filters the elements of a sequence based on a specified condition.
    /// </summary>
    /// <param name="source">The IQueryable source sequence to filter.</param>
    /// <param name="conditionExpression">The lambda expression representing the condition.</param>
    /// <returns>
    /// An <see cref="IQueryable"/> that contains elements from the input sequence that satisfy the condition
    /// specified by the provided lambda expression.
    /// </returns>
    internal static IQueryable Where(this IQueryable source, LambdaExpression conditionExpression)
        => source.Provider.CreateQuery(
            Expression.Call(
                WhereQueryable.MakeGenericMethod(source.ElementType),
                source.Expression,
                conditionExpression
                )
            );

    /// <summary>
    /// Performs a Select operation on an <see cref="IQueryable"/>  source using the specified projection expression.
    /// </summary>
    /// <param name="source">The IQueryable source to apply the Select operation on.</param>
    /// <param name="expression">The projection expression used in the Select operation.</param>
    /// <returns>An <see cref="IQueryable"/> representing the result of the Select operation.</returns>
    internal static IQueryable Select(IQueryable source, LambdaExpression expression)
        => source.Provider.CreateQuery(
                Expression.Call(
                    SelectQueryable.MakeGenericMethod(source.ElementType, expression.ReturnType),
                    source.Expression,
                    Expression.Quote(expression)
                )
            );

    /// <summary>
    /// Generates a <see cref="MethodCallExpression"/> representing a sorting operation in the <see cref="Queryable"/> class.
    /// </summary>
    /// <param name="sortMethodName">The name of the sorting method.</param>
    /// <param name="memberAccess">The expression representing the member access for sorting.</param>
    /// <param name="lambda">The lambda expression used for sorting.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the sorting operation.</returns>
    internal static MethodCallExpression CallSort(string sortMethodName, Expression memberAccess, LambdaExpression lambda)
        => Expression.Call(
                QueryableMethod(sortMethodName, 2, 2).MakeGenericMethod(memberAccess.GetGenericArgument(), lambda.ReturnType),
                memberAccess,
                Expression.Quote(lambda)
            );

    /// <summary>
    /// Retrieves the <see cref="MethodInfo"/> for a specified static generic method in the <see cref="Queryable"/> class.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="parameterCount">The number of parameters the method should have.</param>
    /// <param name="genericArgumentCount">The number of generic arguments the method should have.</param>
    /// <returns>The <see cref="MethodInfo"/> for the specified static generic method.</returns>
    internal static MethodInfo QueryableMethod(string methodName, int parameterCount, int genericArgumentCount)
        => typeof(Queryable).StaticGenericMethod(methodName, parameterCount, genericArgumentCount);

    /// <summary>
    /// Retrieves the <see cref="MethodInfo"/> for a specified static generic method in the <see cref="Enumerable"/> class.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="parameterCount">The number of parameters the method should have.</param>
    /// <param name="genericArgumentCount">The number of generic arguments the method should have.</param>
    /// <returns>The <see cref="MethodInfo"/> for the specified static generic method.</returns>
    internal static MethodInfo EnumerableMethod(string methodName, int parameterCount, int genericArgumentCount)
        => typeof(Enumerable).StaticGenericMethod(methodName, parameterCount, genericArgumentCount);

    /// <summary>
    /// Retrieves the corresponding method information for an <see cref="Enumerable"/> method.
    /// </summary>
    /// <param name="baseMethod">The base method to retrieve information for.</param>
    /// <returns>The method information for the <see cref="Enumerable"/> method.</returns>
    /// <remarks>
    /// This method fetches the method information for an <see cref="Enumerable"/> method by extracting its name, parameter count, and generic argument count
    /// from the provided base method and then calls the overload with those extracted values.
    /// </remarks>
    internal static MethodInfo EnumerableMethod(MethodInfo baseMethod)
        => EnumerableMethod(baseMethod.Name, baseMethod.GetParameters().Length, baseMethod.GetGenericArguments().Length);

    /// <summary>
    /// Creates a <see cref="MethodCallExpression"/> that represents a call to the LINQ <c>ToList</c> method.
    /// </summary>
    /// <param name="expression">
    /// The <see cref="Expression"/> to be converted to a list using the LINQ <c>ToList</c> method.
    /// </param>
    /// <returns>
    /// A <see cref="MethodCallExpression"/> representing the call to the LINQ <c>ToList</c> method on the specified <paramref name="expression"/>.
    /// </returns>
    /// <remarks>
    /// This method internally uses <c>CallCore</c> to generate the <see cref="MethodCallExpression"/> and resolves
    /// the <c>ToList</c> method using <c>EnumerableMethod</c> with appropriate parameter and generic argument counts.
    /// </remarks>
    internal static MethodCallExpression CallToList(Expression expression)
        => CallCore(EnumerableMethod(ToList, parameterCount: 1, genericArgumentCount: 1), expression);

    /// <summary>
    /// Creates a <see cref="MethodCallExpression"/> that represents a call to the LINQ <c>ToArray</c> method.
    /// </summary>
    /// <param name="expression">The <see cref="Expression"/> to be converted to an array using the LINQ <c>ToArray</c> method.</param>
    /// <returns>
    /// A <see cref="MethodCallExpression"/> representing the invocation of the LINQ <c>ToArray</c> method on the specified <paramref name="expression"/>.
    /// </returns>
    /// <remarks>
    /// This method uses <c>CallCore</c> to generate a <see cref="MethodCallExpression"/>, resolving the <c>ToArray</c> method 
    /// with the help of <c>EnumerableMethod</c>, configured with the required parameter and generic argument counts.
    /// </remarks>
    internal static MethodCallExpression CallToArray(Expression expression)
        => CallCore(EnumerableMethod(ToArray, parameterCount: 1, genericArgumentCount: 1), expression);

    /// <summary>
    /// Generates a <see cref="MethodCallExpression"/> for the <see cref="SelectEnumerable"/> method with the provided member access and lambda expression.
    /// </summary>
    /// <param name="memberAccess">The source expression to apply the Select method.</param>
    /// <param name="lambda">The lambda expression representing the transformation to apply.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the `Select` method in the <see cref="Enumerable"/> class.</returns>
    /// <remarks>
    /// This method generates a <see cref="MethodCallExpression"/> that applies the `Select` method on the provided source expression,
    /// applying the provided lambda expression to each element to produce the resulting sequence.
    /// </remarks>
    internal static MethodCallExpression CallSelect(Expression memberAccess, LambdaExpression lambda)
        => CallCore(SelectEnumerable, memberAccess, lambda);

    /// <summary>
    /// Generates a <see cref="MethodCallExpression"/> that represents a call to the LINQ <c>Select</c> method followed by the <c>ToList</c> method.
    /// </summary>
    /// <param name="memberAccess">The source <see cref="Expression"/> to apply the <c>Select</c> operation.</param>
    /// <param name="lambda">The <see cref="LambdaExpression"/> defining the transformation to apply in the <c>Select</c> operation.</param>
    /// <returns>
    /// A <see cref="MethodCallExpression"/> representing the invocation of the <c>ToList</c> method on the result of the <c>Select</c> operation.
    /// </returns>
    /// <remarks>
    /// This method creates a <see cref="MethodCallExpression"/> that chains the LINQ <c>Select</c> and <c>ToList</c> methods, 
    /// allowing the transformation defined by the <paramref name="lambda"/> to be applied to each element in the sequence 
    /// and then converting the result into a list.
    /// </remarks>
    internal static MethodCallExpression CallToListSelect(Expression memberAccess, LambdaExpression lambda)
        => CallToList(CallSelect(memberAccess, lambda));

    /// <summary>
    /// Generates a <see cref="MethodCallExpression"/> representing a filtering operation using the `Where` method.
    /// </summary>
    /// <param name="memberAccess">The source expression to filter using the Where method.</param>
    /// <param name="conditionExpression">The condition expression for the Where method.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the `Where` method in the <see cref="Enumerable"/> class.</returns>
    /// <remarks>
    /// This method generates a <see cref="MethodCallExpression"/> that applies the `Where` method on the provided source expression,
    /// filtering elements based on the provided condition expression.
    /// </remarks>
    internal static MethodCallExpression CallWhere(Expression memberAccess, LambdaExpression conditionExpression)
        => CallCore(EnumerableMethod(MethodAlias.Where, parameterCount: 2, genericArgumentCount: 1), memberAccess, (Expression)conditionExpression);

    /// <summary>
    /// Generates a <see cref="MethodCallExpression"/> representing a combination of `Where` and `Select` methods in the <see cref="Enumerable"/> class.
    /// </summary>
    /// <param name="memberAccess">The source expression to filter using the Where method.</param>
    /// <param name="conditionExpression">The condition expression for the Where method.</param>
    /// <param name="initExpression">The projection expression for the Select method.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the combination of `Where` and `Select` methods in the <see cref="Enumerable"/> class.</returns>
    /// <remarks>
    /// This method combines the `Where` method with the provided condition expression to filter the source expression,
    /// and then applies the `Select` method with the given projection expression to transform the filtered sequence.
    /// </remarks>
    internal static MethodCallExpression CallWhereSelect(Expression memberAccess, LambdaExpression conditionExpression, LambdaExpression initExpression)
        => CallSelect(CallWhere(memberAccess, conditionExpression), initExpression);

    /// <summary>
    /// Generates a <see cref="MethodCallExpression"/> representing the `SelectMany` method in the <see cref="Enumerable"/> class.
    /// </summary>
    /// <param name="memberAccess">The source expression to apply the SelectMany method on.</param>
    /// <param name="lambda">The lambda expression representing the transformation to apply.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the `SelectMany` method in the <see cref="Enumerable"/> class.</returns>
    /// <remarks>
    /// This method is used to generate a <see cref="MethodCallExpression"/> that applies the `SelectMany` method on the provided source expression,
    /// which projects each element of a sequence to an IEnumerable&lt;TSource&gt; and flattens the resulting sequences into one sequence, according to the given lambda expression.
    /// </remarks>
    internal static MethodCallExpression CallSelectMany(Expression memberAccess, LambdaExpression lambda)
    {
        CheckExpressionType(lambda, nameof(CallSelectMany));
        return CallCore(SelectManyEnumerable, memberAccess, lambda);
    }

    /// <summary>
    /// Constructs a <see cref="MethodCallExpression"/>  for a specified method, member access expression, and lambda expression.
    /// </summary>
    /// <param name="method">The MethodInfo representing the method to be called in <see cref="Enumerable"/> class.</param>
    /// <param name="memberAccess">The Expression representing the member access or target object for the method call.</param>
    /// <param name="lambda">The LambdaExpression representing the arguments or parameters for the method call.</param>
    /// <returns>A <see cref="MethodCallExpression"/>  representing the specified method call.</returns>
    /// <remarks>
    /// This method performs pre-call checks to ensure that the provided method is declared in the Enumerable class
    /// and that the member access expression and lambda expression conform to the expected types.
    /// then constructs a <see cref="MethodCallExpression"/> by resolving the generic method and adjusting the lambda expression if necessary.
    /// </remarks>
    /// <exception cref="PopulateNotHandleException">
    /// if the provided method isn't declared in the Enumerable or the member access expression and lambda expression invalid type
    /// </exception>
    internal static MethodCallExpression CallCore(MethodInfo method, Expression memberAccess, LambdaExpression lambda)
    {
        CheckEnumerableMethod(method, nameof(CallCore));
        CheckExpressionType(memberAccess, nameof(CallCore));

        MethodInfo genericMethod = MakeGenericMethod();
        ParameterInfo[] parameterInfos = genericMethod.GetParameters();
        if (parameterInfos.Length > 1)
        {
            ParameterInfo secondParam = parameterInfos[1];

            // When the method is SelectMany, the second parameter of the method is of type IEnumerable, but the lambda type is ICollection so the Type must be changed
            // Example: x.Translations.SelectMany(x1 => x1.ActualImages) : x1.ActualImages is ICollection -> must change to IEnumerable
            if (lambda.Type != secondParam.ParameterType)
            {
                lambda = lambda.ToEnumerableType(secondParam.Member.ResolveMemberType());
            }
        }

        return Expression.Call(genericMethod, memberAccess, lambda);
        MethodInfo MakeGenericMethod()
            => method.GetGenericArguments().Length switch
            {
                1 => method.MakeGenericMethod(GetExpressionElementType(lambda)),
                2 => method.MakeGenericMethod(memberAccess.GetGenericArgument(), GetExpressionElementType(lambda)),
                _ => throw new PopulateNotHandleException(),
            };
    }

    /// <summary>
    /// Constructs a <see cref="MethodCallExpression"/> for a specified method and member access expression with an additional expression.
    /// </summary>
    /// <param name="method">The MethodInfo representing the method to be called in <see cref="Enumerable"/> class.</param>
    /// <param name="memberAccess">The Expression representing the member access or target object for the method call.</param>
    /// <param name="expression">The additional Expression representing arguments or parameters for the method call.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the specified method call.</returns>
    /// <remarks>
    /// This method performs pre-call checks to ensure that the provided method is declared in the Enumerable class
    /// and that the member access expression conforms to the expected type,
    /// then constructs a <see cref="MethodCallExpression"/> by resolving the generic method and using the provided expressions as arguments.
    /// </remarks>
    /// <exception cref="PopulateNotHandleException">
    /// if the provided method isn't declared in the Enumerable or the member access expression and lambda expression invalid type
    /// </exception>
    internal static MethodCallExpression CallCore(MethodInfo method, Expression memberAccess, Expression expression)
    {
        CheckEnumerableMethod(method, nameof(CallCore));
        CheckExpressionType(memberAccess, nameof(CallCore));

        return Expression.Call(MakeGenericMethod(), memberAccess, expression);
        MethodInfo MakeGenericMethod() => method.MakeGenericMethod(GetExpressionElementType(memberAccess));
    }

    /// <summary>
    /// Constructs a <see cref="MethodCallExpression"/> for a specified method and member access expression without an additional expression.
    /// </summary>
    /// <param name="method">The MethodInfo representing the method to be called in <see cref="Enumerable"/> class.</param>
    /// <param name="memberAccess">The Expression representing the member access or target object for the method call.</param>
    /// <returns>A <see cref="MethodCallExpression"/> representing the specified method call.</returns>
    /// <remarks>
    /// This method performs pre-call checks to ensure that the provided method is declared in the Enumerable class
    /// and that the member access expression conforms to the expected type,
    /// then constructs a <see cref="MethodCallExpression"/> by resolving the generic method and using the provided expression as an argument.
    /// </remarks>
    /// <exception cref="PopulateNotHandleException">
    /// if the provided method isn't declared in the Enumerable or the member access expression and lambda expression invalid type
    /// </exception>
    internal static MethodCallExpression CallCore(MethodInfo method, Expression memberAccess)
    {
        CheckEnumerableMethod(method, nameof(CallCore));
        CheckExpressionType(memberAccess, nameof(CallCore));

        return Expression.Call(MakeGenericMethod(), memberAccess);
        MethodInfo MakeGenericMethod() => method.MakeGenericMethod(GetExpressionElementType(memberAccess));
    }

    /// <summary>
    /// Retrieves the first generic argument type of the expression whose return type is a collection.
    /// </summary>
    /// <param name="expression">The expression whose return type is a collection.</param>
    /// <returns>The first generic type argument of the expression.</returns>
    /// <exception cref="PopulateNotHandleException">
    /// Throw when expression type is not a generic collection
    /// </exception>
    internal static Type GetGenericArgument(this Expression expression)
    {
        CheckExpressionType(expression, nameof(GetGenericArgument));
        Type type = expression is LambdaExpression lambda ? lambda.Body.Type : expression.Type;
        return type.GetCollectionElementType();
    }

    /// <summary>
    /// Retrieves the MethodInfo for a specified static generic method in a given type.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="parameterCount">The number of parameters the method should have.</param>
    /// <param name="genericArgumentCount">The number of generic arguments the method should have.</param>
    /// <returns>The MethodInfo for the specified static generic method.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thow when not found method matchs <paramref name="methodName"/> and <paramref name="parameterCount"/>
    /// </exception>
    private static MethodInfo StaticGenericMethod(this Type type, string methodName, int parameterCount, int genericArgumentCount)
    {
        foreach (MethodInfo foundMethod in type.GetMember(methodName, MemberTypes.Method, StaticFlags).OfType<MethodInfo>())
        {
            if (foundMethod.IsGenericMethodDefinition &&
                foundMethod.GetParameters().Length == parameterCount &&
                foundMethod.GetGenericArguments().Length == genericArgumentCount)
            {
                return foundMethod;
            }
        }

        throw new PopulateNotHandleException($"{type.Name} can not define static generic method {methodName} with {parameterCount} parameters");
    }

    /// <summary>
    /// Gets the generic type of an Expression, considering it may be a LambdaExpression.
    /// </summary>
    /// <param name="expression">The Expression for which to determine the generic type.</param>
    /// <returns>
    /// The generic type of the Expression, or the type of the Expression itself if it is not a collection type.
    /// </returns>
    private static Type GetExpressionElementType(Expression expression)
    {
        Type type = expression is LambdaExpression lambda ? lambda.Body.Type : expression.Type;
        if (!type.IsCollection())
        {
            return type;
        }

        return type.GetCollectionElementType();
    }

    /// <summary>
    /// Checks the type of the specified expression to ensure it is a collection type.
    /// </summary>
    /// <param name="expression">The Expression to be checked.</param>
    /// <param name="functionName">The name of the function or method calling this check.</param>
    /// <remarks>Throws a <see cref="PopulateNotHandleException"/> if the type of the expression is not a collection type.</remarks>
    /// <exception cref="PopulateNotHandleException">
    /// Thow if the type of the expression is not a collection type./>
    /// </exception>
    private static void CheckExpressionType(Expression expression, string functionName)
    {
        Type type = expression is LambdaExpression lambda ? lambda.Body.Type : expression.Type;
        if (!type.IsCollection())
        {
            throw new PopulateNotHandleException($"`{nameof(MethodExtension)}`:`{functionName}` the type of expression must be collection");
        }
    }

    /// <summary>
    /// Checks if the specified MethodInfo is declared in the Enumerable class.
    /// </summary>
    /// <param name="methodInfo">The MethodInfo to be checked.</param>
    /// <param name="functionName">The name of the function or method calling this check.</param>
    /// <remarks>Throws a <see cref="PopulateNotHandleException"/> if the method info is not declared in Enumerable.</remarks>
    /// <exception cref="PopulateNotHandleException">
    /// Thow if the method info is not declared in Enumerable./>
    /// </exception>
    private static void CheckEnumerableMethod(MethodInfo methodInfo, string functionName)
    {
        if (methodInfo.DeclaringType != typeof(Enumerable))
        {
            throw new PopulateNotHandleException($"`{nameof(MethodExtension)}`:`{functionName}` method info not declare in Enumerable");
        }
    }
}
