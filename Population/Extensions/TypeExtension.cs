using Population.Exceptions;
using Population.Internal;
using Population.Public.Descriptors;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Population.Extensions;

internal static class TypeExtension
{
    private static readonly ConcurrentDictionary<string, Type> DbJsonGenericArgumentTypeCache = new();
    private static readonly ImmutableDictionary<string, Type> DbJsonTypeCache = GetDbJsonTypeCache();

    /// <summary>
    /// Determines whether the specified type represents a <see cref="long"/> type.
    /// </summary>
    /// <param name="targetType">The type to check.</param>
    /// <returns>True if the specified type represents a <see cref="long"/> type; otherwise, false.</returns>
    internal static bool IsLong(this Type targetType) => targetType == typeof(long) || targetType == typeof(ulong);

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is an enumeration type,
    /// considering nullable types.
    /// </summary>
    /// <param name="targetType">The type to check.</param>
    /// <returns>
    /// <c>true</c> if the specified type is an enumeration type; otherwise, <c>false</c>.
    /// For nullable types, returns <c>true</c> if the underlying type is an enumeration.
    /// </returns>
    internal static bool IsEnum(this Type targetType) => targetType.IsNullableType() ? Nullable.GetUnderlyingType(targetType)?.IsEnum == true : targetType.IsEnum;

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is a <see cref="Guid"/> type,
    /// considering nullable types.
    /// </summary>
    /// <param name="targetType">The type to check.</param>
    /// <returns>
    /// <c>true</c> if the specified type is a <see cref="Guid"/> type; otherwise, <c>false</c>.
    /// For nullable types, returns <c>true</c> if the underlying type is a <see cref="Guid"/>.
    /// </returns>
    internal static bool IsGuid(this Type targetType) => targetType.IsNullableType() ? Nullable.GetUnderlyingType(targetType) == typeof(Guid) : targetType == typeof(Guid);

    /// <summary>
    /// Determines whether the specified type represents a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> type.
    /// </summary>
    /// <param name="targetType">The type to check.</param>
    /// <returns>True if the specified type represents a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> type; otherwise, false.</returns>
    internal static bool IsDateTime(this Type targetType) => targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset);

    /// <summary>
    /// Determines whether the specified type represents a class type (excluding <see cref="string"/> and <see cref="IEnumerable"/> types).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a class type; otherwise, false.</returns>
    internal static bool IsClass(this Type type) => type != typeof(string) && !type.IsAssignableTo(typeof(IEnumerable)) && type.IsClass;

    /// <summary>
    /// Determines whether the specified type represents a collection type (<see cref="Array"/> or <see cref="IEnumerable"/>).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a collection type; otherwise, false.</returns>
    internal static bool IsCollection(this Type type) => type.IsArray || (type != typeof(string) && type.IsAssignableTo(typeof(IEnumerable)));

    /// <summary>
    /// Determines whether the specified type represents a <see cref="Nullable"/> type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a <see cref="Nullable"/> type; otherwise, false.</returns>
    internal static bool IsNullableType(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// Determines whether the specified type represents a generic collection with a reference type as its generic argument.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a generic collection with a reference type as its generic argument; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the specified type is a collection (<see cref="Array"/> or <see cref="IEnumerable"/>) and if its generic argument is a reference type (class or collection).
    /// </remarks>
    internal static bool IsGenericCollection(this Type type) => type.IsCollection() && type.GetCollectionElementType() is Type elementType && (elementType.IsClass() || elementType.IsCollection());

    /// <summary>
    /// Determines whether the specified type represents a generic collection of the specified generic argument type.
    /// </summary>
    /// <param name="baseType">The base type to check.</param>
    /// <param name="genericArgumentType">The type of the generic argument.</param>
    /// <returns>True if the specified type represents a generic collection of the specified <paramref name="genericArgumentType"/>; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the specified base type is a collection (<see cref="Array"/> or <see cref="IEnumerable"/>) and if its generic argument matches the specified <paramref name="genericArgumentType"/>.
    /// </remarks>
    internal static bool IsGenericCollectionOfType(this Type baseType, Type genericArgumentType) => baseType.IsCollection() && baseType.GetCollectionElementType() == genericArgumentType;

    /// <summary>
    /// Determines whether a type should be ignored during search operations.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the type is an enum, a GUID, a collection, or a database JSON type; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks specific characteristics of a type to decide if it is not relevant for search operations:
    /// <list type="bullet">
    /// <item>Enums are typically not searched directly.</item>
    /// <item>GUIDs are often used as unique identifiers and may not be part of search logic.</item>
    /// <item>Collections and database JSON types represent complex or nested data, which might not be searchable directly.</item>
    /// </list>
    /// </remarks>
    internal static bool IsIgnoreSearch(this Type type)
        => type.IsEnum()
        || type.IsGuid()
        || type.IsCollection()
        || type.IsDbJsonType()
        || type == typeof(TimeOnly) // Error: Translation of method 'System.TimeOnly.ToString' failed. If this method can be mapped to your custom function, see https://go.microsoft.com/fwlink/?linkid=2132413 for more information
        || type == typeof(bool)
        || (
             type.IsNullableType() &&
             IsIgnoreSearch(Nullable.GetUnderlyingType(type)!)
            );

    /// <summary>
    /// Determines whether the specified type represents a primitive type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the specified type represents a primitive type; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the specified type is a numeric type, an <see cref="Enum"/>, <see cref="string"/>, <see cref="char"/>, <see cref="Guid"/>, or <see cref="bool"/>.
    /// Additionally, if the type is <see cref="Nullable"/>, it recursively checks if its underlying type is a primitive type.
    /// </remarks>
    internal static bool IsPrimitiveType(this Type type)
    {
        return IsNumericType(type) ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(char) ||
            type == typeof(Guid) ||
            type == typeof(bool) ||
            (
             type.IsNullableType() &&
             IsPrimitiveType(Nullable.GetUnderlyingType(type)!)
            );
    }

    /// <summary>
    /// Determines whether the specified type represents a numeric type.
    /// </summary>
    /// <param name="targetType">The type to check.</param>
    /// <returns>True if the specified type represents a numeric type; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the specified type is one of the numeric types: <see cref="int"/>, <see cref="uint"/>, <see cref="float"/>,
    /// <see cref="double"/>, <see cref="decimal"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="byte"/>, <see cref="sbyte"/>, <see cref="long"/>,
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/>.
    /// Additionally, if the type is <see cref="Nullable"/>, it recursively checks if its underlying type is a numeric type.
    /// </remarks>
    internal static bool IsNumericType(this Type targetType)
    {
        return targetType == typeof(int) ||
               targetType == typeof(uint) ||
               targetType == typeof(float) ||
               targetType == typeof(double) ||
               targetType == typeof(decimal) ||
               targetType == typeof(short) ||
               targetType == typeof(ushort) ||
               targetType == typeof(byte) ||
               targetType == typeof(sbyte) ||
               targetType.IsLong() ||
               targetType.IsDateTime() ||
               (
                targetType.IsNullableType() &&
                IsNumericType(Nullable.GetUnderlyingType(targetType)!)
               );
    }

    /// <summary>
    /// Gets the element type of the specified collection type.
    /// </summary>
    /// <param name="collectionType">The type representing the collection.</param>
    /// <returns>The element type of the collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the specified type is not a collection.</exception>
    /// <remarks>
    /// This method retrieves the element type of the specified collection type.
    /// If the collection type is an array, it returns the element type of the array.
    /// If the collection type is a generic collection (e.g., List&lt;T&gt;), it returns the generic type argument representing the element type.
    /// </remarks>
    internal static Type GetCollectionElementType(this Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType()!;
        }

        if (collectionType.GenericTypeArguments.Length > 0)
        {
            return collectionType.GenericTypeArguments[0];
        }

        throw new InvalidOperationException($"{collectionType.FullName} is not collection");
    }

    /// <summary>
    /// Checks if the values in the input string are valid for the specified target type.
    /// </summary>
    /// <param name="input">The input string containing values separated by commas.</param>
    /// <param name="targetType">The target type to check the validity of the values against.</param>
    /// <returns>True if all values in the input string are valid for the target type; otherwise, false.</returns>
    /// <remarks>
    /// This method iterates through each value in the input string, attempting to convert it to the specified target type.
    /// If any value fails to convert, the method returns false; otherwise, it returns true.
    /// </remarks>
    internal static bool IsValidValuesForType(string input, Type targetType)
    {
        foreach (string value in input.Split(Comma, TrimSplitOptions))
        {
            if (!targetType.TryChangeType(value.Trim(), out _))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the specified <see cref="CompareOperator"/> is valid for the specified target type.
    /// </summary>
    /// <param name="compareOperator">The <see cref="CompareOperator"/> to check.</param>
    /// <param name="targetType">The target type to check the validity of the <see cref="CompareOperator"/> against.</param>
    /// <returns>True if the <see cref="CompareOperator"/> is valid for the target type; otherwise, false.</returns>
    /// <remarks>
    /// This method checks if the specified <see cref="CompareOperator"/> is valid for the specified target type.
    /// For example, if the <see cref="CompareOperator"/> is Contains, the target type should not be bool or bool?.
    /// If the <see cref="CompareOperator"/> belongs to a comparison group, the target type should be numeric.
    /// </remarks>
    internal static bool IsValidCompareOperatorForType(CompareOperator compareOperator, Type targetType)
    {
        if (compareOperator == CompareOperator.Contains)
        {
            return targetType != typeof(bool) || targetType != typeof(bool?);
        }

        if (OperatorManager.IsComparisonGroup(compareOperator))
        {
            return targetType.IsNumericType();
        }

        return true;
    }

    /// <summary>
    /// Determines whether a given type is a cached database JSON type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// <c>true</c> if the type is found in the database JSON type cache or if it is a generic collection whose element type
    /// is found in the generic argument type cache; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method checks if a type is present in the <see cref="DbJsonTypeCache"/>. If the type is a generic collection, it checks whether
    /// the element type of the collection is present in the <see cref="DbJsonGenericArgumentTypeCache"/>.
    /// </remarks>
    internal static bool IsDbJsonType(this Type type)
        => DbJsonTypeCache.ContainsKey(type.FullName ?? type.Name)
        ||
        (
            type.IsGenericCollection()
            && type.GetCollectionElementType() is Type elementType
            && DbJsonGenericArgumentTypeCache.ContainsKey(elementType.FullName ?? elementType.Name)
        );

    /// <summary>
    /// Tries to change the type of the specified value to the specified conversion type.
    /// </summary>
    /// <param name="conversionType">The type to which the value should be converted.</param>
    /// <param name="value">The value to be converted.</param>
    /// <param name="result">When this method returns, contains the converted value if the conversion succeeded; otherwise, null.</param>
    /// <returns>True if the conversion succeeded; otherwise, false.</returns>
    internal static bool TryChangeType(this Type conversionType, object? value, out object? result)
    {
        if (value == null)
        {
            result = conversionType.IsNullableType() ? null : Expression.Constant(default, conversionType);
            return conversionType.IsNullableType();
        }

        Type actualType = conversionType.IsNullableType() ? Nullable.GetUnderlyingType(conversionType)! : conversionType;

        try
        {
            result = TypeDescriptor.GetConverter(actualType).ConvertFrom(value);
            return result != null;
        }
        catch (Exception ex)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Resolves the type of the specified member.
    /// </summary>
    /// <param name="memberInfo">The MemberInfo object representing the member.</param>
    /// <returns>The type of the member.</returns>
    /// <exception cref="PopulateNotHandleException">Thrown when the specified MemberInfo is not supported.</exception>
    /// <remarks>
    /// This method resolves the type of the specified member, which can be a <see cref="PropertyInfo"/>, <see cref="FieldInfo"/>, or <see cref="MethodInfo"/>.
    /// </remarks>
    internal static Type ResolveMemberType(this MemberInfo memberInfo)
        => memberInfo switch
        {
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            FieldInfo fieldInfo => fieldInfo.FieldType,
            MethodInfo methodInfo => methodInfo.ReturnType,
            _ => throw new PopulateNotHandleException($"{nameof(TypeExtension)}:{nameof(ResolveMemberType)} not support member info"),
        };

    /// <summary>
    /// Retrieves the underlying type of a nullable type if the specified type is nullable; 
    /// otherwise, returns the specified type itself.
    /// </summary>
    /// <param name="type">The type to inspect for an underlying nullable type.</param>
    /// <returns>The underlying type if <paramref name="type"/> is a nullable type; otherwise, the original type.</returns>
    internal static Type GetUnderlyingType(this Type type)
        => type.IsNullableType() ? Nullable.GetUnderlyingType(type)! : type;

    /// <summary>
    /// Determines whether a given property is marked as a Database JSON property based on its attributes.
    /// </summary>
    /// <param name="propertyInfo">The property information to check.</param>
    /// <returns>
    /// <c>true</c> if the property is marked with a <see cref="ColumnAttribute"/> and its type name contains "json";
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This extension method checks if a property has a <see cref="ColumnAttribute"/> with a type name that includes the string "json".
    /// The comparison is case-insensitive.
    /// </remarks>
    private static bool IsJson(this PropertyInfo propertyInfo)
        => propertyInfo.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute columnAttribute
        && columnAttribute.TypeName?.Contains("json", IgnoreCaseCompare) == true;

    /// <summary>
    /// Scans the properties in the calling assembly to find those marked as Database JSON properties and caches them.
    /// </summary>
    /// <returns>
    /// An immutable dictionary where the keys are the full names of the property types (or type names if full names are not available),
    /// and the values are the corresponding property types.
    /// </returns>
    /// <remarks>
    /// This method retrieves all properties from the calling assembly, filters them to include only those marked as Database JSON properties,
    /// and ensures they are distinct by their property types. If a property type is a generic collection, the element type of the collection
    /// is cached separately for efficient lookup.
    /// Example: If a Database JSON property is of type List&lt;Media&gt;, then the element type Media is cached to account for 
    /// ICollection&lt;Media&gt;, IEnumerable&lt;Media&gt;, etc.
    /// </remarks>
    private static ImmutableDictionary<string, Type> GetDbJsonTypeCache()
    {
        IEnumerable<PropertyInfo> dbJsonProperties = Assembly
        .GetCallingAssembly()
        .ExportedTypes
        .SelectMany(x => x.GetProperties())
        .Where(IsJson)
        .DistinctBy(x => x.PropertyType.FullName)
        .ToList();

        // Nếu db json type là collection thì lấy generic argument để cache:
        // Example: List<Media> là Database JSON property, thì cache Media luôn, để ICollection<Media>, IEnumerable<Media> cũng được tính
        foreach (PropertyInfo item in dbJsonProperties.Where(x => x.PropertyType.IsGenericCollection()))
        {
            Type type = item.PropertyType.GetCollectionElementType();
            DbJsonGenericArgumentTypeCache.TryAdd(type.FullName ?? type.Name, type);
        }

        return dbJsonProperties.ToImmutableDictionary(key => key.PropertyType.FullName ?? key.PropertyType.Name, value => value.PropertyType);
    }
}
