using AutoMapper;
using Common.Extensions;
using Populates.Public.Queries;
using static Populates.Extensions.MethodExtension;
using static Populates.Internal.Queries.CompileExpression;

namespace Populates;

public static class CompileQueryExtension
{
    public static async Task<PaginationResponse<dynamic>> CompileQueryAsync<TDestination>(this IQueryable entities, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await CompileCore(entities, typeof(TDestination), context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static async Task<PaginationResponse<dynamic, TMoreInfo>> CompileQueryAsync<TDestination, TMoreInfo>(this IQueryable entities, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await CompileCore(entities, typeof(TDestination), context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, moreInfo, cancellationToken);

    public static async Task<PaginationResponse<dynamic>> CompileQueryAsync(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await CompileCore(entities, destinationType, context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static async Task<PaginationResponse<dynamic, TMoreInfo>> CompileQueryAsync<TMoreInfo>(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => await CompileCore(entities, destinationType, context, mapper, queryOptions).ToPagedListAsync(context.Pagination.Page, context.Pagination.PageSize, moreInfo, cancellationToken);

    public static PaginationResponse<dynamic> CompileQuery<TDestination>(this IQueryable entities, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => CompileCore(entities, typeof(TDestination), context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static PaginationResponse<dynamic, TMoreInfo> CompileQuery<TDestination, TMoreInfo>(this IQueryable entities, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null)
        => CompileCore(entities, typeof(TDestination), context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, moreInfo);

    public static PaginationResponse<dynamic> CompileQuery(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null, CancellationToken cancellationToken = default)
        => CompileCore(entities, destinationType, context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, cancellationToken);

    public static PaginationResponse<dynamic, TMoreInfo> CompileQuery<TMoreInfo>(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, TMoreInfo moreInfo, QueryOptions? queryOptions = null)
        => CompileCore(entities, destinationType, context, mapper, queryOptions).ToPagedList(context.Pagination.Page, context.Pagination.PageSize, moreInfo);

    private static IQueryable<dynamic> CompileCore(this IQueryable entities, Type destinationType, QueryContext context, IMapper mapper, QueryOptions? queryOptions = null)
        => Instance(entities, mapper, destinationType, context, queryOptions).ManipulationChain(Select);
}