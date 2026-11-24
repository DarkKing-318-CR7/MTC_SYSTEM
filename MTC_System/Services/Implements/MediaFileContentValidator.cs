using System.Buffers.Binary;
using System.Text;
using MTC_System.Common;
using Microsoft.AspNetCore.Http;

namespace MTC_System.Services.Implements;

internal static class MediaFileContentValidator
{
    private const int BufferSize = 81920;
    // MP4 accepts first-box ftyp ISO-BMFF files with a supported brand, mvhd, an audio/video track, and non-empty mdat.
    // MP3 accepts consecutive MPEG Layer III frames (MPEG-1, MPEG-2, or MPEG-2.5), optionally preceded by ID3v2.2-2.4.
    private static readonly HashSet<string> Mp4Brands = new(StringComparer.Ordinal)
    {
        "isom", "iso2", "mp41", "mp42", "M4V ", "M4A ", "avc1", "dash", "MSNV", "3gp4", "3gp5"
    };
    private static readonly uint[] PngCrcTable = CreatePngCrcTable();

    public static async Task ValidateAsync(string extension, string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var valid = extension.ToLowerInvariant() switch
        {
            ".mp4" => await IsMp4Async(stream, cancellationToken),
            ".mp3" => await IsMp3Async(stream, cancellationToken),
            ".wav" => await IsWaveAsync(stream, cancellationToken),
            ".png" => await IsPngAsync(stream, cancellationToken),
            ".jpg" or ".jpeg" => await IsJpegAsync(stream, cancellationToken),
            _ => false
        };

        if (!valid)
            throw new MediaUploadException(StatusCodes.Status400BadRequest, "File content is malformed or does not match its supported extension.");
    }

    private static async Task<bool> IsMp4Async(FileStream stream, CancellationToken ct)
    {
        if (stream.Length < 24) return false;
        var first = await ReadBoxHeaderAsync(stream, stream.Length, ct);
        if (first is null || first.Value.Type != "ftyp" || !await HasSupportedFtypAsync(stream, first.Value, ct)) return false;

        var hasMovie = false;
        var hasMediaData = false;
        while (stream.Position < stream.Length)
        {
            var box = await ReadBoxHeaderAsync(stream, stream.Length, ct);
            if (box is null) return false;
            if (box.Value.Type == "moov")
            {
                if (hasMovie || !await HasMovieTrackAsync(stream, box.Value, ct)) return false;
                hasMovie = true;
            }
            else if (box.Value.Type == "mdat" && box.Value.End > box.Value.PayloadStart)
            {
                hasMediaData = true;
            }
            stream.Position = box.Value.End;
        }
        return stream.Position == stream.Length && hasMovie && hasMediaData;
    }

    private static async Task<bool> HasSupportedFtypAsync(FileStream stream, MediaBox box, CancellationToken ct)
    {
        if (box.End - box.PayloadStart < 8 || (box.End - box.PayloadStart - 8) % 4 != 0) return false;
        stream.Position = box.PayloadStart;
        var fields = new byte[8];
        if (!await TryReadExactlyAsync(stream, fields, ct)) return false;
        var supported = IsSupportedMp4Brand(Encoding.ASCII.GetString(fields, 0, 4));
        while (stream.Position < box.End)
        {
            var brand = new byte[4];
            if (!await TryReadExactlyAsync(stream, brand, ct)) return false;
            supported |= IsSupportedMp4Brand(Encoding.ASCII.GetString(brand));
        }
        return supported;
    }

    private static bool IsSupportedMp4Brand(string brand) => Mp4Brands.Contains(brand);

    private static async Task<bool> HasMovieTrackAsync(FileStream stream, MediaBox moov, CancellationToken ct)
    {
        var hasMovieHeader = false;
        var hasMediaTrack = false;
        stream.Position = moov.PayloadStart;
        while (stream.Position < moov.End)
        {
            var box = await ReadBoxHeaderAsync(stream, moov.End, ct);
            if (box is null) return false;
            if (box.Value.Type == "mvhd")
            {
                if (hasMovieHeader || !await HasValidMovieHeaderAsync(stream, box.Value, ct)) return false;
                hasMovieHeader = true;
            }
            else if (box.Value.Type == "trak" && await HasMediaTrackAsync(stream, box.Value, ct))
            {
                hasMediaTrack = true;
            }
            stream.Position = box.Value.End;
        }
        return stream.Position == moov.End && hasMovieHeader && hasMediaTrack;
    }

    private static async Task<bool> HasValidMovieHeaderAsync(FileStream stream, MediaBox box, CancellationToken ct)
    {
        var payloadLength = box.End - box.PayloadStart;
        if (payloadLength < 4) return false;
        stream.Position = box.PayloadStart;
        var version = new byte[1];
        if (!await TryReadExactlyAsync(stream, version, ct)) return false;
        return version[0] switch
        {
            0 => payloadLength >= 100,
            1 => payloadLength >= 112,
            _ => false
        };
    }

    private static async Task<bool> HasMediaTrackAsync(FileStream stream, MediaBox track, CancellationToken ct)
    {
        stream.Position = track.PayloadStart;
        while (stream.Position < track.End)
        {
            var box = await ReadBoxHeaderAsync(stream, track.End, ct);
            if (box is null) return false;
            if (box.Value.Type == "mdia" && await HasAudioOrVideoHandlerAsync(stream, box.Value, ct)) return true;
            stream.Position = box.Value.End;
        }
        return stream.Position == track.End && false;
    }

    private static async Task<bool> HasAudioOrVideoHandlerAsync(FileStream stream, MediaBox media, CancellationToken ct)
    {
        stream.Position = media.PayloadStart;
        while (stream.Position < media.End)
        {
            var box = await ReadBoxHeaderAsync(stream, media.End, ct);
            if (box is null) return false;
            if (box.Value.Type == "hdlr")
            {
                if (box.Value.End - box.Value.PayloadStart < 12) return false;
                stream.Position = box.Value.PayloadStart + 8;
                var handler = new byte[4];
                if (!await TryReadExactlyAsync(stream, handler, ct)) return false;
                var type = Encoding.ASCII.GetString(handler);
                return type is "vide" or "soun";
            }
            stream.Position = box.Value.End;
        }
        return false;
    }

    private static async Task<MediaBox?> ReadBoxHeaderAsync(FileStream stream, long parentEnd, CancellationToken ct)
    {
        var start = stream.Position;
        if (parentEnd - start < 8) return null;
        var header = new byte[16];
        if (!await TryReadExactlyAsync(stream, header.AsMemory(0, 8), ct)) return null;
        var size32 = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));
        var type = Encoding.ASCII.GetString(header, 4, 4);
        long headerLength = 8;
        ulong size = size32;
        if (size32 == 1)
        {
            if (parentEnd - start < 16 || !await TryReadExactlyAsync(stream, header.AsMemory(8, 8), ct)) return null;
            size = BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(8, 8));
            headerLength = 16;
        }
        else if (size32 == 0)
        {
            size = checked((ulong)(parentEnd - start));
        }
        if (size < (ulong)headerLength || size > (ulong)(parentEnd - start) || size > long.MaxValue) return null;
        var end = checked(start + (long)size);
        return new MediaBox(type, start, start + headerLength, end);
    }

    private static async Task<bool> IsMp3Async(FileStream stream, CancellationToken ct)
    {
        if (stream.Length < 8) return false;
        stream.Position = 0;
        var prefix = new byte[3];
        if (!await TryReadExactlyAsync(stream, prefix, ct)) return false;
        stream.Position = 0;
        if (prefix.AsSpan().SequenceEqual("ID3"u8))
        {
            if (stream.Length < 10) return false;
            var tag = new byte[10];
            if (!await TryReadExactlyAsync(stream, tag, ct)) return false;
            var major = tag[3];
            var allowedFlags = major switch { 2 => 0xC0, 3 => 0xE0, 4 => 0xF0, _ => -1 };
            if (allowedFlags < 0 || tag[4] == 0xFF || (tag[5] & ~allowedFlags) != 0) return false;
            if ((tag[6] | tag[7] | tag[8] | tag[9]) >= 0x80) return false;
            var tagSize = (long)((tag[6] << 21) | (tag[7] << 14) | (tag[8] << 7) | tag[9]);
            var hasFooter = major == 4 && (tag[5] & 0x10) != 0;
            var tagEnd = 10L + tagSize;
            if (tagEnd > stream.Length || hasFooter && tagSize < 10) return false;
            if (hasFooter)
            {
                stream.Position = tagEnd - 10;
                var footer = new byte[10];
                if (!await TryReadExactlyAsync(stream, footer, ct) || !footer.AsSpan(0, 3).SequenceEqual("3DI"u8) ||
                    !footer.AsSpan(3).SequenceEqual(tag.AsSpan(3))) return false;
            }
            stream.Position = tagEnd;
        }
        else
        {
            stream.Position = 0;
        }

        var first = await ReadMp3FrameAsync(stream, ct);
        if (first is null) return false;
        var secondPosition = stream.Position - 4 + first.Value.FrameLength;
        if (secondPosition + 4 > stream.Length) return false;
        stream.Position = secondPosition;
        var second = await ReadMp3FrameAsync(stream, ct);
        return second.HasValue && first.Value.Version == second.Value.Version && first.Value.SampleRate == second.Value.SampleRate;
    }

    private static async Task<Mp3Frame?> ReadMp3FrameAsync(FileStream stream, CancellationToken ct)
    {
        var header = new byte[4];
        if (!await TryReadExactlyAsync(stream, header, ct)) return null;
        var bits = BinaryPrimitives.ReadUInt32BigEndian(header);
        if ((bits & 0xFFE00000) != 0xFFE00000) return null;
        var versionBits = (int)((bits >> 19) & 0x3);
        var layerBits = (int)((bits >> 17) & 0x3);
        var bitrateIndex = (int)((bits >> 12) & 0xF);
        var sampleRateIndex = (int)((bits >> 10) & 0x3);
        if (versionBits == 1 || layerBits != 1 || bitrateIndex is 0 or 15 || sampleRateIndex == 3) return null;
        var mpeg1 = versionBits == 3;
        var bitrates = mpeg1
            ? new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 }
            : new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 };
        var baseSampleRates = new[] { 44100, 48000, 32000 };
        var sampleRate = baseSampleRates[sampleRateIndex] / (versionBits == 3 ? 1 : versionBits == 2 ? 2 : 4);
        var frameLength = (mpeg1 ? 144 : 72) * bitrates[bitrateIndex] * 1000 / sampleRate + (int)((bits >> 9) & 1);
        var frame = new Mp3Frame(versionBits, sampleRate, frameLength);
        return frameLength >= 4 && stream.Position - 4 + frameLength <= stream.Length ? frame : null;
    }

    private static async Task<bool> IsWaveAsync(FileStream stream, CancellationToken ct)
    {
        if (stream.Length < 44) return false;
        stream.Position = 0;
        var header = new byte[12];
        if (!await TryReadExactlyAsync(stream, header, ct) || !header.AsSpan(0, 4).SequenceEqual("RIFF"u8) ||
            !header.AsSpan(8, 4).SequenceEqual("WAVE"u8)) return false;
        var riffEnd = 8L + BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4));
        if (riffEnd != stream.Length) return false;

        var hasFormat = false;
        var hasData = false;
        while (stream.Position < riffEnd)
        {
            if (riffEnd - stream.Position < 8) return false;
            var chunkHeader = new byte[8];
            if (!await TryReadExactlyAsync(stream, chunkHeader, ct)) return false;
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.AsSpan(4, 4));
            var payloadStart = stream.Position;
            var payloadEnd = payloadStart + chunkLength;
            var paddedEnd = payloadEnd + (chunkLength & 1);
            if (payloadEnd < payloadStart || paddedEnd > riffEnd) return false;
            var chunkType = Encoding.ASCII.GetString(chunkHeader, 0, 4);
            if (chunkType == "fmt ")
            {
                if (hasFormat || chunkLength < 16) return false;
                var format = new byte[16];
                if (!await TryReadExactlyAsync(stream, format, ct)) return false;
                var encoding = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(0, 2));
                var channels = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(2, 2));
                var sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(format.AsSpan(4, 4));
                var byteRate = BinaryPrimitives.ReadUInt32LittleEndian(format.AsSpan(8, 4));
                var blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(12, 2));
                var bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(14, 2));
                if (encoding == 0 || channels == 0 || sampleRate == 0 || byteRate == 0 || blockAlign == 0 ||
                    (encoding is 1 or 3 && bitsPerSample == 0)) return false;
                hasFormat = true;
            }
            else if (chunkType == "data" && chunkLength > 0)
            {
                if (!hasFormat) return false;
                hasData = true;
            }
            stream.Position = paddedEnd;
        }
        return stream.Position == riffEnd && hasFormat && hasData;
    }

    private static async Task<bool> IsPngAsync(FileStream stream, CancellationToken ct)
    {
        if (stream.Length < 57) return false;
        stream.Position = 0;
        var signature = new byte[8];
        if (!await TryReadExactlyAsync(stream, signature, ct) ||
            !signature.AsSpan().SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return false;

        var buffer = new byte[BufferSize];
        var hasHeader = false;
        var indexedColor = false;
        byte indexedColorDepth = 0;
        var hasPalette = false;
        var hasImageData = false;
        while (stream.Position < stream.Length)
        {
            if (stream.Length - stream.Position < 12) return false;
            var chunkHeader = new byte[8];
            if (!await TryReadExactlyAsync(stream, chunkHeader, ct)) return false;
            var chunkLength = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader.AsSpan(0, 4));
            var dataStart = stream.Position;
            var crcPosition = dataStart + chunkLength;
            var chunkEnd = crcPosition + 4;
            if (crcPosition < dataStart || chunkEnd > stream.Length) return false;
            var type = Encoding.ASCII.GetString(chunkHeader, 4, 4);
            var crc = UpdateCrc(0xFFFFFFFF, chunkHeader.AsSpan(4, 4));

            if (!hasHeader)
            {
                if (type != "IHDR" || chunkLength != 13) return false;
                var ihdr = new byte[13];
                if (!await TryReadExactlyAsync(stream, ihdr, ct)) return false;
                var width = BinaryPrimitives.ReadUInt32BigEndian(ihdr.AsSpan(0, 4));
                var height = BinaryPrimitives.ReadUInt32BigEndian(ihdr.AsSpan(4, 4));
                if (width == 0 || height == 0 || !IsValidPngColorDepth(ihdr[9], ihdr[8]) || ihdr[10] != 0 || ihdr[11] != 0 || ihdr[12] > 1) return false;
                indexedColor = ihdr[9] == 3;
                indexedColorDepth = ihdr[8];
                crc = UpdateCrc(crc, ihdr);
                hasHeader = true;
            }
            else if (type == "IHDR")
            {
                return false;
            }
            else if (type == "PLTE")
            {
                if (hasPalette || hasImageData || chunkLength is < 3 or > 768 || chunkLength % 3 != 0 ||
                    indexedColor && chunkLength / 3 > (1U << indexedColorDepth)) return false;
                hasPalette = true;
            }
            else if (type == "IDAT" && indexedColor && !hasPalette)
            {
                return false;
            }

            var remaining = (long)chunkLength - (type == "IHDR" ? 13 : 0);
            while (remaining > 0)
            {
                var count = (int)Math.Min(buffer.Length, remaining);
                if (!await TryReadExactlyAsync(stream, buffer.AsMemory(0, count), ct)) return false;
                crc = UpdateCrc(crc, buffer.AsSpan(0, count));
                remaining -= count;
            }
            var storedCrcBytes = new byte[4];
            if (!await TryReadExactlyAsync(stream, storedCrcBytes, ct)) return false;
            var storedCrc = BinaryPrimitives.ReadUInt32BigEndian(storedCrcBytes);
            if (~crc != storedCrc) return false;

            if (type == "IDAT" && chunkLength > 0) hasImageData = true;
            if (type == "IEND")
                return chunkLength == 0 && hasHeader && hasImageData && stream.Position == stream.Length;
        }
        return false;
    }

    private static bool IsValidPngColorDepth(byte colorType, byte bitDepth) => colorType switch
    {
        0 => bitDepth is 1 or 2 or 4 or 8 or 16,
        2 => bitDepth is 8 or 16,
        3 => bitDepth is 1 or 2 or 4 or 8,
        4 => bitDepth is 8 or 16,
        6 => bitDepth is 8 or 16,
        _ => false
    };

    private static async Task<bool> IsJpegAsync(FileStream stream, CancellationToken ct)
    {
        if (stream.Length < 12) return false;
        stream.Position = 0;
        var soi = new byte[2];
        if (!await TryReadExactlyAsync(stream, soi, ct) || soi[0] != 0xFF || soi[1] != 0xD8) return false;
        var hasFrame = false;
        var hasScan = false;
        while (stream.Position < stream.Length - 2)
        {
            var markerPrefix = new byte[1];
            if (!await TryReadExactlyAsync(stream, markerPrefix, ct) || markerPrefix[0] != 0xFF) return false;
            do
            {
                if (!await TryReadExactlyAsync(stream, markerPrefix, ct)) return false;
            } while (markerPrefix[0] == 0xFF);
            var marker = markerPrefix[0];
            if (marker == 0x00 || marker is 0xD8 or 0xD9 || marker is >= 0xD0 and <= 0xD7) return false;
            var lengthBytes = new byte[2];
            if (!await TryReadExactlyAsync(stream, lengthBytes, ct)) return false;
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);
            if (segmentLength < 2 || stream.Position + segmentLength - 2 > stream.Length) return false;
            var dataLength = segmentLength - 2;
            var segmentEnd = stream.Position + dataLength;
            if (IsStartOfFrame(marker))
            {
                if (dataLength < 6) return false;
                var frame = new byte[6];
                if (!await TryReadExactlyAsync(stream, frame, ct)) return false;
                var height = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(1, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(3, 2));
                var components = frame[5];
                if (width == 0 || height == 0 || components == 0 || dataLength < 6 + 3 * components) return false;
                hasFrame = true;
            }
            if (marker == 0xDA)
            {
                if (!hasFrame || dataLength < 6) return false;
                var scanComponents = new byte[1];
                if (!await TryReadExactlyAsync(stream, scanComponents, ct) || scanComponents[0] == 0 || dataLength < 4 + 2 * scanComponents[0]) return false;
                hasScan = true;
                break;
            }
            stream.Position = segmentEnd;
        }
        if (!hasScan || stream.Position >= stream.Length - 2 || stream.Length < 2) return false;
        stream.Position = stream.Length - 2;
        var eoi = new byte[2];
        return await TryReadExactlyAsync(stream, eoi, ct) && eoi[0] == 0xFF && eoi[1] == 0xD9;
    }

    private static bool IsStartOfFrame(byte marker) => marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static async Task<bool> TryReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var readTotal = 0;
        while (readTotal < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[readTotal..], ct);
            if (read == 0) return false;
            readTotal += read;
        }
        return true;
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
            crc = (crc >> 8) ^ PngCrcTable[(crc ^ value) & 0xFF];
        return crc;
    }

    private static uint[] CreatePngCrcTable()
    {
        var table = new uint[256];
        for (var i = 0; i < table.Length; i++)
        {
            var value = (uint)i;
            for (var bit = 0; bit < 8; bit++) value = (value & 1) != 0 ? 0xEDB88320 ^ (value >> 1) : value >> 1;
            table[i] = value;
        }
        return table;
    }

    private readonly record struct MediaBox(string Type, long Start, long PayloadStart, long End);
    private readonly record struct Mp3Frame(int Version, int SampleRate, int FrameLength);
}
