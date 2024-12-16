using Populates.Builders;
using Populates.Public;
using System.Linq.Expressions;

namespace Populates.Internal.Queries;

internal class QueryExpression
{
    internal QueryExpression(LambdaExpression projection, IMetaPathBag pathMap)
    {
        PathMap = pathMap;
        Projection = projection;
        RootParameter = Projection.Parameters[0];
    }

    protected QueryExpression(QueryExpression queryExpression)
        : this(queryExpression.Projection, queryExpression.PathMap)
    {
    }

    internal ParameterExpression RootParameter { get; }

    internal LambdaExpression Projection { get; set; }

    internal IMetaPathBag PathMap { get; set; }

    /// <summary>
    /// Applies a transformation to the specified source using the provided selector function and the projection expression.
    /// </summary>
    /// <typeparam name="T">The type of the source.</typeparam>
    /// <param name="source">The source to be transformed.</param>
    /// <param name="select">The selector function that takes the source and projection expression and returns the transformed source.</param>
    /// <returns>Returns the transformed source of type <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// This method applies the given selector function to the specified source using the projection expression of the current <see cref="QueryExpression"/>.
    /// The transformed source is returned, allowing for further chaining and manipulation of the source.
    /// </remarks>
    internal T Chain<T>(T source, Func<T, LambdaExpression, T> select) => select(source, Projection);

    /// <summary>
    /// Prepares a query parameters specified for the current <see cref="QueryExpression"/> instance.
    /// </summary>
    /// <param name="queryParameters">The query parameters to be associated with the current <see cref="QueryExpression"/>.</param>
    /// <returns>Returns a new <see cref="QueryExpression"/> instance with the <see cref="Projection"/> adjusted for the specified query parameters.</returns>
    /// <remarks>
    /// This method creates a new <see cref="QueryExpression"/> instance by setting the parameters of the <see cref="Projection"/> to the provided <paramref name="queryParameters"/>. 
    /// It then calls the <see cref="Prepare(object, LambdaExpression)"/> method to set the parameters of <see cref="Projection"/>.
    /// </remarks>
    internal QueryExpression Prepare(object? queryParameters) => new(Prepare(queryParameters, Projection), PathMap);

    /// <summary>
    /// Adjusts the parameters of the specified <see cref="LambdaExpression"/> based on the provided query parameters.
    /// </summary>
    /// <param name="queryParameters">The query parameters to be used for setting the parameters of the <see cref="LambdaExpression"/>.</param>
    /// <param name="expression">The <see cref="LambdaExpression"/> to be adjusted.</param>
    /// <returns>Returns the adjusted <see cref="LambdaExpression"/> with the parameters set to the specified query parameters if provided; otherwise, returns the original <see cref="LambdaExpression"/>.</returns>
    /// <remarks>
    /// This static method checks if the <paramref name="queryParameters"/> is null. If it is not null, it sets the parameters of the provided <paramref name="expression"/> to the specified query parameters. 
    /// If <paramref name="queryParameters"/> is null, it returns the original <paramref name="expression"/>.
    /// </remarks>
    private static LambdaExpression Prepare(object? queryParameters, LambdaExpression expression) => queryParameters == null ? expression : expression.SetParameters(queryParameters);
}