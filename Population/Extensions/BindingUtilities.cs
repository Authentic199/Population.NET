using Population.Definations;
using System.Collections;
using System.Reflection;
using static Population.Definations.PopulateConstant;
using static Population.Definations.PopulateConstant.SpecialCharacter;
using static Population.Definations.PopulateOptions;
using static Population.Extensions.RegexExtension;
using ParamsBag = System.Collections.Generic.IDictionary<string, string>;
using ParamsPair = System.Collections.Generic.KeyValuePair<string, string>;

namespace Population.Extensions;

internal static class BindingUtilities
{
    /// <summary>
    /// Resolves and creates a collection value for a property based on the provided ParamsBag.
    /// </summary>
    /// <param name="property">The PropertyInfo representing the collection property.</param>
    /// <param name="paramsBag">The ParamsBag containing key-value pairs representing parameters.</param>
    /// <param name="objectName">The name of the object containing the property.</param>
    /// <returns>
    /// An object representing the resolved collection value. Returns null if the collection could not be created.
    /// </returns>
    internal static object? ResolveCollectionValue(this PropertyInfo property, ParamsBag paramsBag, string objectName)
    {
        Type propertyType = property.PropertyType;
        object? collection = Activator.CreateInstance(propertyType);
        if (collection == null)
        {
            return null;
        }

        IList collectionValue = (IList)collection;
        Type elementType = propertyType.GetCollectionElementType();
        foreach (ParamsPair paramsPair in paramsBag.Where(x => IsSingleMatch(x.Key, BuildPropertyRegex(objectName, property.Name))))
        {
            string[] valueParts = paramsPair.Value.Split(Comma, TrimSplitOptions);
            Array.ForEach(valueParts, value =>
            {
                if (elementType.TryChangeType(value, out object? result))
                {
                    collectionValue.Add(result);
                }
            });
        }

        return collectionValue;
    }

    /// <summary>
    /// Resolves and creates a property value based on the provided ParamsBag for a given PropertyInfo.
    /// </summary>
    /// <param name="property">The PropertyInfo representing the property.</param>
    /// <param name="paramsBag">The ParamsBag containing key-value pairs representing parameters.</param>
    /// <param name="objectName">The name of the object containing the property.</param>
    /// <returns>
    /// An object representing the resolved property value. Returns null if the resolution is unsuccessful.
    /// </returns>
    internal static object? ResolvePropertyValue(this PropertyInfo property, ParamsBag paramsBag, string objectName)
    {
        ParamsPair paramsPair = paramsBag.FirstOrDefault(x => IsSingleMatch(x.Key, BuildPropertyRegex(objectName, property.Name)));
        if (string.IsNullOrWhiteSpace(paramsPair.Key) || string.IsNullOrWhiteSpace(paramsPair.Value))
        {
            return null;
        }

        string[] valueParts = paramsPair.Value.Split(Comma, TrimSplitOptions);

        if (valueParts.Length == 0 || !property.PropertyType.TryChangeType(valueParts[0], out object? result))
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Binds populate parameters and generates an IEnumerable of populate keys based on the provided ParamsBag.
    /// </summary>
    /// <param name="populateParams">The ParamsBag containing key-value pairs representing populate parameters.</param>
    /// <returns>An IEnumerable of populate keys generated from the populate parameters.</returns>
    internal static IEnumerable<string> Bind(this ParamsBag populateParams)
    {
        foreach (ParamsPair paramsPair in populateParams!)
        {
            if (string.IsNullOrEmpty(paramsPair.Value))
            {
                continue;
            }

            // fields=id, fields=createdAt, fields[0]=id
            if (StartFieldOrderRegex.IsMatch(paramsPair.Key)
                && paramsPair.Value.Split(Comma, TrimSplitOptions).Distinct() is IEnumerable<string> splitParts)
            {
                if (splitParts.All(x => !x.EqualAsterisk()))
                {
                    foreach (string valuePart in paramsPair.Value.Split(Comma, TrimSplitOptions).Distinct())
                    {
                        yield return valuePart;
                    }
                }

                continue;
            }

            // populate=root1, populate=*
            if (string.Equals(paramsPair.Key, PopulateAlias, IgnoreCaseCompare))
            {
                if (paramsPair.Value.EqualAsterisk())
                {
                    yield return OneLevelWildcard;
                }

                string[] valueParts = paramsPair.Value.Split(Comma, TrimSplitOptions);

                // In STRAPI not support duplicate query param populate=*&populate=* or populate=root*populate=root -> but still reasonable so still support
                if (valueParts.Distinct().Count() == 1)
                {
                    yield return valueParts[0].EqualAsterisk() ? OneLevelWildcard : valueParts[0].AttachAsterisk();
                    continue;
                }

                continue;
            }

            // populate[root][populate][nested][fields][0]=id, populate[root][populate][nested][fields]=createdAt
            if (IsSingleMatch(paramsPair.Key, LastNestedFieldPattern))
            {
                string populateFieldKey = paramsPair.Key.RegexReplace(LastNestedFieldPattern, Dot.ToString()).PopulateReplace();
                foreach (string valuePart in paramsPair.Value.Split(Comma, TrimSplitOptions).Distinct())
                {
                    yield return string.Concat(populateFieldKey, valuePart);
                }

                continue;
            }

            // populate[root][populate][nested]=true, populate[root][populate][nested]=*, populate[0]=root
            string populateKey = paramsPair.Key.PopulateReplace();

            foreach (string valuePart in paramsPair.Value.Split(Comma, TrimSplitOptions).Distinct())
            {
                if (bool.TryParse(valuePart, out bool result) && result)
                {
                    yield return populateKey.AttachAsterisk();
                    continue;
                }

                if (valuePart.EqualAsterisk())
                {
                    if (IsSingleMatch(populateKey, LastPopulateOrderPattern))
                    {
                        populateKey = populateKey.RegexReplace(LastPopulateOrderPattern, string.Empty);
                        yield return string.Concat(populateKey, OneLevelWildcard);
                        continue;
                    }

                    yield return populateKey.AttachAsterisk();
                    continue;
                }

                yield return string.IsNullOrWhiteSpace(populateKey)
                    ? valuePart.AttachAsterisk()
                    : string.Join(Dot, populateKey.RegexReplace(LastPopulateOrderPattern, string.Empty), valuePart).AttachAsterisk();
            }
        }
    }

    private static string PopulateReplace(this string input)
        => input
        .RegexReplace(StartPopulateOrderPattern, string.Empty) // populate[0] -> string.Empty
        .RegexReplace(StartPopulateBracketPattern, TwoCaptureReplacePattern) // populate[root] -> root
        .RegexReplace(NestedPopulatePattern, NestedPopulateReplacePattern); // root[populate][nested] -> root.nested
}
