using Microsoft.Extensions.Caching.Memory;
using Population.Extensions;
using Population.Public.Attributes;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using TraversalBag = System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, int>;
using TypeBag = System.ValueTuple<System.Type, Population.Public.MemberPath>;

namespace Population.Public;

public static class PropertyAnalyzer
{
    private const string NullableFullName = "System.Runtime.CompilerServices.NullableAttribute";

    private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// Determines whether a given property can accept a null value.
    /// </summary>
    /// <param name="property">The <see cref="PropertyInfo"/> representing the property to check.</param>
    /// <returns>
    /// <c>true</c> if the property allows null values; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// A property is considered to allow null values if:
    /// <list type="bullet">
    /// <item>It is a value type and is nullable (e.g., <see cref="Nullable{T}"/>).</item>
    /// <item>It has a custom attribute indicating nullability, with a type identifier matching a known nullable type full name.</item>
    /// </list>
    /// This method evaluates the property's type and checks for attributes to make the determination.
    /// </remarks>
    public static bool AllowedNull(this PropertyInfo property)
    {
        Type propertyType = property.PropertyType;
        return (
                    propertyType.IsValueType
                    && propertyType.IsNullableType()
               )
               ||
               propertyType.GetCustomAttributes().Any(x => ((Type)x.TypeId).FullName == NullableFullName);
    }

    /// <summary>
    /// Checks if the specified <paramref name="memberInfo"/> has a specific attribute of type <typeparamref name="TAttribute"/>.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute to check for.</typeparam>
    /// <param name="memberInfo">The member information (e.g., property, method, field, etc.) to check for the attribute.</param>
    /// <returns>
    /// Returns true if the specified <paramref name="memberInfo"/> has an attribute of type <typeparamref name="TAttribute"/>;
    /// otherwise, returns false.
    /// </returns>
    public static bool HasAttribute<TAttribute>(this MemberInfo memberInfo)
        where TAttribute : Attribute
        => memberInfo.GetCustomAttribute<TAttribute>() != null;

    /// <summary>
    /// Checks if the specified <paramref name="memberInfo"/> has a specific attribute.
    /// </summary>
    /// <param name="memberInfo">The member information (e.g., property, method, field, etc.) to check for the attribute.</param>
    /// <param name="attributeType">The type of attribute ignored.</param>
    /// <returns>
    /// Returns true if the specified <paramref name="memberInfo"/> has an attribute;
    /// otherwise, returns false.
    /// </returns>
    public static bool HasAttribute(this MemberInfo memberInfo, Type attributeType)
        => memberInfo.GetCustomAttribute(attributeType) != null;

    /// <summary>
    /// Recursively retrieves the property names of a type up to a specified depth, while ignoring certain attributes and types.
    /// </summary>
    /// <param name="baseType">The base type from which to start retrieving property names.</param>
    /// <param name="level">The depth of recursion for retrieving properties. A value of 0 retrieves only the immediate properties.</param>
    /// <param name="typeIgnores">
    /// An array of types to be ignored during the property retrieval. These types must be attributes, and any invalid type
    /// will result in an exception.
    /// </param>
    /// <returns>
    /// A <see cref="List{T}"/> of property names, formatted as their full paths based on the hierarchy of the properties.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when any type in the <paramref name="typeIgnores"/> array is not an attribute type.
    /// </exception>
    /// <remarks>
    /// This method uses a recursive approach to traverse the properties of the specified <paramref name="baseType"/>. It:
    /// <list type="bullet">
    /// <item>Ignores properties decorated with specific attributes in the <paramref name="typeIgnores"/> array.</item>
    /// <item>Skips certain types such as <c>S3FilePath</c> or properties named "LanguageCode".</item>
    /// <item>Handles nested types by traversing their properties up to the specified <paramref name="level"/> depth.</item>
    /// <item>Includes properties of type <see cref="string"/> but skips collections and system types unless explicitly required.</item>
    /// </list>
    /// </remarks>
    public static List<string> GetPropertyRecursiveWithDeep(this Type baseType, uint level, params Type[] typeIgnores)
    {
        const string PathFormat = "{0}.{1}";

        return Array.Exists(typeIgnores, x => !x.IsSubclassOf(typeof(Attribute)))
            ? throw new ArgumentException($"`{nameof(PropertyAnalyzer)}`:`{nameof(GetPropertyRecursiveWithDeep)}` - Type ignores has an element that refers to a object, not a attribute")
            : DumpObjectTree(baseType.GetProperties(), level, [], string.Empty, [], typeIgnores.Append(typeof(NotMappedAttribute)).Append(typeof(NotSearchAttribute)));

        List<string> DumpObjectTree(PropertyInfo[] propertyInfos, uint level, List<string> result, string path, List<string>? objects, IEnumerable<Type> typeIgnores)
        {
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.CustomAttributes.Any(x => typeIgnores.Contains(x.AttributeType)) || propertyInfo.PropertyType == typeof(S3FilePath) || propertyInfo.Name.Equals("LanguageCode", IgnoreCaseCompare))
                {
                    continue;
                }

                if (level != 0
                    && (propertyInfo.PropertyType.IsGenericCollection() || !propertyInfo.PropertyType.FullName!.StartsWith("System"))
                    && !objects!.Exists(x => x == propertyInfo.Name))
                {
                    objects!.Add(path.Split('.')[^1]);
                    string recursivePath = !string.IsNullOrEmpty(path) ? string.Format(PathFormat, path, propertyInfo.Name) : propertyInfo.Name;
                    Type recursiveType = propertyInfo.PropertyType.IsGenericCollection() ? propertyInfo.PropertyType.GetGenericArguments()[0] : propertyInfo.PropertyType;
                    DumpObjectTree(recursiveType.GetProperties(), level - 1, result, recursivePath, objects, typeIgnores);
                }

                if (propertyInfo.PropertyType == typeof(string))
                {
                    result.Add(!string.IsNullOrEmpty(path) ? string.Format(PathFormat, path, propertyInfo.Name) : propertyInfo.Name);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Retrieves or creates a cached <see cref="MetaPropertyBag"/> for the specified type, analyzing its properties and metadata.
    /// </summary>
    /// <param name="type">The type for which metadata is to be analyzed.</param>
    /// <param name="ignores">A collection of property types to exclude from analysis.</param>
    /// <param name="populateIgnores">A collection of types to ignore when processing reference properties.</param>
    /// <returns>
    /// A <see cref="MetaPropertyBag"/> containing metadata for the properties of the specified type.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an error occurs during the creation or retrieval of metadata for the specified type.
    /// </exception>
    /// <remarks>
    /// This method uses caching to improve performance by storing and reusing previously computed metadata for a type.
    /// It ensures that property metadata is efficiently retrieved without redundant processing.
    /// </remarks>
    public static MetaPropertyBag GetMetaPropertyBag(this Type type, IEnumerable<Type> ignores = default!, params Type[] populateIgnores)
        => Cache.GetOrCreate(type, _ => GetNestedProperties(type, ignores, populateIgnores))
        ?? throw new InvalidOperationException($"An error occurred while retrieved meta properties for type {type.FullName}");

    /// <summary>
    /// Retrieves metadata for the properties of a specified type, including nested types, and organizes them into a <see cref="MetaPropertyBag"/>.
    /// </summary>
    /// <param name="type">The type for which nested properties are to be retrieved.</param>
    /// <param name="ignores">A collection of property types to exclude from analysis.</param>
    /// <param name="populateIgnores">A collection of types to ignore when processing reference properties.</param>
    /// <returns>
    /// A <see cref="MetaPropertyBag"/> containing metadata for the properties and nested properties of the specified type.
    /// </returns>
    /// <remarks>
    /// This method recursively analyzes the properties of the specified type, including nested types, constructing metadata for
    /// each property. It tracks traversal to prevent cycles and efficiently organizes the results into a <see cref="MetaPropertyBag"/>.
    /// </remarks>
    private static MetaPropertyBag GetNestedProperties(Type type, IEnumerable<Type> ignores, params Type[] populateIgnores)
    {
        Stack<TypeBag> stack = new();
        stack.Push((type, MemberPath.Root));
        MetaPropertyBag metaPropertyBag = [];
        TraversalBag traversalPropertyBag = [];

        while (stack.Count > 0)
        {
            TypeBag typeBag = stack.Pop();
            List<MetaProperty> value = GetMetaProperties(typeBag, stack, traversalPropertyBag, ignores ?? [], populateIgnores);
            metaPropertyBag.Add(typeBag.Item2, value);
        }

        return metaPropertyBag;
    }

    /// <summary>
    /// Retrieves metadata for the properties of a specified type, handling nested types and preventing cyclic references.
    /// </summary>
    /// <param name="typeBag">A tuple containing the type and its member path for the current context.</param>
    /// <param name="stack">The stack used to track traversal paths and resolve cycles.</param>
    /// <param name="traversalPropertyBag">A dictionary used to track visited properties and their levels to avoid infinite loops.</param>
    /// <param name="ignores">A collection of property types to exclude from analysis.</param>
    /// <param name="populateIgnores">A collection of types to ignore when processing reference properties.</param>
    /// <returns>
    /// A list of <see cref="MetaProperty"/> objects representing metadata for the properties of the specified type.
    /// </returns>
    /// <remarks>
    /// This method analyzes the properties of the specified type to construct metadata for each property. It handles:
    /// <list type="bullet">
    /// <item>Exclusion of ignored types specified in the <paramref name="ignores"/> parameter.</item>
    /// <item>Detection and avoidance of cycles using <paramref name="traversalPropertyBag"/>.</item>
    /// <item>Processing of reference properties and collection types, including nested properties.</item>
    /// </list>
    /// The method ensures efficient handling of complex type hierarchies with safeguards against infinite recursion.
    /// </remarks>
    private static List<MetaProperty> GetMetaProperties(TypeBag typeBag, Stack<TypeBag> stack, TraversalBag traversalPropertyBag, IEnumerable<Type> ignores, params Type[] populateIgnores)
    {
        Type type = typeBag.Item1;
        MemberPath memberPath = typeBag.Item2;
        List<MetaProperty> metaProperties = [];

        foreach (PropertyInfo propertyInfo in type.GetProperties())
        {
            Type propertyType = propertyInfo.PropertyType;

            MemberPath propertyPath = MemberPath.Init(memberPath.Value, propertyInfo.Name);

            if (ignores.Any(propertyInfo.HasAttribute))
            {
                continue;
            }

            if (populateIgnores.Contains(propertyType) || propertyType.IsDbJsonType())
            {
                metaProperties.Add(new MetaProperty(propertyInfo, propertyPath.Value, IsReferences: false));
                continue;
            }

            if (propertyType.IsClass())
            {
                if (TryResolveRecycles(propertyType, ++memberPath.Level))
                {
                    metaProperties.Add(new MetaProperty(propertyInfo, string.Empty, IsReferences: true));
                }

                continue;
            }

            if (propertyType.IsGenericCollection())
            {
                Type elementType = propertyType.GetCollectionElementType();
                if (TryResolveRecycles(elementType, ++memberPath.Level))
                {
                    metaProperties.Add(new MetaProperty(propertyInfo, string.Empty, IsReferences: true));
                }

                continue;
            }

            metaProperties.Add(new MetaProperty(propertyInfo, propertyPath.Value, IsReferences: false));

            bool TryResolveRecycles(Type referenceType, int incrementLevel)
            {
                if (traversalPropertyBag.TryGetValue(propertyInfo, out int existingLevel))
                {
                    if (existingLevel == incrementLevel)
                    {
                        stack.Push((referenceType, propertyPath));
                        return true;
                    }

                    return false;
                }

                stack.Push((referenceType, propertyPath));
                traversalPropertyBag.Add(propertyInfo, incrementLevel);
                return true;
            }
        }

        return metaProperties;
    }
}
