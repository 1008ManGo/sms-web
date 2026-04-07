using SmppClient.Services;
using Xunit;

namespace SmppClient.Tests;

public class LongMessageProcessorTests
{
    private readonly LongMessageProcessor _processor = new();

    [Fact]
    public void DetectBestEncoding_Gsm7Only_ReturnsGsm7()
    {
        var result = _processor.DetectBestEncoding("Hello World 123");
        Assert.Equal(DataCoding.GSM7Bit, result);
    }

    [Fact]
    public void DetectBestEncoding_ContainsUnicode_ReturnsUcs2()
    {
        var result = _processor.DetectBestEncoding("Hello 世界");
        Assert.Equal(DataCoding.UCS2, result);
    }

    [Theory]
    [InlineData("Hello", 160)]
    [InlineData("你好", 70)]
    public void GetMaxSingleMessageLength_ReturnsCorrectLength(string content, int expected)
    {
        var coding = _processor.DetectBestEncoding(content);
        var length = _processor.GetMaxSingleMessageLength(coding);
        Assert.Equal(expected, length);
    }

    [Fact]
    public void Split_ShortMessage_ReturnsSingleSegment()
    {
        var result = _processor.Split("Hello");

        Assert.Single(result.Segments);
        Assert.Equal(1, result.Segments[0].TotalSegments);
        Assert.Equal(1, result.Segments[0].SegmentNumber);
        Assert.False(result.UsedPayload);
    }

    [Fact]
    public void Split_LongMessage_ReturnsMultipleSegments()
    {
        var longMessage = new string('A', 200);
        var result = _processor.Split(longMessage);

        Assert.True(result.Segments.Count > 1);
        Assert.All(result.Segments, s =>
        {
            Assert.True(s.TotalSegments > 1);
            Assert.True(s.SegmentNumber >= 1);
            Assert.True(s.SegmentNumber <= s.TotalSegments);
        });
    }

    [Fact]
    public void Split_LongMessage_SameReferenceNumber()
    {
        var longMessage = new string('A', 200);
        var result = _processor.Split(longMessage);

        var reference = result.Segments[0].ReferenceNumber;
        Assert.All(result.Segments, s => Assert.Equal(reference, s.ReferenceNumber));
    }

    [Fact]
    public void Merge_ValidSegments_ReturnsOriginalMessage()
    {
        var original = "Hello World Test Message for Merging";
        var splitResult = _processor.Split(original);

        var mergeResult = _processor.Merge(splitResult.Segments);

        Assert.True(mergeResult.Success);
        var decoded = _processor.Decode(mergeResult.Data, mergeResult.Coding);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Decode_Gsm7_ReturnsCorrectString()
    {
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var result = _processor.Decode(data, DataCoding.GSM7Bit);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Decode_Ucs2_ReturnsCorrectString()
    {
        var data = new byte[] { 0x4E, 0x2D, 0x65, 0x7D }; // "你好" in UCS2-BE
        var result = _processor.Decode(data, DataCoding.UCS2);
        Assert.Equal("你好", result);
    }
}
