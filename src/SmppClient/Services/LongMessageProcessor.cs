using System.Text;
using SmppClient.Protocol;

namespace SmppClient.Services;

public class LongMessageProcessor
{
    private static readonly byte[] Gsm7BitSeptets = new byte[]
    {
        0x40, 0xA3, 0x24, 0xA5, 0xE8, 0xE9, 0xF9, 0xEC, 0xE2, 0xF8, 0xE5, 0x39, 0xC8, 0xC5, 0x3F, 0xFB,
        0xF1, 0xD1, 0xC6, 0x20, 0x21, 0x22, 0x23, 0xBE, 0x25, 0x26, 0x27, 0xCB, 0x2A, 0x2B, 0x2C, 0x2D,
        0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D,
        0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D,
        0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D,
        0x5E, 0x5F, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D,
        0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D,
        0x7E, 0x7F, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD,
        0xCE, 0xCF, 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD,
        0xDE, 0xDF, 0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED,
        0xEE, 0xEF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD,
        0xFE, 0xFF, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD,
        0xAE, 0xAF, 0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD,
        0xBE, 0xBF, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD,
        0xCE, 0xCF, 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD,
        0xDE, 0xDF, 0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED
    };

    public record Segment(byte[] Data, DataCoding Coding, int ReferenceNumber, int SegmentNumber, int TotalSegments);

    public record SplitResult(IList<Segment> Segments, DataCoding UsedCoding, bool UsedPayload);

    public record MergedResult(byte[] Data, DataCoding Coding, bool Success);

    private int _referenceCounter = 0;

    public DataCoding DetectBestEncoding(string message)
    {
        if (CanEncodeAsGsm7(message))
            return DataCoding.GSM7Bit;
        return DataCoding.UCS2;
    }

    public bool CanEncodeAsGsm7(string message)
    {
        foreach (char c in message)
        {
            if (c > 0x7F)
                return false;
            if (!IsGsm7Char(c))
                return false;
        }
        return true;
    }

    private bool IsGsm7Char(char c)
    {
        if (c == 0x20 || (c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x5A) || (c >= 0x61 && c <= 0x7A))
            return true;
        return c switch
        {
            (char)0x0A or (char)0x0D or (char)0x20 or (char)0x21 or (char)0x22 or (char)0x23 or (char)0x24 or (char)0x25 or (char)0x26 or (char)0x27 or (char)0x28 or (char)0x29 or (char)0x2A
            or (char)0x2B or (char)0x2C or (char)0x2D or (char)0x2E or (char)0x2F or (char)0x3A or (char)0x3B or (char)0x3C or (char)0x3D or (char)0x3E or (char)0x3F or (char)0x40
            or (char)0x5B or (char)0x5C or (char)0x5D or (char)0x5F => true,
            _ => false
        };
    }

    public int GetMaxSingleMessageLength(DataCoding coding)
    {
        return coding switch
        {
            DataCoding.GSM7Bit => 160,
            DataCoding.UCS2 => 70,
            _ => 140
        };
    }

    public int GetMaxSegmentLength(DataCoding coding)
    {
        return coding switch
        {
            DataCoding.GSM7Bit => 153,
            DataCoding.UCS2 => 67,
            _ => 134
        };
    }

    public int GetUdhLength(DataCoding coding)
    {
        return coding switch
        {
            DataCoding.GSM7Bit => 7,
            _ => 6
        };
    }

    public SplitResult Split(string message, DataCoding? preferredCoding = null)
    {
        var coding = preferredCoding ?? DetectBestEncoding(message);
        var maxLength = GetMaxSingleMessageLength(coding);
        var maxSegmentLength = GetMaxSegmentLength(coding);
        var udhLength = GetUdhLength(coding);

        if (message.Length <= maxLength)
        {
            var data = coding == DataCoding.GSM7Bit
                ? EncodeGsm7(message)
                : Encoding.BigEndianUnicode.GetBytes(message);

            return new SplitResult(
                new List<Segment>
                {
                    new(data, coding, 0, 1, 1)
                },
                coding,
                false
            );
        }

        var referenceNumber = NextReferenceNumber();
        var segments = new List<Segment>();
        var totalSegments = (int)Math.Ceiling((double)message.Length / maxSegmentLength);
        var segmentData = coding switch
        {
            DataCoding.GSM7Bit => SplitGsm7WithUdh(message, referenceNumber, totalSegments, maxSegmentLength, udhLength),
            DataCoding.UCS2 => SplitUcs2WithUdh(message, referenceNumber, totalSegments, maxSegmentLength, udhLength),
            _ => throw new NotSupportedException($"Coding {coding} not supported")
        };

        return new SplitResult(segmentData, coding, false);
    }

    private List<Segment> SplitGsm7WithUdh(string message, int referenceNumber, int totalSegments, int maxSegmentLength, int udhLength)
    {
        var segments = new List<Segment>();
        var udh = new byte[]
        {
            0x00,
            0x03,
            (byte)referenceNumber,
            (byte)totalSegments
        };

        var encoded = EncodeGsm7(message);
        var totalBytes = encoded.Length;
        var pos = 0;
        var segmentNumber = 1;

        while (pos < totalBytes)
        {
            var udhWithSeq = new byte[udhLength];
            Buffer.BlockCopy(udh, 0, udhWithSeq, 0, udh.Length);
            udhWithSeq[3] = (byte)referenceNumber;
            udhWithSeq[4] = (byte)totalSegments;
            udhWithSeq[5] = (byte)segmentNumber;

            var segmentData = new byte[maxSegmentLength + udhLength];
            Buffer.BlockCopy(udhWithSeq, 0, segmentData, 0, udhLength);

            var copyLength = Math.Min(maxSegmentLength, totalBytes - pos);
            Buffer.BlockCopy(encoded, pos, segmentData, udhLength, copyLength);

            segments.Add(new Segment(segmentData, DataCoding.GSM7Bit, referenceNumber, segmentNumber, totalSegments));

            pos += maxSegmentLength;
            segmentNumber++;
        }

        return segments;
    }

    private List<Segment> SplitUcs2WithUdh(string message, int referenceNumber, int totalSegments, int maxSegmentLength, int udhLength)
    {
        var segments = new List<Segment>();
        var udh = new byte[]
        {
            0x00,
            0x03,
            (byte)referenceNumber,
            (byte)totalSegments
        };

        var encoded = Encoding.BigEndianUnicode.GetBytes(message);
        var totalBytes = encoded.Length;
        var pos = 0;
        var segmentNumber = 1;

        while (pos < totalBytes)
        {
            var udhWithSeq = new byte[udhLength];
            Buffer.BlockCopy(udh, 0, udhWithSeq, 0, udh.Length);
            udhWithSeq[3] = (byte)referenceNumber;
            udhWithSeq[4] = (byte)totalSegments;
            udhWithSeq[5] = (byte)segmentNumber;

            var segmentData = new byte[maxSegmentLength + udhLength];
            Buffer.BlockCopy(udhWithSeq, 0, segmentData, 0, udhLength);

            var copyLength = Math.Min(maxSegmentLength, totalBytes - pos);
            Buffer.BlockCopy(encoded, pos, segmentData, udhLength, copyLength);

            segments.Add(new Segment(segmentData, DataCoding.UCS2, referenceNumber, segmentNumber, totalSegments));

            pos += maxSegmentLength;
            segmentNumber++;
        }

        return segments;
    }

    public SplitResult SplitUsingPayload(string message, DataCoding? preferredCoding = null)
    {
        var coding = preferredCoding ?? DetectBestEncoding(message);
        var data = coding == DataCoding.GSM7Bit
            ? EncodeGsm7(message)
            : Encoding.BigEndianUnicode.GetBytes(message);

        return new SplitResult(
            new List<Segment>
            {
                new(data, coding, 0, 1, 1)
            },
            coding,
            true
        );
    }

    private byte[] EncodeGsm7(string message)
    {
        var result = new List<byte>();
        foreach (char c in message)
        {
            result.Add(EncodeGsm7Char(c));
        }
        return result.ToArray();
    }

    private byte EncodeGsm7Char(char c)
    {
        if (c == 0x20) return 0x20;
        if (c >= 0x41 && c <= 0x5A) return (byte)c;
        if (c >= 0x61 && c <= 0x7A) return (byte)(c - 0x20);
        if (c >= 0x30 && c <= 0x39) return (byte)c;

        return c switch
        {
            (char)0x0A => (byte)0x0A,
            (char)0x0D => (byte)0x0D,
            (char)0x21 => (byte)0x21,
            (char)0x22 => (byte)0x22,
            (char)0x23 => (byte)0x23,
            (char)0x24 => (byte)0x02,
            (char)0x25 => (byte)0x25,
            (char)0x26 => (byte)0x26,
            (char)0x27 => (byte)0x27,
            (char)0x28 => (byte)0x28,
            (char)0x29 => (byte)0x29,
            (char)0x2A => (byte)0x2A,
            (char)0x2B => (byte)0x2B,
            (char)0x2C => (byte)0x2C,
            (char)0x2D => (byte)0x2D,
            (char)0x2E => (byte)0x2E,
            (char)0x2F => (byte)0x2F,
            (char)0x3A => (byte)0x3A,
            (char)0x3B => (byte)0x3B,
            (char)0x3C => (byte)0x3C,
            (char)0x3D => (byte)0x3D,
            (char)0x3E => (byte)0x3E,
            (char)0x3F => (byte)0x3F,
            (char)0x40 => (byte)0x00,
            (char)0x5B => (byte)0x1B,
            (char)0x5C => (byte)0x1B,
            (char)0x5D => (byte)0x1B,
            (char)0x5F => (byte)0x11,
            _ => (byte)0x3F
        };
    }

    public MergedResult Merge(IList<Segment> segments)
    {
        if (segments.Count == 0)
            return new MergedResult(Array.Empty<byte>(), DataCoding.GSM7Bit, false);

        if (segments.Count == 1)
        {
            var seg = segments[0];
            return new MergedResult(seg.Data, seg.Coding, true);
        }

        var firstSegment = segments[0];
        var coding = firstSegment.Coding;
        var totalSegments = firstSegment.TotalSegments;
        var referenceNumber = firstSegment.ReferenceNumber;

        if (segments.Any(s => s.ReferenceNumber != referenceNumber || s.TotalSegments != totalSegments))
            return new MergedResult(Array.Empty<byte>(), coding, false);

        var segmentLength = coding == DataCoding.GSM7Bit ? 153 : 67;
        var udhLength = coding == DataCoding.GSM7Bit ? 7 : 6;

        var sortedSegments = segments.OrderBy(s => s.SegmentNumber).ToList();
        var result = new List<byte>();
        var receivedSegments = 0;

        foreach (var segment in sortedSegments)
        {
            if (segment.SegmentNumber < 1 || segment.SegmentNumber > totalSegments)
                return new MergedResult(Array.Empty<byte>(), coding, false);

            if (receivedSegments >= segment.SegmentNumber)
                return new MergedResult(Array.Empty<byte>(), coding, false);

            var offset = udhLength;
            var length = segment.Data.Length - udhLength;
            if (length < 0 || segment.Data.Length < udhLength)
                return new MergedResult(Array.Empty<byte>(), coding, false);

            for (var i = 0; i < length; i++)
                result.Add(segment.Data[offset + i]);

            receivedSegments++;
        }

        if (receivedSegments != totalSegments)
            return new MergedResult(Array.Empty<byte>(), coding, false);

        return new MergedResult(result.ToArray(), coding, true);
    }

    public string Decode(byte[] data, DataCoding coding)
    {
        return coding switch
        {
            DataCoding.GSM7Bit => DecodeGsm7(data),
            DataCoding.UCS2 => Encoding.BigEndianUnicode.GetString(data),
            DataCoding.ASCII => Encoding.ASCII.GetString(data),
            _ => Encoding.UTF8.GetString(data)
        };
    }

    private string DecodeGsm7(byte[] data)
    {
        var result = new char[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            result[i] = DecodeGsm7Char(data[i]);
        }
        return new string(result);
    }

    private char DecodeGsm7Char(byte b)
    {
        return b switch
        {
            0x00 => '@',
            0x01 => 'A',
            0x02 => '$',
            0x03 => 'B',
            0x04 => 'C',
            0x05 => 'D',
            0x06 => 'E',
            0x07 => 'F',
            0x08 => 'G',
            0x09 => 'H',
            0x0A => '\n',
            0x0B => 'J',
            0x0C => 'K',
            0x0D => '\r',
            0x0E => 'L',
            0x0F => 'M',
            0x10 => 'N',
            0x11 => 'O',
            0x12 => 'P',
            0x13 => 'Q',
            0x14 => 'R',
            0x15 => 'S',
            0x16 => 'T',
            0x17 => 'U',
            0x18 => 'V',
            0x19 => 'W',
            0x1A => 'X',
            0x1B => (char)0x1B,
            0x1C => 'Y',
            0x1D => 'Z',
            0x1E => 'a',
            0x1F => 'b',
            _ when b >= 0x20 && b <= 0x7A => (char)b,
            0x7B => 'c',
            0x7C => 'd',
            0x7D => 'e',
            0x7E => (char)0x3F,
            0x7F => (char)0x3F,
            _ => '?'
        };
    }

    private int NextReferenceNumber()
    {
        return Interlocked.Increment(ref _referenceCounter) % 255;
    }
}
