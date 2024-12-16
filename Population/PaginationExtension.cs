using Infrastructure.Facades.Populates.Definations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Infrastructure.Facades.Common.Extensions;

public static class PaginationExtension
{
    public static PaginationResponse<T> ToPagedList<T>(this IEnumerable<T> entities, int current, int pageSize)
    {
        IEnumerable<T> items = entities.Skip((current - 1) * pageSize).Take(pageSize);
        return new PaginationResponse<T>(items, entities.Count(), pageSize, current);
    }

    public static PaginationResponse<TEntity, TMoreInfo> ToPagedList<TEntity, TMoreInfo>(this IEnumerable<TEntity> entities, int current, int pageSize, TMoreInfo moreInfo)
    {
        IEnumerable<TEntity> items = entities.Skip((current - 1) * pageSize).Take(pageSize);
        return new PaginationResponse<TEntity, TMoreInfo>(items, entities.Count(), pageSize, current, moreInfo);
    }

    public static async Task<PaginationResponse<T>> ToPagedListAsync<T>(this IQueryable<T> entities, int current, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<T> items = await entities.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginationResponse<T>(items, await entities.CountAsync(cancellationToken: cancellationToken), pageSize, current);
    }

    public static async Task<PaginationResponse<TEntity, TMoreInfo>> ToPagedListAsync<TEntity, TMoreInfo>(this IQueryable<TEntity> entities, int current, int pageSize, TMoreInfo moreInfo, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> items = await entities.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginationResponse<TEntity, TMoreInfo>(items, await entities.CountAsync(cancellationToken: cancellationToken), pageSize, current, moreInfo);
    }
}

public class PaginationResponse<T>
{
    public PaginationResponse(IEnumerable<T> items, int totalCount, int pageSize, int current)
    {
        PagedData = items;
        PageInfo = new(totalCount, pageSize, current);
    }

    public IEnumerable<T> PagedData { get; set; }

    public PageInfo PageInfo { get; set; }
}

public sealed class PaginationResponse<T, TMoreInfo> : PaginationResponse<T>
{
    public PaginationResponse(IEnumerable<T> items, int totalCount, int pageSize, int current, TMoreInfo moreInfo)
        : base(items, totalCount, pageSize, current)
    {
        MoreInfo = moreInfo;
    }

    public TMoreInfo MoreInfo { get; set; }
}

public class PageInfo
{
    public PageInfo(int totalCount, int pageSize, int current)
    {
        TotalCount = totalCount;
        PageSize = pageSize;
        Current = current;
    }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }

    public int Current { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => Current < TotalPages;

    public bool HasPrevious => Current > 1 && Current <= TotalPages;
}

public class QueryContainer : IValidatableObject
{
    /// <summary>
    /// filter data by operator($eq, $null, $in, $gt, $lt, $lte, $gte, $btw, $ilike, $sw) ex: { filter.propName : "$eq:mxm" }
    /// </summary>
    [ModelBinder(BinderType = typeof(CustomFilterBinder))]
    public Dictionary<string, List<string>?>? Filter { get; set; }

    /// <summary>
    /// Number elements on a page.
    /// </summary>
    public int PageSize { get; set; } = int.MaxValue / 2;

    /// <summary>
    /// Pages number to take out of the total pages.
    /// </summary>
    public int Current { get; set; } = 1;

    /// <summary>
    /// Search field. Ex: '["Name","Relatives.Name"]'.
    /// </summary>
    public string[]? SearchFields { get; set; }

    /// <summary>
    /// Search keyword. Ex: 'Magnus Maximus'.
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// Sort query string. Ex: 'Name desc,Region.Name'.
    /// </summary>
    public string? SortQuery { get; set; }

    public List<string>? PopulateKeys { get; set; } = [PopulateConstant.SpecialCharacter.PoundSign];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageSize <= 0 || PageSize > int.MaxValue / 2)
        {
            yield return new ValidationResult(
                $"{nameof(PageSize)}Invalid",
                [nameof(PageSize)]
            );
        }

        if (Current <= 0 || Current > int.MaxValue / 2)
        {
            yield return new ValidationResult(
                $"{nameof(PageSize)}Invalid",
                [nameof(PageSize)]
            );
        }
    }
}

public class CustomFilterBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext.HttpContext.Request.QueryString.HasValue)
        {
            const string filterKey = nameof(QueryContainer.Filter);
            Dictionary<string, List<string?>> filterQueries = bindingContext.HttpContext.Request.QueryString.Value![1..]
                .Split('&')
                .Where(x => x.StartsWith(filterKey, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Split('=')[0])
                .ToDictionary(x => x.Key[(filterKey.Length + 1)..], x => x.Select(x =>
                {
                    string[] compareValue = x.Split('=');
                    return compareValue.Length > 1 ? HttpUtility.UrlDecode(compareValue.GetValue(1)?.ToString()) : string.Empty;
                }).ToList());

            bindingContext.Result = ModelBindingResult.Success(filterQueries);
        }

        return Task.CompletedTask;
    }
}