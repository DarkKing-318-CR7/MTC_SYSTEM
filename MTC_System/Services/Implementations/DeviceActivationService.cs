using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MTC_System.Data;
using MTC_System.DTOs.Devices;
using MTC_System.Models;
using MTC_System.Services.Interfaces;
using Npgsql;
using QRCoder;

namespace MTC_System.Services.Implementations;

public sealed class DeviceActivationService : IDeviceActivationService
{
    private const int MaximumFailedAttempts = 5;
    private readonly MyDbContext _context;
    private readonly byte[] _credentialKey;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _activationCodeDurationMinutes;
    private readonly int _accessTokenDurationMinutes;
    private readonly int _refreshTokenDurationDays;

    public DeviceActivationService(MyDbContext context, IConfiguration configuration)
    {
        _context = context;
        _jwtSecret = configuration["DeviceJwt:SecretKey"]
            ?? configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Thiếu cấu hình DeviceJwt:SecretKey hoặc Jwt:SecretKey.");
        if (Encoding.UTF8.GetByteCount(_jwtSecret) < 32)
            throw new InvalidOperationException("Khóa ký Device JWT phải dài ít nhất 32 byte.");

        var credentialPepper = configuration["DeviceJwt:CredentialPepper"] ?? _jwtSecret;
        _credentialKey = Encoding.UTF8.GetBytes(credentialPepper);
        _jwtIssuer = configuration["DeviceJwt:Issuer"] ?? "MTC_DeviceGateway";
        _jwtAudience = configuration["DeviceJwt:Audience"] ?? "MTC_Devices";
        _activationCodeDurationMinutes = Positive(configuration.GetValue("DeviceJwt:ActivationCodeDurationMinutes", 5), "ActivationCodeDurationMinutes");
        _accessTokenDurationMinutes = Positive(configuration.GetValue("DeviceJwt:AccessTokenDurationMinutes", 15), "AccessTokenDurationMinutes");
        _refreshTokenDurationDays = Positive(configuration.GetValue("DeviceJwt:RefreshTokenDurationDays", 30), "RefreshTokenDurationDays");
    }

    public async Task<DeviceActivationCodeResponse> GenerateActivationCodeAsync(
        Guid deviceId,
        DeviceActivationAdminContext admin,
        CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");

        EnsureAdminScope(device, admin);
        if (!device.ActiveStatus)
            throw DeviceModuleException.Forbidden("Thiết bị đang bị khóa hoặc đã ngừng hoạt động.");

        var now = DateTimeOffset.UtcNow;
        var qrToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expiresAt = now.AddMinutes(_activationCodeDurationMinutes);

        device.ActivationQrTokenHash = HashCredential(device.Id, "qr", qrToken);
        device.ActivationOtpHash = HashCredential(device.Id, "otp", otp);
        device.ActivationCodeExpiresAt = expiresAt;
        device.ActivationFailedAttempts = 0;
        device.ActivationLockedUntil = null;
        device.IsActivated = false;
        device.ActivatedAt = null;
        device.DeviceRefreshTokenHash = null;
        device.DeviceRefreshTokenExpiresAt = null;
        device.DeviceTokenVersion++;
        device.UpdatedAt = now;

        AddActivityLog(device.Id, admin.UserId, "GenerateActivationCode", "Thành công", null, null,
            $"{{\"expiresAt\":\"{expiresAt:O}\"}}");
        await _context.SaveChangesAsync(cancellationToken);

        var qrPayload = $"mtc://device/activate?deviceCode={Uri.EscapeDataString(device.DeviceCode)}&qrToken={Uri.EscapeDataString(qrToken)}";
        return new DeviceActivationCodeResponse
        {
            DeviceId = device.Id,
            DeviceCode = device.DeviceCode,
            QrToken = qrToken,
            Otp = otp,
            QrPayload = qrPayload,
            QrCodeDataUrl = GenerateQrCodeDataUrl(qrPayload),
            ExpiresAt = expiresAt
        };
    }

    public async Task<DeviceTokenResponse> ActivateAsync(
        ActivateDeviceRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var deviceCode = request.DeviceCode.Trim().ToUpperInvariant();
            var device = await _context.Devices.FirstOrDefaultAsync(x => x.DeviceCode == deviceCode, cancellationToken)
                ?? throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");
            EnsureDeviceCanAuthenticate(device);

            var now = DateTimeOffset.UtcNow;
            if (device.ActivationLockedUntil.HasValue && device.ActivationLockedUntil > now)
                throw DeviceModuleException.Locked("Thiết bị tạm khóa kích hoạt do nhập sai quá nhiều lần.");
            if (!device.ActivationCodeExpiresAt.HasValue || device.ActivationCodeExpiresAt <= now ||
                (device.ActivationQrTokenHash is null && device.ActivationOtpHash is null))
                throw DeviceModuleException.Unauthorized("Mã kích hoạt không tồn tại hoặc đã hết hạn.");

            var valid = !string.IsNullOrWhiteSpace(request.QrToken)
                ? FixedTimeMatches(device.ActivationQrTokenHash, HashCredential(device.Id, "qr", request.QrToken.Trim()))
                : FixedTimeMatches(device.ActivationOtpHash, HashCredential(device.Id, "otp", request.Otp!.Trim()));

            if (!valid)
            {
                device.ActivationFailedAttempts++;
                var locked = device.ActivationFailedAttempts >= MaximumFailedAttempts;
                if (locked)
                    device.ActivationLockedUntil = now.AddMinutes(_activationCodeDurationMinutes);
                device.UpdatedAt = now;
                AddActivityLog(device.Id, null, "Activate", "Thất bại", ipAddress,
                    locked ? "Thiết bị tạm khóa do nhập sai mã kích hoạt quá nhiều lần." : "Mã kích hoạt không hợp lệ.");
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw locked
                    ? DeviceModuleException.Locked("Thiết bị tạm khóa kích hoạt do nhập sai quá nhiều lần.")
                    : DeviceModuleException.Unauthorized("Mã kích hoạt không hợp lệ.");
            }

            var tokenPair = CreateTokenPair(device, now);
            device.ActivationQrTokenHash = null;
            device.ActivationOtpHash = null;
            device.ActivationCodeExpiresAt = null;
            device.ActivationFailedAttempts = 0;
            device.ActivationLockedUntil = null;
            device.IsActivated = true;
            device.ActivatedAt = now;
            device.DeviceRefreshTokenHash = tokenPair.RefreshTokenHash;
            device.DeviceRefreshTokenExpiresAt = tokenPair.Response.RefreshTokenExpiresAt;
            device.UpdatedAt = now;
            AddActivityLog(device.Id, null, "Activate", "Thành công", ipAddress, null);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return tokenPair.Response;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw DeviceModuleException.Conflict("Mã kích hoạt đã được sử dụng. Vui lòng sinh mã mới.");
        }
    }

    public async Task<DeviceTokenResponse> RefreshAsync(
        RefreshDeviceTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var deviceCode = request.DeviceCode.Trim().ToUpperInvariant();
            var device = await _context.Devices.FirstOrDefaultAsync(x => x.DeviceCode == deviceCode, cancellationToken)
                ?? throw DeviceModuleException.Unauthorized("Refresh token không hợp lệ.");
            EnsureDeviceCanAuthenticate(device);

            var now = DateTimeOffset.UtcNow;
            var refreshHash = HashCredential(device.Id, "refresh", request.RefreshToken.Trim());
            if (!device.IsActivated || device.DeviceRefreshTokenExpiresAt <= now ||
                !FixedTimeMatches(device.DeviceRefreshTokenHash, refreshHash))
                throw DeviceModuleException.Unauthorized("Refresh token không hợp lệ hoặc đã hết hạn.");

            var tokenPair = CreateTokenPair(device, now);
            device.DeviceRefreshTokenHash = tokenPair.RefreshTokenHash;
            device.DeviceRefreshTokenExpiresAt = tokenPair.Response.RefreshTokenExpiresAt;
            device.UpdatedAt = now;
            AddActivityLog(device.Id, null, "RefreshDeviceToken", "Thành công", ipAddress, null);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return tokenPair.Response;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw DeviceModuleException.Conflict("Refresh token vừa được sử dụng. Vui lòng đăng nhập lại bằng token mới nhất.");
        }
    }

    private (DeviceTokenResponse Response, string RefreshTokenHash) CreateTokenPair(Device device, DateTimeOffset now)
    {
        var accessExpiresAt = now.AddMinutes(_accessTokenDurationMinutes);
        var refreshExpiresAt = now.AddDays(_refreshTokenDurationDays);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, device.Id.ToString()),
            new(ClaimTypes.NameIdentifier, device.Id.ToString()),
            new(ClaimTypes.Name, device.DeviceCode),
            new(ClaimTypes.Role, "DEVICE"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type", "device"),
            new("device_code", device.DeviceCode),
            new("organizationId", device.OrganizationId.ToString()),
            new("facilityId", device.FacilityId.ToString()),
            new("token_version", device.DeviceTokenVersion.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpiresAt.UtcDateTime,
            signingCredentials: credentials);

        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (
            new DeviceTokenResponse
            {
                DeviceId = device.Id,
                DeviceCode = device.DeviceCode,
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                AccessTokenExpiresAt = accessExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExpiresAt
            },
            HashCredential(device.Id, "refresh", refreshToken));
    }

    private void EnsureAdminScope(Device device, DeviceActivationAdminContext admin)
    {
        if (admin.IsSystemAdministrator)
            return;
        if (!admin.OrganizationId.HasValue || admin.OrganizationId.Value != device.OrganizationId)
            throw DeviceModuleException.Forbidden("Bạn không có quyền kích hoạt thiết bị thuộc tổ chức này.");
        if (admin.FacilityId.HasValue && admin.FacilityId.Value != device.FacilityId)
            throw DeviceModuleException.Forbidden("Bạn không có quyền kích hoạt thiết bị thuộc cơ sở này.");
    }

    private static void EnsureDeviceCanAuthenticate(Device device)
    {
        if (device.IsDeleted)
            throw DeviceModuleException.NotFound("Không tìm thấy thiết bị.");
        if (!device.ActiveStatus)
            throw DeviceModuleException.Forbidden("Thiết bị đang bị khóa hoặc đã ngừng hoạt động.");
    }

    private string HashCredential(Guid deviceId, string purpose, string credential)
    {
        using var hmac = new HMACSHA256(_credentialKey);
        var data = Encoding.UTF8.GetBytes($"{deviceId:N}|{purpose}|{credential}");
        return Convert.ToHexString(hmac.ComputeHash(data));
    }

    private static bool FixedTimeMatches(string? storedHash, string candidateHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHash),
                Convert.FromHexString(candidateHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void AddActivityLog(
        Guid deviceId,
        Guid? userId,
        string actionType,
        string result,
        string? ipAddress,
        string? errorMessage,
        string? actionDetail = null)
    {
        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = deviceId,
            UserId = userId,
            ActionType = actionType,
            Result = result,
            IpAddress = ipAddress,
            ErrorMessage = errorMessage,
            ActionDetail = actionDetail
        });
    }

    private static string GenerateQrCodeDataUrl(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return $"data:image/png;base64,{Convert.ToBase64String(qrCode.GetGraphic(8))}";
    }

    private static int Positive(int value, string settingName) => value > 0
        ? value
        : throw new InvalidOperationException($"DeviceJwt:{settingName} phải lớn hơn 0.");
}
