using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Models;

namespace MTC_System.Services.Implements;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface IHlsProcessRunner
{
    Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class HlsProcessRunner : IHlsProcessRunner
{
    private const int MaxCapturedCharacters = 64 * 1024;

    public async Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start()) throw new InvalidOperationException("The configured media processing executable did not start.");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException("Unable to start the configured media processing executable. Check MediaProcessing executable paths and permissions.", ex);
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var stdoutTask = ReadBoundedAsync(process.StandardOutput, linked.Token);
        var stderrTask = ReadBoundedAsync(process.StandardError, linked.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            try { await process.WaitForExitAsync(CancellationToken.None); } catch (InvalidOperationException) { }
            await ObserveReaderExitAsync(stdoutTask);
            await ObserveReaderExitAsync(stderrTask);
            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException("Media processing exceeded its configured timeout.");
            throw;
        }
        return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        var output = new StringBuilder();
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (output.Length < MaxCapturedCharacters)
                output.Append(buffer, 0, Math.Min(read, MaxCapturedCharacters - output.Length));
        }
        return output.ToString();
    }

    private static async Task ObserveReaderExitAsync(Task<string> readerTask)
    {
        try { await readerTask; }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException) { }
    }
}

public sealed class HlsProcessingService
{
    private static readonly Regex SegmentName = new("^segment_[0-9]{5,8}\\.ts$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly MyDbContext _db;
    private readonly IHlsProcessRunner _runner;
    private readonly ILogger<HlsProcessingService> _logger;
    private readonly string _storageRoot;
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private readonly TimeSpan _timeout;
    private readonly int _segmentDuration;

    public HlsProcessingService(MyDbContext db, IHlsProcessRunner runner, IConfiguration configuration, IWebHostEnvironment environment, ILogger<HlsProcessingService> logger)
    {
        _db = db; _runner = runner; _logger = logger;
        _storageRoot = MediaStoragePathResolver.ResolveStorageRoot(configuration, environment);
        _ffmpegPath = configuration["MediaProcessing:FfmpegPath"] ?? "ffmpeg";
        _ffprobePath = configuration["MediaProcessing:FfprobePath"] ?? "ffprobe";
        _timeout = TimeSpan.FromSeconds(ReadBoundedInt(configuration["MediaProcessing:TimeoutSeconds"], 3600, 30, 86400));
        _segmentDuration = ReadBoundedInt(configuration["MediaProcessing:SegmentDurationSeconds"], 6, 2, 30);
        _logger.LogInformation(
            "HLS-DIAGNOSTIC Processor configured FfmpegPath={FfmpegPath} FfprobePath={FfprobePath} StorageRoot={StorageRoot} TimeoutSeconds={TimeoutSeconds} SegmentDurationSeconds={SegmentDurationSeconds}",
            _ffmpegPath, _ffprobePath, _storageRoot, _timeout.TotalSeconds, _segmentDuration);
    }

    public async Task ProcessAsync(Guid mediaId, Guid attemptId, CancellationToken cancellationToken)
    {
        var media = await _db.Media.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mediaId && !x.IsDeleted &&
            x.HlsStatus == "Processing" && x.HlsProcessingAttemptId == attemptId, cancellationToken);
        if (media is null) return;
        _logger.LogInformation("HLS-DIAGNOSTIC Attempt started MediaId={MediaId} AttemptId={AttemptId}", mediaId, attemptId);
        string? staging = null;
        var deadlineAt = DateTimeOffset.UtcNow + _timeout;
        using var deadline = new CancellationTokenSource(_timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var processingToken = linked.Token;
        try
        {
            var input = MediaStoragePathResolver.ResolveUnderRoot(_storageRoot, media.FilePath);
            if (!File.Exists(input)) throw new InvalidOperationException("Original media file is missing.");
            staging = MediaStoragePathResolver.ResolveUnderRoot(_storageRoot,
                Path.Combine("hls-tmp", media.Id.ToString("N"), attemptId.ToString("N")));
            Directory.CreateDirectory(staging);
            var probe = await _runner.RunAsync(_ffprobePath,
                new[] { "-v", "error", "-show_entries", "stream=codec_type", "-of", "csv=p=0", input }, _timeout, processingToken);
            processingToken.ThrowIfCancellationRequested();
            if (probe.ExitCode != 0)
            {
                _logger.LogWarning("FFprobe failed for media {MediaId} (exit {ExitCode}): {Diagnostics}", mediaId, probe.ExitCode, Limit(probe.StandardError));
                throw new InvalidDataException("ffprobe could not inspect the uploaded media.");
            }
            var hasVideo = probe.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.Trim() == "video");
            var hasAudio = probe.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(x => x.Trim() == "audio");
            if (!hasVideo && !hasAudio) throw new InvalidDataException("Uploaded media has no supported audio or video stream.");

            var playlist = Path.Combine(staging, "index.m3u8");
            var segmentPattern = Path.Combine(staging, "segment_%05d.ts");
            var args = new List<string> { "-hide_banner", "-nostdin", "-y", "-i", input };
            if (hasVideo)
            {
                args.AddRange(new[] { "-map", "0:v:0", "-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p" });
                if (hasAudio) args.AddRange(new[] { "-map", "0:a:0?", "-c:a", "aac", "-b:a", "128k" });
                else args.Add("-an");
            }
            else args.AddRange(new[] { "-map", "0:a:0", "-vn", "-c:a", "aac", "-b:a", "128k" });
            args.AddRange(new[] { "-f", "hls", "-hls_time", _segmentDuration.ToString(), "-hls_playlist_type", "vod", "-hls_flags", "temp_file", "-hls_segment_filename", segmentPattern, playlist });
            var result = await _runner.RunAsync(_ffmpegPath, args, RemainingTime(deadlineAt), processingToken);
            processingToken.ThrowIfCancellationRequested();
            if (result.ExitCode != 0)
            {
                _logger.LogWarning("FFmpeg failed for media {MediaId} (exit {ExitCode}): {Diagnostics}", mediaId, result.ExitCode, Limit(result.StandardError));
                throw new InvalidOperationException("FFmpeg conversion failed.");
            }

            var segments = ValidateOutput(staging, playlist);
            processingToken.ThrowIfCancellationRequested();
            var finalRelative = Path.Combine("hls", media.Id.ToString("N"), attemptId.ToString("N")).Replace('\\', '/');
            var finalDirectory = MediaStoragePathResolver.ResolveUnderRoot(_storageRoot, finalRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(finalDirectory)!);
            Directory.Move(staging, finalDirectory);
            staging = null;
            var attemptOutput = finalDirectory;
            var outputSize = segments.Sum(x => new FileInfo(Path.Combine(finalDirectory, x)).Length) + new FileInfo(Path.Combine(finalDirectory, "index.m3u8")).Length;
            var published = await _db.Media.Where(x => x.Id == mediaId && !x.IsDeleted && x.HlsStatus == "Processing" &&
                    x.HlsProcessingAttemptId == attemptId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.HlsRelativePath, $"{finalRelative}/index.m3u8")
                    .SetProperty(x => x.HlsOutputSizeBytes, outputSize)
                    .SetProperty(x => x.HlsStatus, "Ready")
                    .SetProperty(x => x.HlsProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.HlsProcessingAttemptId, (Guid?)null)
                    .SetProperty(x => x.HlsFailureCode, (string?)null), CancellationToken.None);
            if (published != 1)
            {
                TryDeleteDirectory(attemptOutput);
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDeleteDirectory(staging);
            await _db.Media.Where(x => x.Id == mediaId && !x.IsDeleted && x.HlsStatus == "Processing" && x.HlsProcessingAttemptId == attemptId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.HlsStatus, "Pending")
                    .SetProperty(x => x.HlsProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.HlsProcessingAttemptId, (Guid?)null), CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            await PersistFailureAsync(mediaId, attemptId, "Timeout", ex, staging);
        }
        catch (Exception ex)
        {
            var failureCode = ex switch { TimeoutException => "Timeout", InvalidDataException => "InvalidMedia", _ => "ProcessingFailed" };
            await PersistFailureAsync(mediaId, attemptId, failureCode, ex, staging);
        }
    }

    private async Task PersistFailureAsync(Guid mediaId, Guid attemptId, string failureCode, Exception exception, string? staging)
    {
        TryDeleteDirectory(staging);
        _logger.LogWarning(exception, "HLS processing failed for media {MediaId}, attempt {AttemptId}.", mediaId, attemptId);
        var changed = await _db.Media.Where(x => x.Id == mediaId && !x.IsDeleted && x.HlsStatus == "Processing" &&
                x.HlsProcessingAttemptId == attemptId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.HlsStatus, "Failed")
                .SetProperty(x => x.HlsProcessingStartedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.HlsProcessingAttemptId, (Guid?)null)
                .SetProperty(x => x.HlsFailureCode, failureCode), CancellationToken.None);
        if (changed == 0)
            _logger.LogInformation("Discarded stale HLS failure for media {MediaId}, attempt {AttemptId}.", mediaId, attemptId);
    }

    private static TimeSpan RemainingTime(DateTimeOffset deadlineAt)
    {
        var remaining = deadlineAt - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(1);
    }

    private static string[] ValidateOutput(string directory, string playlistPath)
    {
        if (!File.Exists(playlistPath)) throw new InvalidDataException("FFmpeg did not produce an HLS playlist.");
        if (new FileInfo(playlistPath).Length > 8 * 1024 * 1024) throw new InvalidDataException("The generated HLS playlist exceeds the validation limit.");
        var segments = new List<string>();
        var hasEndList = false;
        foreach (var line in File.ReadLines(playlistPath))
        {
            if (line == "#EXT-X-ENDLIST") hasEndList = true;
            if (!line.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)) continue;
            if (segments.Count >= 100_000 || !SegmentName.IsMatch(line)) throw new InvalidDataException("The generated HLS playlist contains an invalid segment reference.");
            var segmentPath = Path.Combine(directory, line);
            if (!File.Exists(segmentPath) || new FileInfo(segmentPath).Length == 0) throw new InvalidDataException("The HLS playlist references a missing segment.");
            segments.Add(line);
        }
        if (!hasEndList) throw new InvalidDataException("The HLS playlist is not a complete VOD playlist.");
        if (segments.Count == 0) throw new InvalidDataException("The HLS playlist does not contain segments.");
        return segments.ToArray();
    }

    private static int ReadBoundedInt(string? value, int fallback, int min, int max)
    {
        var parsed = int.TryParse(value, out var configured) ? configured : fallback;
        if (parsed < min || parsed > max) throw new InvalidOperationException($"MediaProcessing setting must be between {min} and {max}.");
        return parsed;
    }
    private void TryDeleteDirectory(string? directory)
    {
        if (directory is null || !Directory.Exists(directory)) return;
        try
        {
            Directory.Delete(directory, recursive: true);
            var parent = Directory.GetParent(directory)?.FullName;
            for (var level = 0; level < 2 && parent is not null && Directory.Exists(parent) &&
                 !Directory.EnumerateFileSystemEntries(parent).Any(); level++)
            {
                Directory.Delete(parent);
                parent = Directory.GetParent(parent)?.FullName;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _logger.LogWarning(ex, "Could not remove partial HLS output for directory {Directory}.", directory); }
    }
    private static string Limit(string? text) => string.IsNullOrEmpty(text) ? string.Empty : text[..Math.Min(text.Length, 4096)];
}

public sealed class HlsProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HlsProcessingWorker> _logger;
    private readonly int _concurrency;
    private readonly TimeSpan _timeout;
    public HlsProcessingWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<HlsProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory; _logger = logger;
        _concurrency = int.TryParse(configuration["MediaProcessing:Concurrency"], out var value) ? Math.Clamp(value, 1, 4) : 1;
        var seconds = int.TryParse(configuration["MediaProcessing:TimeoutSeconds"], out var timeout) ? Math.Clamp(timeout, 30, 86400) : 3600;
        _timeout = TimeSpan.FromSeconds(seconds);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "HLS worker polling failed."); }
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var staleBefore = DateTimeOffset.UtcNow - _timeout - TimeSpan.FromMinutes(5);
        await db.Media.Where(x => !x.IsDeleted && x.HlsStatus == "Processing" && (!x.HlsProcessingStartedAt.HasValue || x.HlsProcessingStartedAt < staleBefore))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.HlsStatus, "Pending")
                .SetProperty(x => x.HlsProcessingStartedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.HlsProcessingAttemptId, (Guid?)null), ct);
        var ids = await db.Media.AsNoTracking().Where(x => !x.IsDeleted && x.HlsStatus == "Pending").OrderBy(x => x.CreatedAt).Select(x => x.Id).Take(_concurrency).ToListAsync(ct);
        var claimedIds = new List<(Guid Id, Guid AttemptId)>();
        foreach (var id in ids)
        {
            var started = DateTimeOffset.UtcNow;
            var attemptId = Guid.NewGuid();
            var claimed = await db.Media.Where(x => x.Id == id && !x.IsDeleted && x.HlsStatus == "Pending")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.HlsStatus, "Processing")
                    .SetProperty(x => x.HlsProcessingStartedAt, started)
                    .SetProperty(x => x.HlsProcessingAttemptId, attemptId), ct);
            if (claimed == 1) claimedIds.Add((id, attemptId));
        }
        await Task.WhenAll(claimedIds.Select(async claim =>
        {
            await using var jobScope = _scopeFactory.CreateAsyncScope();
            var processor = jobScope.ServiceProvider.GetRequiredService<HlsProcessingService>();
            await processor.ProcessAsync(claim.Id, claim.AttemptId, ct);
        }));
    }
}
