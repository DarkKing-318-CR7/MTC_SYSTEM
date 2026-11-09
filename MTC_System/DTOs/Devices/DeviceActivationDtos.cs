using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs.Devices;

public sealed class DeviceActivationCodeResponse
{
    public Guid DeviceId { get; init; }
    public string DeviceCode { get; init; } = string.Empty;
    public string QrToken { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
    public string QrPayload { get; init; } = string.Empty;
    public string QrCodeDataUrl { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed class ActivateDeviceRequest : IValidatableObject
{
    [Required, StringLength(50)]
    public string DeviceCode { get; init; } = string.Empty;

    [StringLength(200, MinimumLength = 20)]
    public string? QrToken { get; init; }

    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải gồm đúng 6 chữ số.")]
    public string? Otp { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasQrToken = !string.IsNullOrWhiteSpace(QrToken);
        var hasOtp = !string.IsNullOrWhiteSpace(Otp);
        if (hasQrToken == hasOtp)
        {
            yield return new ValidationResult(
                "Chỉ cung cấp một trong hai giá trị QrToken hoặc Otp.",
                new[] { nameof(QrToken), nameof(Otp) });
        }
    }
}

public sealed class RefreshDeviceTokenRequest
{
    [Required, StringLength(50)]
    public string DeviceCode { get; init; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 40)]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class DeviceTokenResponse
{
    public Guid DeviceId { get; init; }
    public string DeviceCode { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public string AccessToken { get; init; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}

public sealed record DeviceActivationAdminContext(
    Guid? UserId,
    Guid? OrganizationId,
    Guid? FacilityId,
    bool IsSystemAdministrator);
