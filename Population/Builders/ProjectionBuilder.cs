using AutoMapper;
using Infrastructure.Facades.Populates.Definations;
using Infrastructure.Facades.Populates.Exceptions;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Internal.PrimitiveMappers;
using Infrastructure.Facades.Populates.Internal.Projection;
using Infrastructure.Facades.Populates.Internal.Queries;
using Infrastructure.Facades.Populates.Public;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;
using System.Reflection;
using static Infrastructure.Facades.Populates.Definations.PopulateOptions;

namespace Infrastructure.Facades.Populates.Builders;

internal class ProjectionBuilder
{
    private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions()
    {
        SizeLimit = 1024,
    });

    private readonly IConfigurationProvider configurationProvider;
    private readonly MetaPathBag pathMaps = [];
    private PopulateAnalyzer populateAnalyzer = default!;
    private int parameterCount;

    internal ProjectionBuilder(IConfigurationProvider configurationProvider)
    {
        this.configurationProvider = configurationProvider;
    }

    /// <summary>
    /// Constructs and caches the query expression for projecting data from a source type to a destination type.
    /// </summary>
    /// <param name="source">The source data being projected.</param>
    /// <param name="destinationType">The type to which the data is projected.</param>
    /// <param name="populates">A collection of populate keys specifying which fields to include in the projection.</param>
    /// <param name="queryParameters">Optional query parameters to customize the projection.</param>
    /// <returns>
    /// A <see cref="QueryExpression"/> representing the cached or newly constructed projection.
    /// </returns>
    /// <remarks>
    /// This method creates a <see cref="ProjectionRequest"/> for the specified types and populate keys, initializes
    /// a <see cref="PopulateAnalyzer"/>, and attempts to retrieve a cached projection expression.
    /// If no cached expression exists, it constructs a new one and stores it in the cache with a sliding expiration of 7 days.
    /// </remarks>
    internal QueryExpression GetProjection(IQueryable source, Type destinationType, IEnumerable<string> populates, object? queryParameters)
    {
        populateAnalyzer = PopulateAnalyzer.Init(destinationType, populates);
        ProjectionRequest projectionRequest = new(configurationProvider, source.ElementType, destinationType, populateAnalyzer, MemberPath.Root);

        return Cache.GetOrCreate(
            projectionRequest,
            entry =>
            {
                entry.Size = 1;
                entry.SlidingExpiration = TimeSpan.FromDays(7);
                return new QueryExpression(CreateProjection(projectionRequest), pathMaps.Filters());
            })!.Prepare(queryParameters);
    }

    /// <summary>
    /// Creates a <see cref="LambdaExpression"/> representing the projection based on the provided <see cref="ProjectionRequest"/>.
    /// </summary>
    /// <param name="projectionRequest">The projection request containing information about the projection.</param>
    /// <returns>
    /// Returns a <see cref="LambdaExpression"/> representing the projection.
    /// </returns>
    /// <remarks>
    /// This method creates a projection <see cref="LambdaExpression"/> based on the provided <see cref="ProjectionRequest"/>.
    /// It first creates a <see cref="ParameterExpression"/> using the source type of the <see cref="ProjectionRequest"/>.
    /// Then, it initializes a member of the anonymous type specified in the <see cref="ProjectionRequest"/>
    /// using the <see cref="MemberInit"/> method. Finally, it creates a <see cref="LambdaExpression"/>
    /// with the member initialization expression and the parameter expression.
    /// </remarks>
    private LambdaExpression CreateProjection(ProjectionRequest projectionRequest)
    {
        ParameterExpression parameter = Parametor(projectionRequest.SourceType);
        MemberInitExpression initExpression = MemberInit(projectionRequest, parameter);
        return Expression.Lambda(initExpression, parameter);
    }

    /// <summary>
    /// Generates a <see cref="MemberInitExpression"/> for initializing an anonymous type based on the provided <see cref="ProjectionRequest"/>,
    /// </summary>
    /// <param name="projectionRequest">The projection request containing information about the projection.</param>
    /// <param name="parameter">The parameter expression representing the object containing the members.</param>
    /// <returns>
    /// A <see cref="MemberInitExpression"/> for initializing properties of the anonymous type
    /// </returns>
    /// <remarks>
    /// This method initializes a member of a type based on the provided <see cref="ProjectionRequest"/> and parameter expression.
    /// It first creates a new instance of the anonymous type specified in the projection request using <see cref="Expression.New(ConstructorInfo)"/>.
    /// Then, it initializes the members of the new instance using Expression.MemberInit with the member bindings
    /// generated by the <see cref="MemberBindings"/> method.
    /// </remarks>
    private MemberInitExpression MemberInit(ProjectionRequest projectionRequest, Expression parameter)
    {
        NewExpression newExpression = Expression.New(projectionRequest.AnonymousType.GetConstructor(Type.EmptyTypes)!);
        return Expression.MemberInit(newExpression, MemberBindings(projectionRequest, parameter));
    }

    /// <summary>
    /// Generates a collection of <see cref="MemberBinding"/> expressions for initializing properties of an anonymous type.
    /// </summary>
    /// <param name="projectionRequest">The projection request containing information about the projection.</param>
    /// <param name="parameter">The parameter expression representing the object containing the members.</param>
    /// <returns>
    /// An <see cref="Enumerable"/> of <see cref="MemberBinding"/> expressions for property initialization
    /// </returns>
    /// <remarks>
    /// This method generates member bindings for properties in a <see cref="ProjectionRequest"/>. It iterates through the properties
    /// of the anonymous type specified in the <see cref="ProjectionRequest"/>. For each property, it attempts to find a corresponding
    /// property mapper in the property maps of the <see cref="ProjectionRequest"/>. If a property mapper is found, it resolves a member
    /// binding for the property using the <see cref="ResolveMemberBinding"/> method and yields the result.
    /// </remarks>
    private IEnumerable<MemberBinding> MemberBindings(ProjectionRequest projectionRequest, Expression parameter)
    {
        foreach (PropertyInfo propertyInfo in projectionRequest.AnonymousType.GetProperties(InstanceFlags))
        {
            if (projectionRequest.PropertyMaps.TryFirst(x => x.DestinationName == propertyInfo.Name, out PropertyMapper? propertyMapper))
            {
                yield return ResolveMemberBinding(propertyMapper!, propertyInfo, parameter);
            }
        }
    }

    /// <summary>
    /// Resolves a member binding for a property based on the provided <paramref name="propertyMapper"/>.
    /// </summary>
    /// <param name="propertyMapper">The property mapper defining the mapping between source and destination types.</param>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> object representing the property information.</param>
    /// <param name="parameter">The parameter expression representing the object containing the member.</param>
    /// <returns>
    /// Returns a <see cref="MemberBinding"/> representing the resolved binding for the property mapping.
    /// </returns>
    /// <remarks>
    /// This method resolves a member binding for a property mapping. It first attempts to handle any ignored properties
    /// by processing the default expression provided by the <paramref name="propertyMapper"/>.
    /// If the property is not ignored, it resolves the member access expression for the property using the custom map expression (if available) or the property name.
    /// It then processes the path map for the property and resolves populating for the member access expression using
    /// the <see cref="ResolvePopulating"/> method. Finally, it creates and returns a <see cref="MemberBinding"/> for the property.
    /// </remarks>
    private MemberAssignment ResolveMemberBinding(PropertyMapper propertyMapper, PropertyInfo propertyInfo, Expression parameter)
    {
        parameter = propertyMapper.CheckCustomSource(parameter);
        if (propertyMapper.TryHandleIgnored(out Expression? defaultExpression))
        {
            ProcessPathMap(propertyMapper, defaultExpression!, parameter);
            return Expression.Bind(propertyInfo, defaultExpression!);
        }

        Expression memberAccess = ExpressionBuilder.ResolveMemberAccess(parameter, propertyMapper.CustomMapExpression, propertyInfo.Name);
        if (propertyInfo.PropertyType == typeof(object))
        {
            ProcessPathMap(propertyMapper, memberAccess, parameter);
        }

        memberAccess = ResolvePopulating(propertyMapper, propertyInfo, memberAccess);
        ProcessPathMap(propertyMapper, memberAccess, parameter);
        return Expression.Bind(propertyInfo, memberAccess);
    }

    /// <summary>
    /// Adjusts the member access expression to reflect the populating process for a property mapping.
    /// </summary>
    /// <param name="propertyMapper">The property mapper defining the mapping between source and destination types.</param>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/> object representing the property information.</param>
    /// <param name="memberAccess">The expression representing the member access to the property.</param>
    /// <returns>
    /// An <see cref="Expression"/> adjusted to reflect the populating process.
    /// </returns>
    /// <remarks>
    /// This method adjusts the member access expression to reflect the populating process for a property mapping. 
    /// If the property type is <see cref="object"/>, it processes nested properties using the <see cref="ProcessNestedProperties"/> method.
    /// Otherwise, it converts the member access expression to the destination type of the <see cref="PropertyMapper"/> using the
    /// <see cref="ExpressionBuilder.ToType(Expression, Type)"/> method.
    /// If the <see cref="PropertyMapper.AllowNull"/> property is set to <c>true</c> and the destination type is not a collection,
    /// the method ensures that the expression handles null values appropriately by using the <see cref="ExpressionBuilder.IfNullElse"/> method.
    /// </remarks>
    private Expression ResolvePopulating(PropertyMapper propertyMapper, PropertyInfo propertyInfo, Expression memberAccess)
    {
        Expression populateAccess = propertyInfo.PropertyType == typeof(object)
            ? ProcessNestedProperties(propertyMapper, memberAccess, propertyMapper.MemberPath)
            : IPrimitiveMapper.TryMap(memberAccess, propertyMapper).ToType(propertyMapper.DestinationType);

        return propertyMapper.AllowNull
            && !propertyMapper.DestinationType.IsPrimitiveType()
            && !propertyMapper.DestinationType.IsCollection()
                ? memberAccess.IfNullElse(Expression.Default(populateAccess.Type), populateAccess)
                : populateAccess;
    }

    /// <summary>
    /// Processes nested properties based on the <see cref="PropertyMapper"/> information and updates the member access expression accordingly.
    /// </summary>
    /// <param name="propertyMapper">The <see cref="PropertyMapper"/> defining the mapping between source and destination types.</param>
    /// <param name="memberAccess">The expression representing the member access to the nested property.</param>
    /// <param name="memberPath">The member path representing the property access path.</param>
    /// <returns>
    /// Returns an <see cref="Expression"/> representing the processed nested property access.
    /// </returns>
    /// <remarks>
    /// This method processes nested properties within a property mapping. If the destination type of the property mapper
    /// is a class, it initializes a <see cref="ProjectionRequest"/> and visits the member access with a member visitor to perform
    /// the projection. If the destination type is a generic collection, it delegates the processing to the
    /// <see cref="ProcessGenericCollection"/> method. If neither condition is met, it returns the original member access expression.
    /// </remarks>
    private Expression ProcessNestedProperties(PropertyMapper propertyMapper, Expression memberAccess, MemberPath memberPath)
    {
        if (propertyMapper.DestinationType.IsClass())
        {
            ProjectionRequest projectionRequest = new(configurationProvider, propertyMapper.SourceType, propertyMapper.DestinationType, populateAnalyzer, memberPath);
            return memberAccess.VisitMember(projectionRequest, MemberInit);
        }

        if (propertyMapper.DestinationType.IsGenericCollection())
        {
            return ProcessGenericCollection(propertyMapper, memberAccess, memberPath);
        }

        return memberAccess;
    }

    /// <summary>
    /// Processes properties of a generic collection type based on the <see cref="PropertyMapper"/> information and updates the member access expression accordingly.
    /// </summary>
    /// <param name="propertyMapper">The <see cref="PropertyMapper"/> defining the mapping between source and destination types.</param>
    /// <param name="memberAccess">The expression representing the member access to the generic collection property.</param>
    /// <param name="memberPath">The member path representing the property access path.</param>
    /// <returns>
    /// Returns an <see cref="Expression"/> representing the projection process for the generic collection property.
    /// </returns>
    /// <remarks>
    /// This method processes a generic collection property mapping by creating a projection for each element
    /// of the collection. It extracts the generic argument types from the source and destination types of the
    /// <see cref="PropertyMapper"/> and initializes a <see cref="ProjectionRequest"/> with the relevant information.
    /// Depending on whether the member access is a member expression or not, it either calls a method to transform the collection
    /// to a list and apply a projection or directly visits the member access with a member visitor to perform
    /// the projection. The resulting expression represents the projection process for the generic collection property.
    /// </remarks>
    private Expression ProcessGenericCollection(PropertyMapper propertyMapper, Expression memberAccess, MemberPath memberPath)
    {
        Type argumentSourceType = propertyMapper.SourceType.GetCollectionElementType();
        Type argumentDestinationType = propertyMapper.DestinationType.GetCollectionElementType();
        ProjectionRequest projectionRequest = new(configurationProvider, argumentSourceType, argumentDestinationType, populateAnalyzer, memberPath);

        return memberAccess is MemberExpression
            ? MethodExtension.CallToListSelect(memberAccess, CreateProjection(projectionRequest))
            : memberAccess.VisitMember(projectionRequest, MemberInit);
    }

    /// <summary>
    /// Creates a <see cref="ParameterExpression"/> for the specified type.
    /// </summary>
    /// <param name="type">The type of the parameter.</param>
    /// <returns>A <see cref="ParameterExpression"/> for the specified type.</returns>
    /// <remarks>
    /// This method creates a <see cref="ParameterExpression"/> for the specified type with a parameter name generated using <seealso cref="ProjectionUtilities.IncrementParameter"/>.
    /// </remarks>
    private ParameterExpression Parametor(Type type)
        => Expression.Parameter(type, ProjectionUtilities.IncrementParameter("x", ref parameterCount));

    /// <summary>
    /// Processes the path mapping for the specified <paramref name="propertyMapper"/>, <paramref name="memberAccess"/>, and <paramref name="parameter"/>.
    /// </summary>
    /// <param name="propertyMapper">The <see cref="PropertyMapper"/> representing the property mapping.</param>
    /// <param name="memberAccess">The member access expression.</param>
    /// <param name="parameter">The parameter expression.</param>
    /// <remarks>
    /// This method processes the path mapping based on the provided <paramref name="propertyMapper"/>, <paramref name="memberAccess"/>, and <paramref name="parameter"/>.
    /// If the root path of the <see cref="PropertyMapper"/> exists in the path maps and represents a generic collection type,
    /// it adds a new <see cref="PathInfo"/> using CallSelectMany or CallSelect based on the destination type of the <see cref="PropertyMapper"/>.
    /// Otherwise, it adds a new <see cref="PathInfo"/> using the member access expression and destination member of the <see cref="PropertyMapper"/>.
    /// </remarks>
    private void ProcessPathMap(PropertyMapper propertyMapper, Expression memberAccess, Expression parameter)
    {
        try
        {
            if (pathMaps.TryGetValue(propertyMapper.RootPath, out PathInfo? mapRoot) && mapRoot.PathMap.Type.IsGenericCollection())
            {
                if (mapRoot.PathMap is MethodCallExpression methodCallExpression
                    && parameter is not ParameterExpression
                    && (
                        methodCallExpression.Method.Name.EqualSelect()
                        || methodCallExpression.Method.Name.EqualSelectMany()
                        )
                    )
                {
                    pathMaps.TryAdd(propertyMapper.MemberPath, new PathInfo(mapRoot.PathMap.ReplaceSelectMethodMember(memberAccess), propertyMapper.DestinationMember));
                    return;
                }

                if (propertyMapper.DestinationType.IsCollection())
                {
                    pathMaps.TryAdd(propertyMapper.MemberPath, new PathInfo(CallSelectMany(), propertyMapper.DestinationMember));
                    return;
                }

                pathMaps.TryAdd(propertyMapper.MemberPath, new PathInfo(CallSelect(), propertyMapper.DestinationMember));
                return;
            }

            pathMaps.TryAdd(propertyMapper.MemberPath, new PathInfo(memberAccess, propertyMapper.DestinationMember));

            MethodCallExpression CallSelectMany() => MethodExtension.CallSelectMany(mapRoot.PathMap!, MakeLambda());
            MethodCallExpression CallSelect() => MethodExtension.CallSelect(mapRoot.PathMap, MakeLambda());
            LambdaExpression MakeLambda() => Expression.Lambda(memberAccess, (ParameterExpression)parameter);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new PopulateNotHandleException($"{nameof(ProjectionBuilder)}:{nameof(ProcessPathMap)} occur error", ex);
        }
    }
}