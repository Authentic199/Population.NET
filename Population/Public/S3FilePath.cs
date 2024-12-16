namespace Population.Public;

public record S3FilePath(string? Path)
{
    public bool IsSystem { get; set; } = true;
}