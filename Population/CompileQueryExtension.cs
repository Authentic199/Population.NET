using AutoMapper;
using Population.Public.Queries;

namespace Population;

public static class CompileQueryExtension
{
    public static async Task<PaginationResponse<dynamic>> CompileQueryAsync<TDestination>(this IQueryable entities, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await entities.CompileCore(typeof(TDestination), context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static async Task<PaginationResponse<dynamic, TMoreInfo>> CompileQueryAsync<TDestination, TMoreInfo>(this IQueryable entities, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await entities.CompileCore(typeof(TDestination), context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, moreInfo, cancellationToken);

    public static async Task<PaginationResponse<dynamic>> CompileQueryAsync(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await entities.CompileCore(destinationType, context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static async Task<PaginationResponse<dynamic, TMoreInfo>> CompileQueryAsync<TMoreInfo>(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await entities.CompileCore(destinationType, context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, moreInfo, cancellationToken);

    public static PaginationResponse<dynamic> CompileQuery<TDestination>(this IQueryable entities, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => entities.CompileCore(typeof(TDestination), context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static PaginationResponse<dynamic, TMoreInfo> CompileQuery<TDestination, TMoreInfo>(this IQueryable entities, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null)
        => entities.CompileCore(typeof(TDestination), context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, moreInfo);

    public static PaginationResponse<dynamic> CompileQuery(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => entities.CompileCore(destinationType, context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static PaginationResponse<dynamic, TMoreInfo> CompileQuery<TMoreInfo>(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null)
        => entities.CompileCore(destinationType, context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, moreInfo);

    private static IQueryable<dynamic> CompileCore(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null)
        => Instance(entities, mapper, destinationType, context, queryOptions).ManipulationChain(Select);
}