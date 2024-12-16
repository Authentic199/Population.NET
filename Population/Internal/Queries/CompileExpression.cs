using AutoMapper;
using Populates.Builders;
using Populates.Extensions;
using Populates.Mappers;
using Populates.Public;
using Populates.Public.Descriptors;
using Populates.Public.Queries;
using Microsoft.EntityFrameworkCore;
using Population.Builders;
using Population.Public;
using Population.Public.Descriptors;
using System.Linq.Expressions;

namespace Populates.Internal.Queries;

internal sealed class CompileExpression : QueryExpression
{
    internal static CompileExpression Instance(IQueryable source, IMapper mapper, Type destinationType, QueryContext context, QueryOptions? queryOptions)
        => new(source, mapper, destinationType, context, queryOptions);

    internal CompileExpression(IQueryable source, IMapper mapper, Type destinationType, QueryContext context, QueryOptions? queryOptions)
        : base(mapper.ConfigurationProvider.MakeBuilder().GetProjection(source, destinationType, context.Populate.PopulateKeys ?? [], queryOptions?.Parameters))
    {
        Source = source;
        Context = context;
        AsSplitQuery = queryOptions?.AsSplitQuery ?? false;
    }

    internal IQueryable? Source { get; }

    internal IQueryable? ManipulatedSource { get; private set; }

    internal QueryContext? Context { get; }

    internal bool AsSplitQuery { get; }

    internal CompileExpression Manipulate()
    {
        ManipulatedSource = Sort(Search(Filter(InspectAnchor, InspectPrepare)));
        return this;
    }

    internal IQueryable<dynamic> DynamicChain(Func<IQueryable, LambdaExpression, IQueryable> select)
        => !AsSplitQuery
        ? InternalChain(ManipulatedSource ?? InspectAnchor(), select)
        : InternalChain(ManipulatedSource ?? InspectAnchor(), select).AsSplitQuery();

    internal IQueryable<dynamic> ManipulationChain(Func<IQueryable, LambdaExpression, IQueryable> select)
        => !AsSplitQuery
        ? InternalChain(Sort(Search(Filter(InspectAnchor, InspectPrepare))), select)
        : InternalChain(Sort(Search(Filter(InspectAnchor, InspectPrepare))), select).AsSplitQuery();

    private IQueryable<dynamic> InternalChain(IQueryable source, Func<IQueryable, LambdaExpression, IQueryable> select) => (IQueryable<dynamic>)Chain(source, select);

    private IQueryable Filter(Func<IQueryable> anchor, Action prepare)
    {
        prepare.Invoke();

        if (Context!.Filters is null
            || Context.Filters!.Count == 0
            || PathMap.CreateFilterExpression(Context.Filters, RootParameter) is not LambdaExpression filter
            )
        {
            return anchor.Invoke();
        }

        return anchor.Invoke().Where(filter);
    }

    private IQueryable Search(IQueryable source)
    {
        if (string.IsNullOrWhiteSpace(Context!.Search?.Keyword)
            || PathMapper.Map(PathMap) is not IMetaPathBag searchPropertyAccesses
            || searchPropertyAccesses.BuildDescriptor(Context.Search!.Keyword!, Context.Search?.Fields, typeof(S3FilePath)) is not IEnumerable<FilterDescriptor> searchs
            || !searchs.Any()
            || searchPropertyAccesses.CreateFilterExpression(searchs, RootParameter, false) is not LambdaExpression search
            )
        {
            return source;
        }

        return source.Where(search);
    }

    private IQueryable Sort(IQueryable source)
    {
        if (SortBuilder.HandleEmpty(Context!.Sort, source.ElementType) is not ICollection<SortDescriptor> sorts)
        {
            return source;
        }

        return source.ApplySort(PathMap, sorts, RootParameter);
    }

    private IQueryable InspectAnchor()
    {
        if (Source is null)
        {
            throw new ArgumentException($"{nameof(QueryExpression)} when must be anchored to a {nameof(IQueryable)} source");
        }

        return Source;
    }

    private void InspectPrepare()
    {
        if (Context is null)
        {
            throw new ArgumentException($"{nameof(CompileExpression)} when must be prepare a {nameof(QueryContext)}");
        }
    }
}
