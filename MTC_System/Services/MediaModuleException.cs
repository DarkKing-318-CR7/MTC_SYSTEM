namespace MTC_System.Services;

public sealed class MediaModuleException : Exception
{
    public int StatusCode { get; }

    public MediaModuleException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}
