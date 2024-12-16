namespace Population.Public.Descriptors;

public record FilterDescriptor(string Path, string Value, NextLogicalOperator LogicalOperator, CompareOperator CompareOperator, string Group);

public enum NextLogicalOperator
{
    /// <summary>
    /// match if all condition is true
    /// </summary>
    And = 1,

    /// <summary>
    /// match if any condition is true
    /// </summary>
    Or = 2,

    /// <summary>
    /// there are no element in next
    /// </summary>
    None = 3,
}

public enum CompareOperator
{
    /// <summary>
    /// Represents an equality comparison.
    /// </summary>
    Equal = 1,

    /// <summary>
    /// Represents an inequality comparison.
    /// </summary>
    NotEqual = 2,

    /// <summary>
    /// Represents a less than comparison.
    /// </summary>
    LessThan = 3,

    /// <summary>
    /// Represents a greater than comparison.
    /// </summary>
    GreaterThan = 4,

    /// <summary>
    /// Represents a greater than or equal to comparison.
    /// </summary>
    GreaterThanOrEqual = 5,

    /// <summary>
    /// Represents a less than or equal to comparison.
    /// </summary>
    LessThanOrEqual = 6,

    /// <summary>
    /// Represents a contains comparison for strings or collections.
    /// </summary>
    Contains = 7,

    /// <summary>
    /// Represents a negation of contains comparison for strings or collections.
    /// </summary>
    NotContains = 8,

    /// <summary>
    /// Represents a starts with comparison for strings.
    /// </summary>
    StartsWith = 9,

    /// <summary>
    /// Represents a negation of starts with comparison for strings.
    /// </summary>
    NotStartsWith = 10,

    /// <summary>
    /// Represents an ends with comparison for strings.
    /// </summary>
    EndsWith = 11,

    /// <summary>
    /// Represents a negation of ends with comparison for strings.
    /// </summary>
    NotEndsWith = 12,

    /// <summary>
    /// Represents an array contains comparison for collections.
    /// </summary>
    In = 13,

    /// <summary>
    /// Represents a negation of array contains comparison for collections.
    /// </summary>
    NotIn = 14,

    /// <summary>
    /// Represents is Null value
    /// </summary>
    Null = 15,

    /// <summary>
    /// Represents is NotNull value
    /// </summary>
    NotNull = 16,
}