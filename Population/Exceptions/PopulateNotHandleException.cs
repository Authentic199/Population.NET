namespace Populates.Exceptions;

[Serializable]
public class PopulateNotHandleException : Exception
{
    public PopulateNotHandleException()
    {
    }

    public PopulateNotHandleException(string exception)
        : base(exception)
    {
    }

    public PopulateNotHandleException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
