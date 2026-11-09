namespace MTC_System.Services;

public sealed class DeviceModuleException : Exception
{
    public int StatusCode { get; }

    public DeviceModuleException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public static DeviceModuleException BadRequest(string message) => new(400, message);
    public static DeviceModuleException Unauthorized(string message) => new(401, message);
    public static DeviceModuleException Forbidden(string message) => new(403, message);
    public static DeviceModuleException NotFound(string message) => new(404, message);
    public static DeviceModuleException Conflict(string message) => new(409, message);
    public static DeviceModuleException Locked(string message) => new(423, message);
}
