using System.Buffers.Binary;

namespace MirrorImaged.Data;

/// <summary>A sound file's length, read from its headers, not its sound: WAV, FLAC, Ogg Vorbis and MP3.</summary>
public static class SoundFiles
{
    /// <summary>The file's length in whole milliseconds, cut toward zero; null when it is none of the four or is cut short.</summary>
    public static int? Duration(ReadOnlySpan<byte> file)
    {
        try
        {
            var length = file.StartsWith("RIFF"u8) ? Wave(file)
                : file.StartsWith("fLaC"u8) ? Flac(file)
                : file.StartsWith("OggS"u8) ? Vorbis(file)
                : Mp3(file);
            return length is >= 0 and <= int.MaxValue ? (int)length : null;
        }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    // Units over units a second, in milliseconds
    static long? Milliseconds(long units, long rate) => rate > 0 && units >= 0 ? units * 1000 / rate : null;

    // RIFF chunks: fmt's bytes a second, then data's size
    static long? Wave(ReadOnlySpan<byte> file)
    {
        if (!file[8..12].SequenceEqual("WAVE"u8)) return null;
        uint rate = 0;
        for (var at = 12; at + 8 <= file.Length;)
        {
            var size = BinaryPrimitives.ReadUInt32LittleEndian(file[(at + 4)..]);
            if (file[at..(at + 4)].SequenceEqual("fmt "u8)) rate = BinaryPrimitives.ReadUInt32LittleEndian(file[(at + 16)..]);
            else if (file[at..(at + 4)].SequenceEqual("data"u8)) return Milliseconds(size, rate);
            at += 8 + (int)size + (int)(size & 1);
        }
        return null;
    }

    // STREAMINFO, the first block: 20 bits of sample rate, 3 of channels, 5 of bits a sample, 36 of samples
    static long? Flac(ReadOnlySpan<byte> file)
    {
        var bits = BinaryPrimitives.ReadUInt64BigEndian(file[18..]);
        return Milliseconds((long)(bits & 0xFFFFFFFFF), (long)(bits >> 44));
    }

    // The identification header's sample rate, and the last page's granule position: its samples
    static long? Vorbis(ReadOnlySpan<byte> file)
    {
        var header = file.IndexOf("\u0001vorbis"u8);
        return header < 0 ? null
            : Milliseconds(BinaryPrimitives.ReadInt64LittleEndian(file[(file.LastIndexOf("OggS"u8) + 6)..]), BinaryPrimitives.ReadUInt32LittleEndian(file[(header + 12)..]));
    }

    static readonly int[] Mpeg1Rates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
    static readonly int[] Mpeg2Rates = [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160];

    // Layer III frames of one sample rate counted one by one, after any ID3v2 tag, until one doesn't follow; then one
    // frame more, which the game reads for 29 of the 61 mp3s measured, as a decoder's delay would give (Close: D520)
    static long? Mp3(ReadOnlySpan<byte> file)
    {
        var at = file.StartsWith("ID3"u8) ? 10 + (file[6] << 21 | file[7] << 14 | file[8] << 7 | file[9]) : 0;
        long samples = 0;
        int frame = 0, rate = 0;
        while (at + 4 <= file.Length && file[at] == 0xFF && (file[at + 1] & 0xE6) == 0xE2)
        {
            int version = (file[at + 1] >> 3) & 3, rateIndex = file[at + 2] >> 4, frequencyIndex = (file[at + 2] >> 2) & 3;
            if (version == 1 || rateIndex is 0 or 15 || frequencyIndex == 3) break;
            var frequency = new[] { 44100, 48000, 32000 }[frequencyIndex] >> (version == 3 ? 0 : version == 2 ? 1 : 2);
            if (rate != 0 && frequency != rate) break;
            (rate, frame) = (frequency, version == 3 ? 1152 : 576);
            at += frame / 8 * (version == 3 ? Mpeg1Rates : Mpeg2Rates)[rateIndex] * 1000 / frequency + ((file[at + 2] >> 1) & 1);
            samples += frame;
        }
        return frame > 0 ? Milliseconds(samples + frame, rate) : null;
    }
}
