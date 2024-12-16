using AutoMapper;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Public.Descriptors;
using static Infrastructure.Facades.Populates.Extensions.MethodExtension;
using DictionaryParameters = System.Collections.Generic.IDictionary<string, object>;

namespace Infrastructure.Facades.Populates;

public static class ProjectionExtension
{
    public static IQueryable<dynamic> ProjectDynamic<TDestination>(this IQueryable source, IMapper mapper, PopulateDescriptor? populate)
        => ToCore(source, mapper, typeof(TDestination), null, populate?.PopulateKeys ?? []);

    public static IQueryable<dynamic> ProjectDynamic<TDestination>(this IQueryable source, IMapper mapper, PopulateDescriptor? populate, DictionaryParameters queryParameters)
        => ToCore(source, mapper, typeof(TDestination), queryParameters, populate?.PopulateKeys ?? []);

    public static IQueryable<dynamic> ProjectDynamic<TDestination>(this IQueryable source, IMapper mapper, PopulateDescriptor? populate, object queryParameters)
        => ToCore(source, mapper, typeof(TDestination), queryParameters, populate?.PopulateKeys ?? []);

    public static IQueryable<dynamic> ProjectDynamic<TDestination>(this IQueryable source, IMapper mapper, params string[] populates)
        => ToCore(source, mapper, typeof(TDestination), null, populates);

    public static IQueryable<dynamic> ProjectDynamic<TDestination>(this IQueryable source, IMapper mapper, DictionaryParameters queryParameters, params string[] populates)
        => ToCore(source, mapper, typeof(TDestination), queryParameters, populates);

    public static IQueryable<dynamic> ProjectDynamic<TDestination>(this IQueryable source, IMapper mapper, object queryParameters, params string[] populates)
        => ToCore(source, mapper, typeof(TDestination), queryParameters, populates);

    public static IQueryable<dynamic> ProjectDynamic(this IQueryable source, Type destinationType, IMapper mapper, PopulateDescriptor? populate)
        => ToCore(source, mapper, destinationType, null, populate?.PopulateKeys ?? []);

    public static IQueryable<dynamic> ProjectDynamic(this IQueryable source, Type destinationType, IMapper mapper, PopulateDescriptor? populate, DictionaryParameters queryParameters)
        => ToCore(source, mapper, destinationType, queryParameters, populate?.PopulateKeys ?? []);

    public static IQueryable<dynamic> ProjectDynamic(this IQueryable source, Type destinationType, IMapper mapper, PopulateDescriptor? populate, object queryParameters)
        => ToCore(source, mapper, destinationType, queryParameters, populate?.PopulateKeys ?? []);

    public static IQueryable<dynamic> ProjectDynamic(this IQueryable source, Type destinationType, IMapper mapper, params string[] populates)
        => ToCore(source, mapper, destinationType, null, populates);

    public static IQueryable<dynamic> ProjectDynamic(this IQueryable source, Type destinationType, IMapper mapper, object queryParameters, params string[] populates)
        => ToCore(source, mapper, destinationType, queryParameters, populates);

    public static IQueryable<dynamic> ProjectDynamic(this IQueryable source, Type destinationType, IMapper mapper, DictionaryParameters queryParameters, params string[] populates)
        => ToCore(source, mapper, destinationType, queryParameters, populates);

    private static IQueryable<dynamic> ToCore(IQueryable source, IMapper mapper, Type destinationType, object? queryParameters, IEnumerable<string> populates)
        => (IQueryable<dynamic>)mapper.ConfigurationProvider.MakeBuilder().GetProjection(source, destinationType, populates, queryParameters).Chain(source, Select);
}
