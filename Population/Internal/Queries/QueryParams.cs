using Microsoft.AspNetCore.Http;
using Population.Builders;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using ParamsBag = System.Collections.Generic.Dictionary<string, string>;

namespace Population.Internal.Queries;

public class QueryParams
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryParams"/> class.
    /// </summary>
    /// <param name="queryCollection">The query collection to initialize the <see cref="QueryParams"/> with.</param>
    /// <param name="modelType">The type of the model associated with the query parameters.</param>
    /// <returns>A new instance of the <see cref="QueryParams"/> class initialized with the provided query collection and model type.</returns>
    /// <remarks>
    /// This method creates a new instance of the <see cref="QueryParams"/> class using the provided query collection
    /// and model type. <see cref="QueryParams"/> is a utility class for handling query parameters in web applications.
    /// </remarks>
    public static QueryParams Init(IQueryCollection queryCollection, Type modelType) => new(queryCollection, modelType);

    private const string Params = nameof(Params);

    public QueryParams(IQueryCollection queryCollection, Type modelType)
    {
        SetParams(queryCollection, modelType);
        QueryCollection = queryCollection;
        ModelType = modelType;
    }

    public IQueryCollection QueryCollection { get; }

    public Type ModelType { get; }

    public ParamsBag? SortParams { get; set; }

    public ParamsBag? FilterParams { get; set; }

    public ParamsBag? PaginationParams { get; set; }

    public ParamsBag? SearchParams { get; set; }

    public ParamsBag? PopulateParams { get; set; }

    public static bool DetectAction(string input, QueryAction action) => input.Contains(action.ToString(), IgnoreCaseCompare);

    /// <summary>
    /// Creates a new instance of the <see cref="BindingBuilder"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="BindingBuilder"/> class.</returns>
    /// <remarks>
    /// This method is used to create a new <see cref="BindingBuilder"/> instance.
    /// <see cref="BindingBuilder"/> is a utility class for constructing bindings between properties.
    /// </remarks>
    public BindingBuilder MakeBuilder() => new(this);

    private static ParamsBag GetParams(IQueryCollection queryCollection, string typeQuery)
        => queryCollection
            .Where(
                x => DetectAction(typeQuery, QueryAction.Sort)
                    ? SortMatch(x.Key, typeQuery)
                    : QueryMatch(x.Key, typeQuery)
            )
            .OrderBy(x => x.Key).ToDictionary(x => x.Key, x => HttpUtility.UrlDecode(x.Value.ToString()));

    private static bool SortMatch(string input, string typeQuery)
        => Regex.IsMatch(input, string.Format(UniqueIndexPattern, typeQuery), IgnoreCaseOptions);

    private static bool QueryMatch(string input, string typeQuery)
        => DetectAction(typeQuery, QueryAction.Populate)
        ? Regex.IsMatch(input, PopulateBracketPattern, IgnoreCaseOptions) || Regex.IsMatch(input, StartFieldOrderPattern, IgnoreCaseOptions)
        : input.StartsWith($"{typeQuery}[", IgnoreCaseCompare);

    private void SetParams(IQueryCollection queryCollection, Type modelType)
    {
        PropertyInfo[] modelProperties = modelType.GetProperties();
        foreach (PropertyInfo thisProperty in GetType().GetProperties().Where(x => x.PropertyType == typeof(ParamsBag)))
        {
            string thisPropertyName = thisProperty.Name;
            string shortName = thisPropertyName[..thisPropertyName.IndexOf(Params)];

            if (Array.Find(modelProperties, x => x.Name.Contains(shortName, IgnoreCaseCompare)) is { } modelProperty)
            {
                ParamsBag paramsBag = GetParams(queryCollection, modelProperty.Name);
                thisProperty.SetValue(this, paramsBag);
            }
        }
    }
}
