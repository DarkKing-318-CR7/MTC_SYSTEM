using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.MediaUpload;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implements;

public sealed class ChunkedMediaUploadSvc : IChunkedMediaUploadSvc
{
    private const string UploadingStatus = "Uploading";
    private const string FinalizingStatus = "Finalizing";
    private const string CompletedStatus = "Completed";
    private const string FailedStatus = "Failed";
    private const string ExpiredStatus = "Expired";
    private const long MaximumFileSize = 1024L * 1024 * 1024;
    private const int DefaultChunkSize = 4 * 1024 * 1024;
    private const int MaximumChunkSize = 16 * 1024 * 1024;
    private static readonly object SessionLockSync = new();
    private static readonly Dictionary<Guid, SessionLockEntry> SessionLocks = new();
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mp3", ".wav", ".png", ".jpg", ".jpeg" };

    private readonly MyDbContext _db;
    private readonly string _storageRoot;
    private readonly int _chunkSize;
    private readonly TimeSpan _sessionLifetime;

    public ChunkedMediaUploadSvc(MyDbContext db, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _db = db;
        var configuredRoot = configuration["MediaUpload:StorageRoot"] ?? "App_Data/MediaUploads";
        if (Path.IsPathRooted(configuredRoot)) throw new InvalidOperationException("MediaUpload:StorageRoot must be relative to the application content root.");
        var contentRoot = Path.GetFullPath(environment.ContentRootPath) + Path.DirectorySeparatorChar;
        _storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
        if (!_storageRoot.StartsWith(contentRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MediaUpload:StorageRoot must remain inside the application content root.");

        _chunkSize = int.TryParse(configuration["MediaUpload:ChunkSizeBytes"], out var configuredChunkSize)
            ? configuredChunkSize : DefaultChunkSize;
        if (_chunkSize <= 0 || _chunkSize > MaximumChunkSize)
            throw new InvalidOperationException($"MediaUpload:ChunkSizeBytes must be between 1 and {MaximumChunkSize}.");
        var lifetimeHours = int.TryParse(configuration["MediaUpload:SessionLifetimeHours"], out var hours) ? hours : 24;
        if (lifetimeHours <= 0 || lifetimeHours > 24 * 30)
            throw new InvalidOperationException("MediaUpload:SessionLifetimeHours must be between 1 and 720.");
        _sessionLifetime = TimeSpan.FromHours(lifetimeHours);
    }

    public async Task<InitializeMediaUploadResponseDto> InitializeAsync(InitializeMediaUploadRequestDto request, CancellationToken cancellationToken)
    {
        await CleanupExpiredSessionsAsync(cancellationToken);
        var suppliedFileName = request.FileName?.Trim();
        var fileName = string.IsNullOrEmpty(suppliedFileName)
            ? string.Empty
            : Path.GetFileName(suppliedFileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255 || fileName.Any(char.IsControl) ||
            fileName is "." or ".." || !AllowedExtensions.Contains(Path.GetExtension(fileName)))
            throw new MediaUploadException(StatusCodes.Status400BadRequest, "File name or extension is not allowed.");
        if (request.FileSize < 1 || request.FileSize > MaximumFileSize)
            throw new MediaUploadException(StatusCodes.Status400BadRequest, "File size must be between 1 byte and 1 GB.");
        if (string.IsNullOrWhiteSpace(request.MediaName) || request.MediaName.Trim().Length > 255 || request.MediaName.Any(char.IsControl))
            throw new MediaUploadException(StatusCodes.Status400BadRequest, "Media name is required and must be at most 255 characters.");
        if (request.OrganizationId == Guid.Empty || request.FacilityId == Guid.Empty)
            throw new MediaUploadException(StatusCodes.Status400BadRequest, "Organization and facility identifiers are required.");

        var facility = await _db.Facilities.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.FacilityId && x.OrganizationId == request.OrganizationId && x.IsActive, cancellationToken);
        if (facility is null) throw new MediaUploadException(StatusCodes.Status400BadRequest, "Facility does not exist, is inactive, or does not belong to the specified organization.");
        if (request.CategoryId.HasValue)
        {
            var category = await _db.MediaCategories.AsNoTracking().AnyAsync(x => x.Id == request.CategoryId && (x.OrganizationId == null || x.OrganizationId == request.OrganizationId), cancellationToken);
            if (!category) throw new MediaUploadException(StatusCodes.Status400BadRequest, "Media category is not available to the specified organization.");
        }

        var identifier = Guid.NewGuid();
        var directory = GetSessionDirectory(identifier);
        var expiresAt = DateTimeOffset.UtcNow.Add(_sessionLifetime);
        var chunks = checked((int)((request.FileSize + _chunkSize - 1) / _chunkSize));
        var metadata = new UploadMetadata(request.MediaName.Trim(), request.OrganizationId, request.FacilityId, request.CategoryId);
        try
        {
            Directory.CreateDirectory(directory);
            await WriteMetadataAsync(directory, metadata, cancellationToken);
            _db.MediaUploadSessions.Add(new MediaUploadSession
            {
                UploadIdentifier = identifier.ToString("N"), Status = UploadingStatus, ExpectedFileName = fileName,
                ExpectedFileSize = request.FileSize, ChunkSize = _chunkSize, TotalChunks = chunks,
                ReceivedChunksBitmap = new byte[(chunks + 7) / 8], TempFilePath = directory, ExpiresAt = expiresAt
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            TryDeleteDirectory(directory);
            if (ex is IOException or UnauthorizedAccessException or DbUpdateException)
                throw new MediaUploadException(StatusCodes.Status500InternalServerError, "Upload session could not be initialized.");
            throw;
        }

        return new InitializeMediaUploadResponseDto { UploadIdentifier = identifier.ToString("N"), ChunkSize = _chunkSize, TotalChunks = chunks, ExpiresAt = expiresAt };
    }

    public async Task<MediaUploadStatusResponseDto> UploadChunkAsync(string uploadIdentifier, int chunkIndex, UploadMediaChunkRequestDto request, CancellationToken cancellationToken)
    {
        var id = ParseIdentifier(uploadIdentifier);
        using var sessionLock = await AcquireSessionLockAsync(id, cancellationToken);
        try
        {
            var session = await LoadSessionAsync(id, cancellationToken);
            var entry = _db.Entry(session);

            Console.WriteLine(
                $"[UploadDebug] State={entry.State}, " +
                $"Bitmap={Convert.ToHexString(session.ReceivedChunksBitmap)}");
            await ExpireIfNecessaryAsync(session, cancellationToken);
            EnsureStatus(session, UploadingStatus);
            if (chunkIndex < 0 || chunkIndex >= session.TotalChunks) throw new MediaUploadException(StatusCodes.Status400BadRequest, "Chunk index is outside the expected range.");
            var expectedLength = (int)Math.Min(session.ChunkSize, session.ExpectedFileSize - (long)chunkIndex * session.ChunkSize);
            if (request.Chunk is null || request.Chunk.Length != expectedLength)
                throw new MediaUploadException(StatusCodes.Status400BadRequest, "Chunk length does not match the expected chunk size.");

            var directory = GetSessionDirectory(id);
            Directory.CreateDirectory(directory);
            var chunkPath = Path.Combine(directory, $"chunk-{chunkIndex:D8}.part");
            var candidatePath = Path.Combine(directory, $"candidate-{Guid.NewGuid():N}.part");
            try
            {
                await WriteChunkAsync(request.Chunk, candidatePath, expectedLength, cancellationToken);
                if (IsChunkReceived(session, chunkIndex) || File.Exists(chunkPath))
                {
                    if (!File.Exists(chunkPath) || !await FilesEqualAsync(chunkPath, candidatePath, cancellationToken))
                        throw new MediaUploadException(StatusCodes.Status409Conflict, "A different chunk was already accepted at this index.");
                    if (!IsChunkReceived(session, chunkIndex))
                    {
                        SetChunkReceived(session, chunkIndex);
                        await _db.SaveChangesAsync(cancellationToken);

                    }
                    return ToStatus(session);
                }

                File.Move(candidatePath, chunkPath);
                SetChunkReceived(session, chunkIndex);
                _db.Entry(session)
                    .Property(x => x.ReceivedChunksBitmap)
                    .IsModified = true;
                try { await _db.SaveChangesAsync(cancellationToken);}
                catch { TryDeleteFile(chunkPath); throw; }
                return ToStatus(session);
            }
            finally { TryDeleteFile(candidatePath); }
        }
        catch (MediaUploadException) { throw; }
        catch (IOException) { throw new MediaUploadException(StatusCodes.Status500InternalServerError, "Chunk could not be stored; retry the request."); }
        catch (DbUpdateException) { throw new MediaUploadException(StatusCodes.Status500InternalServerError, "Chunk progress could not be saved; retry the request."); }
    }

    public async Task<MediaUploadStatusResponseDto> GetStatusAsync(string uploadIdentifier, CancellationToken cancellationToken)
    {
        var id = ParseIdentifier(uploadIdentifier);
        using var sessionLock = await AcquireSessionLockAsync(id, cancellationToken);
        var session = await LoadSessionAsync(id, cancellationToken);
        await ExpireIfNecessaryAsync(session, cancellationToken);
        return ToStatus(session);
    }

    public async Task<CompleteMediaUploadResponseDto> CompleteAsync(string uploadIdentifier, CancellationToken cancellationToken)
    {
        var id = ParseIdentifier(uploadIdentifier);
        using var sessionLock = await AcquireSessionLockAsync(id, cancellationToken);
        string? publishedPath = null;
        try
        {
            var session = await LoadSessionAsync(id, cancellationToken);
            await ExpireIfNecessaryAsync(session, cancellationToken);
            if (session.Status == CompletedStatus)
                return new CompleteMediaUploadResponseDto { MediaId = session.MediaId ?? Guid.Empty, FileSize = session.ExpectedFileSize, ChecksumMd5 = session.ChecksumMd5 ?? string.Empty, ChecksumSha256 = session.ChecksumSha256 ?? string.Empty };
            if (session.Status is not (UploadingStatus or FinalizingStatus)) EnsureStatus(session, UploadingStatus);
            if (CountReceivedChunks(session) != session.TotalChunks)
                throw new MediaUploadException(StatusCodes.Status409Conflict, "Upload is incomplete; all chunks must be received before finalization.");

            var metadata = await ReadMetadataAsync(GetSessionDirectory(id), cancellationToken);
            var directory = GetSessionDirectory(id);
            var assemblingPath = Path.Combine(directory, $"assembling-{Guid.NewGuid():N}.tmp");
            CleanupAssemblyFiles(directory);
            if (session.Status == UploadingStatus)
            {
                session.Status = FinalizingStatus;
                await _db.SaveChangesAsync(cancellationToken);
            }
            string md5;
            string sha256;
            try
            {
                (md5, sha256) = await AssembleAndHashAsync(session, directory, assemblingPath, cancellationToken);
                // Stable for this session so an interrupted finalization can safely retry its own destination.
                var mediaId = session.Id;
                var extension = Path.GetExtension(session.ExpectedFileName).ToLowerInvariant();
                var publishedDirectory = Path.Combine(_storageRoot, "media", metadata.OrganizationId.ToString("N"), metadata.FacilityId.ToString("N"));
                var relativeFile = $"media/{metadata.OrganizationId:N}/{metadata.FacilityId:N}/{mediaId:N}{extension}";
                publishedPath = Path.Combine(publishedDirectory, $"{mediaId:N}{extension}");
                Directory.CreateDirectory(Path.GetDirectoryName(publishedPath)!);
                if (File.Exists(publishedPath)) TryDeleteFile(publishedPath);
                File.Move(assemblingPath, publishedPath);

                var media = new Media
                {
                    Id = mediaId,
                    MediaName = metadata.MediaName,
                    FileName = session.ExpectedFileName,
                    FilePath = relativeFile,
                    FileType = GetMediaType(extension),
                    FileSize = session.ExpectedFileSize,
                    SizeInBytes = session.ExpectedFileSize,
                    ChecksumMd5 = md5,
                    ChecksumSha256 = sha256,
                    ApprovalStatus = "Chờ duyệt",
                    OrganizationId = metadata.OrganizationId,
                    FacilityId = metadata.FacilityId,
                    CategoryId = metadata.CategoryId
                };
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                _db.Media.Add(media);
                session.MediaId = mediaId;
                session.ChecksumMd5 = md5;
                session.ChecksumSha256 = sha256;
                session.TempFilePath = relativeFile;
                session.Status = CompletedStatus;
                session.CompletedAt = DateTimeOffset.UtcNow;
                session.FailedReason = null;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TryDeleteDirectory(directory);
                return new CompleteMediaUploadResponseDto { MediaId = mediaId, FileSize = session.ExpectedFileSize, ChecksumMd5 = md5, ChecksumSha256 = sha256 };
            }
            catch (MediaUploadException)
            {
                TryDeleteFile(assemblingPath);
                if (publishedPath is not null) TryDeleteFile(publishedPath);
                session.Status = FailedStatus;
                session.FailedReason = "File verification failed; start a new upload.";
                try { await _db.SaveChangesAsync(CancellationToken.None); } catch { /* Preserve original failure; status may need operator reconciliation. */ }
                TryDeleteDirectory(directory);
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException)
            {
                TryDeleteFile(assemblingPath);
                if (publishedPath is not null) TryDeleteFile(publishedPath);
                _db.ChangeTracker.Clear();
                try
                {
                    var failedSession = await LoadSessionAsync(id, CancellationToken.None);
                    if (failedSession.Status != CompletedStatus)
                    {
                        failedSession.Status = FailedStatus;
                        failedSession.FailedReason = "Finalization or persistence failed; start a new upload.";
                        await _db.SaveChangesAsync(CancellationToken.None);
                        TryDeleteDirectory(directory);
                    }
                }

                catch { /* Preserve the original failure; status may require operator reconciliation. */ }
                throw new MediaUploadException(StatusCodes.Status500InternalServerError, "Upload finalization failed.");
            }
        }
        catch
        {
            throw;
        }
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredIds = await _db.MediaUploadSessions.AsNoTracking()
            .Where(x => (x.Status == UploadingStatus || x.Status == FinalizingStatus) && x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt).Select(x => x.UploadIdentifier).Take(100).ToListAsync(ct);

        foreach (var uploadIdentifier in expiredIds)
        {
            if (!Guid.TryParseExact(uploadIdentifier, "N", out var id)) continue;
            using var sessionLock = await AcquireSessionLockAsync(id, ct);
            var session = await _db.MediaUploadSessions.SingleOrDefaultAsync(x => x.UploadIdentifier == uploadIdentifier, ct);
            if (session is null || session.Status is not (UploadingStatus or FinalizingStatus) || session.ExpiresAt > DateTimeOffset.UtcNow) continue;
            session.Status = ExpiredStatus;
            session.FailedReason = "Upload session expired.";
            await _db.SaveChangesAsync(ct);
            TryDeleteDirectory(GetSessionDirectory(id));
        }
    }

    private static async Task<SessionLockLease> AcquireSessionLockAsync(Guid id, CancellationToken cancellationToken)
    {
        SessionLockEntry entry;
        lock (SessionLockSync)
        {
            if (!SessionLocks.TryGetValue(id, out entry!))
                SessionLocks[id] = entry = new SessionLockEntry();
            entry.References++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            return new SessionLockLease(id, entry);
        }
        catch
        {
            ReleaseSessionLockReference(id, entry, releaseSemaphore: false);
            throw;
        }
    }

    private static void ReleaseSessionLockReference(Guid id, SessionLockEntry entry, bool releaseSemaphore)
    {
        if (releaseSemaphore) entry.Semaphore.Release();
        lock (SessionLockSync)
        {
            entry.References--;
            if (entry.References == 0)
            {
                SessionLocks.Remove(id);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class SessionLockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int References { get; set; }
    }

    private sealed class SessionLockLease(Guid id, SessionLockEntry entry) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseSessionLockReference(id, entry, releaseSemaphore: true);
        }
    }

    private async Task<MediaUploadSession> LoadSessionAsync(Guid id, CancellationToken ct)
    {
        var session = await _db.MediaUploadSessions.SingleOrDefaultAsync(x => x.UploadIdentifier == id.ToString("N"), ct);
        return session ?? throw new MediaUploadException(StatusCodes.Status404NotFound, "Upload session was not found.");
    }

    private async Task ExpireIfNecessaryAsync(MediaUploadSession session, CancellationToken ct)
    {
        if ((session.Status is UploadingStatus or FinalizingStatus) && session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            session.Status = ExpiredStatus;
            session.FailedReason = "Upload session expired.";
            await _db.SaveChangesAsync(ct);
            TryDeleteDirectory(GetSessionDirectory(Guid.ParseExact(session.UploadIdentifier, "N")));
            throw new MediaUploadException(StatusCodes.Status410Gone, "Upload session has expired.");
        }
    }

    private string GetSessionDirectory(Guid id) => Path.Combine(_storageRoot, "sessions", id.ToString("N"));
    private static Guid ParseIdentifier(string value) => Guid.TryParseExact(value, "N", out var id)
        ? id : throw new MediaUploadException(StatusCodes.Status400BadRequest, "Upload identifier is invalid.");
    private static void EnsureStatus(MediaUploadSession session, string expected)
    {
        if (session.Status != expected) throw new MediaUploadException(StatusCodes.Status409Conflict, $"Upload session is {session.Status} and cannot accept this operation.");
    }
    private static bool IsChunkReceived(MediaUploadSession session, int index) => (session.ReceivedChunksBitmap[index / 8] & (1 << (index % 8))) != 0;
    private static void SetChunkReceived(MediaUploadSession session, int index) => session.ReceivedChunksBitmap[index / 8] |= (byte)(1 << (index % 8));
    private static int CountReceivedChunks(MediaUploadSession session)
    {
        var count = 0;
        for (var i = 0; i < session.TotalChunks; i++) if (IsChunkReceived(session, i)) count++;
        return count;
    }
    private static MediaUploadStatusResponseDto ToStatus(MediaUploadSession session)
    {
        var received = CountReceivedChunks(session);
        return new MediaUploadStatusResponseDto
        {
            UploadIdentifier = session.UploadIdentifier, Status = session.Status, ReceivedChunks = received,
            TotalChunks = session.TotalChunks, ProgressPercentage = session.TotalChunks == 0 ? 0 : (double)received / session.TotalChunks * 100,
            ExpiresAt = session.ExpiresAt, FailureReason = session.FailedReason, MediaId = session.MediaId
        };
    }

    private static async Task WriteChunkAsync(IFormFile chunk, string destination, int expectedLength, CancellationToken ct)
    {
        await using var input = chunk.OpenReadStream();
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[81920];
        var total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > expectedLength) throw new MediaUploadException(StatusCodes.Status400BadRequest, "Chunk exceeds its permitted length.");
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        if (total != expectedLength) throw new MediaUploadException(StatusCodes.Status400BadRequest, "Chunk length does not match the expected length.");
    }

    private static async Task<bool> FilesEqualAsync(string first, string second, CancellationToken ct)
    {
        await using var a = File.OpenRead(first);
        await using var b = File.OpenRead(second);
        if (a.Length != b.Length) return false;
        var left = new byte[81920]; var right = new byte[81920];
        while (true)
        {
            var readA = await a.ReadAsync(left, ct); var readB = await b.ReadAsync(right, ct);
            if (readA != readB) return false;
            if (readA == 0) return true;
            if (!left.AsSpan(0, readA).SequenceEqual(right.AsSpan(0, readB))) return false;
        }
    }

    private static async Task<(string Md5, string Sha256)> AssembleAndHashAsync(MediaUploadSession session, string directory, string destination, CancellationToken ct)
    {
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[81920];
        long total = 0;
        for (var i = 0; i < session.TotalChunks; i++)
        {
            var path = Path.Combine(directory, $"chunk-{i:D8}.part");
            if (!File.Exists(path)) throw new MediaUploadException(StatusCodes.Status409Conflict, "A required chunk is missing from temporary storage.");
            await using var input = File.OpenRead(path);
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                md5.AppendData(buffer, 0, read); sha.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
            }
        }
        await output.FlushAsync(ct);
        if (total != session.ExpectedFileSize)
            throw new MediaUploadException(StatusCodes.Status400BadRequest, "Assembled file size does not match the declared size.");
        return (Convert.ToHexString(md5.GetHashAndReset()).ToLowerInvariant(), Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant());
    }

    private static async Task WriteMetadataAsync(string directory, UploadMetadata metadata, CancellationToken ct)
    {
        var path = Path.Combine(directory, "metadata.json");
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, metadata, cancellationToken: ct);
    }
    private static async Task<UploadMetadata> ReadMetadataAsync(string directory, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(Path.Combine(directory, "metadata.json"));
            return await JsonSerializer.DeserializeAsync<UploadMetadata>(stream, cancellationToken: ct)
                ?? throw new JsonException();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new MediaUploadException(StatusCodes.Status500InternalServerError, "Upload metadata is unavailable; the session requires operator recovery.");
        }
    }
    private static string GetMediaType(string extension) => extension switch
    {
        ".mp4" => "Video", ".mp3" or ".wav" => "Audio", ".png" or ".jpg" or ".jpeg" => "Image",
        _ => throw new MediaUploadException(StatusCodes.Status400BadRequest, "File type is not supported.")
    };
    private static void CleanupAssemblyFiles(string directory)
    {
        try { foreach (var file in Directory.EnumerateFiles(directory, "assembling-*.tmp")) TryDeleteFile(file); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private static void TryDeleteDirectory(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    private static void TryDeleteFile(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    private sealed record UploadMetadata(string MediaName, Guid OrganizationId, Guid FacilityId, Guid? CategoryId);
}