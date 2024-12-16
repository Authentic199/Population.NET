namespace Population.Exceptions
{
    [Serializable]
    public class QueryBuilderException : Exception
    {
        public QueryBuilderException()
        {
        }

        public QueryBuilderException(string? message)
            : base(message)
        {
        }

        public QueryBuilderException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}