namespace MTC_System.Common;

public sealed class MediaUploadException : Exception
{
    public int StatusCode { get; }

    public MediaUploadException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}