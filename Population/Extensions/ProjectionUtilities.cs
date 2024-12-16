using AutoMapper;
using AutoMapper.Internal;
using Populates.Builders;
using Populates.Internal.Projection;
using System.Linq.Expressions;

namespace Populates.Extensions;

internal static class ProjectionUtilities
{
    /// <summary>
    /// Creates a new <see cref="ProjectionBuilder"/> instance using the specified <see cref="IConfigurationProvider"/>.
    /// </summary>
    /// <param name="configurationProvider">The <see cref="IConfigurationProvider"/> to use for configuring mappings.</param>
    /// <returns>A new <see cref="ProjectionBuilder"/> instance.</returns>
    /// <remarks>
    /// This method instantiates a new <see cref="ProjectionBuilder"/>, which is used for build projection expression based on the provided <see cref="IConfigurationProvider"/>.
    /// </remarks>
    internal static ProjectionBuilder MakeBuilder(this IConfigurationProvider configurationProvider) => new(configurationProvider);

    /// <summary>
    /// Finds the <see cref="TypeMap"/> for the given source and destination types
    /// using the provided <see cref="IConfigurationProvider"/>.
    /// </summary>
    /// <param name="configurationProvider">The configuration provider to use for the lookup.</param>
    /// <param name="sourceType">The type of the source object.</param>
    /// <param name="destinationType">The type of the destination object.</param>
    /// <returns>
    /// The <see cref="TypeMap"/> that maps from the source type to the destination type,
    /// or <c>null</c> if no mapping is found.
    /// </returns>
    internal static TypeMap FindTypeMap(this IConfigurationProvider configurationProvider, Type sourceType, Type destinationType) => configurationProvider.Internal().ResolveTypeMap(sourceType, destinationType);

    /// <summary>
    /// Evaluates and applies a custom source transformation to an expression, if defined in the property mapper.
    /// </summary>
    /// <param name="propertyMapper">The <see cref="PropertyMapper"/> containing the custom source logic to check.</param>
    /// <param name="instanceParameter">The <see cref="Expression"/> representing the instance parameter to which the custom source logic is applied.</param>
    /// <returns>
    /// The transformed <see cref="Expression"/> based on the custom source logic defined in the <paramref name="propertyMapper"/>.
    /// If no custom source is defined, returns the original <paramref name="instanceParameter"/>.
    /// </returns>
    /// <remarks>
    /// This method checks if the <paramref name="propertyMapper"/> has a custom source defined via the 
    /// <see cref="PropertyMapper.IncludedMember"/> property. If a custom source is provided, it replaces the parameter of the 
    /// <see cref="LambdaExpression"/> representing the custom source with the specified <paramref name="instanceParameter"/>.
    /// If no custom source is defined, the original instance parameter is returned unmodified.
    /// </remarks>
    internal static Expression CheckCustomSource(this PropertyMapper propertyMapper, Expression instanceParameter)
    {
        LambdaExpression? customSource = propertyMapper.IncludedMember?.ProjectToCustomSource;
        if (customSource == null)
        {
            return instanceParameter;
        }

        return customSource.ReplaceParameter(instanceParameter).Body;
    }

    /// <summary>
    /// Tries to handle an ignored property by providing a default expression if it is ignored.
    /// </summary>
    /// <param name="propertyMap">The PropertyMapper representing the property to handle.</param>
    /// <param name="defaultExpression">The default expression to use if the property is ignored.</param>
    /// <returns>True if the property is ignored; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the property represented by the <see cref="PropertyMapper"/> is ignored.
    /// If it is ignored, it assigns a default expression of null (for reference types) or the default value (for value types) to <paramref name="defaultExpression"/>.
    /// </remarks>
    internal static bool TryHandleIgnored(this PropertyMapper propertyMap, out Expression? defaultExpression)
    {
        defaultExpression = propertyMap.Ignored ? Expression.Constant(default, propertyMap.DestinationType) : default;
        return propertyMap.Ignored;
    }

    /// <summary>
    /// Increments and returns a unique parameter name based on the provided prefix and count.
    /// </summary>
    /// <param name="parameterPrefix">The prefix for the parameter name.</param>
    /// <param name="parameterCount">The reference to the parameter count, which is incremented with each call.</param>
    /// <returns>A unique parameter name composed of the prefix and the incremented count.</returns>
    internal static string IncrementParameter(string parameterPrefix, ref int parameterCount)
    {
        return parameterCount == default
         ? string.Concat(parameterPrefix, parameterCount++).Replace("0", string.Empty, StringComparison.OrdinalIgnoreCase)
         : string.Concat(parameterPrefix, parameterCount++);
    }

    /// <summary>
    /// Tries to retrieve the first element of a sequence that satisfies a specified condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence.</typeparam>
    /// <param name="source">An IEnumerable&lt;T&gt; to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="result">When this method returns, contains the first element that satisfies the condition, if found; otherwise, the default value for the type of the element.</param>
    /// <returns>True if an element that satisfies the condition is found; otherwise, false.</returns>
    /// <remarks>
    /// This method attempts to retrieve the first element of the sequence that satisfies the specified condition,
    /// storing the result in <paramref name="result"/>. It returns true if such an element is found, otherwise false.
    /// </remarks>
    internal static bool TryFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource? result)
        where TSource : class
    {
        result = predicate is null ? source.FirstOrDefault() : source.FirstOrDefault(predicate);
        return result != default(TSource);
    }

    /// <summary>
    /// Tries to retrieve the first element of a sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements in the sequence.</typeparam>
    /// <param name="source">An IEnumerable&lt;T&gt; to return an element from.</param>
    /// <param name="result">When this method returns, if found; otherwise, the default value for the type of the element.</param>
    /// <returns>True if an element is found; otherwise, false.</returns>
    /// <remarks>
    /// This method attempts to retrieve the first element of the sequence,
    /// storing the result in <paramref name="result"/>. It returns true if such an element is found, otherwise false.
    /// </remarks>
    internal static bool TryFirst<TSource>(this IEnumerable<TSource> source, out TSource? result)
        where TSource : class
    {
        result = source.FirstOrDefault();
        return result != default(TSource);
    }
}
