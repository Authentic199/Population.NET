using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Populates.Internal.Queries;

public class QueryBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        IQueryCollection queryCollection = bindingContext.HttpContext.Request.Query;
        Type modelType = bindingContext.ModelType;

        if (modelType != typeof(QueryContext))
        {
            bindingContext.ModelState.TryAddModelError(typeof(QueryBinder).FullName!, $"{nameof(QueryBinder)} does not support for type {modelType}");
        }

        bindingContext.Result = ModelBindingResult.Success(QueryParams.Init(queryCollection, modelType).MakeBuilder().Bind());
        return Task.CompletedTask;
    }
}
