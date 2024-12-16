using static Population.Definations.PopulateConstant.SpecialCharacter;
using static Population.Definations.PopulateOptions;

namespace Population.Public;

public struct MemberPath : IEquatable<MemberPath>
{
    /// <summary>
    ///  Represents a static, readonly instance of the <see cref="MemberPath"/> class with an empty root and member path.
    /// </summary>
    public static readonly MemberPath Root = new(string.Empty, string.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberPath"/> class with the specified root path and member path.
    /// </summary>
    /// <param name="rootPath">The root path to be associated with the new <see cref="MemberPath"/> instance.</param>
    /// <param name="memberPath">The member path to be associated with the new <see cref="MemberPath"/> instance.</param>
    /// <returns>A new <see cref="MemberPath"/> instance with the specified root path and member path.</returns>
    public static MemberPath Init(string rootPath, string memberPath) => new(rootPath, memberPath);

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberPath"/> class with an empty root path and the specified member path.
    /// </summary>
    /// <param name="memberPath">The member path to be associated with the new <see cref="MemberPath"/> instance.</param>
    /// <returns>A new <see cref="MemberPath"/> instance with an empty root path and the specified member path.</returns>
    public static MemberPath InitEmptyRoot(string memberPath) => new(string.Empty, memberPath);

    private MemberPath(string rootPath, string value)
    {
        Value = ResolvePath(rootPath, value);
        Level = ResolveLevel(Value);
        PopulateKey = string.Concat(Value, Asterisk);
    }

    public string Value { get; set; }

    public string PopulateKey { get; set; }

    public int Level { get; set; }

    public readonly bool Equals(MemberPath other) => string.Equals(Value, other.Value, IgnoreCaseCompare);

    public override readonly bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        return obj is MemberPath memberPath && Equals(memberPath);
    }

    public override readonly int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(Value);
        hashCode.Add(PopulateKey);
        hashCode.Add(Level);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(MemberPath left, MemberPath right) => left.Equals(right);

    public static bool operator !=(MemberPath left, MemberPath right) => !(left == right);

    private static string ResolvePath(string path, string value)
    {
        string resultPath = string.IsNullOrWhiteSpace(path) ? value : string.Join(Dot, path, value);
        return resultPath.ToLower();
    }

    private static int ResolveLevel(string value) => string.IsNullOrWhiteSpace(value) ? 0 : value.Split(Dot).Length;
}
