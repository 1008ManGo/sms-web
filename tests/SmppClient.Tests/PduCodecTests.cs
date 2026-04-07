using SmppClient.Protocol;
using Xunit;

namespace SmppClient.Tests;

public class PduCodecTests
{
    private readonly PduCodec _codec = new();

    [Fact]
    public void EncodeDecode_BindTransceiver_RoundTrip()
    {
        var original = new BindTransceiverPdu
        {
            SystemId = "test_system",
            Password = "test_password",
            SystemType = "SMPP",
            InterfaceVersion = Ton.Unknown,
            AddressTon = Ton.Unknown,
            AddressNpi = Npi.Unknown,
            AddressRange = ""
        };
        original.SequenceNumber = 123;

        var encoded = _codec.Encode(original);
        var decoded = _codec.Decode(encoded);

        Assert.IsType<BindTransceiverPdu>(decoded);
        var result = (BindTransceiverPdu)decoded;
        Assert.Equal("test_system", result.SystemId);
        Assert.Equal("test_password", result.Password);
        Assert.Equal("SMPP", result.SystemType);
        Assert.Equal(123u, decoded.SequenceNumber);
    }

    [Fact]
    public void EncodeDecode_SubmitSm_RoundTrip()
    {
        var original = new SubmitSmPdu
        {
            SourceAddr = "1234567890",
            DestinationAddr = "0987654321",
            ShortMessage = System.Text.Encoding.ASCII.GetBytes("Hello World"),
            ShortMessageLength = 11,
            DataCoding = DataCoding.GSM7Bit,
            RegisteredDelivery = RegisteredDelivery.FinalDeliveryReceipt,
            SourceAddrTon = Ton.International,
            SourceAddrNpi = Npi.E164,
            DestAddrTon = Ton.International,
            DestAddrNpi = Npi.E164
        };
        original.SequenceNumber = 456;

        var encoded = _codec.Encode(original);
        var decoded = _codec.Decode(encoded);

        Assert.IsType<SubmitSmPdu>(decoded);
        var result = (SubmitSmPdu)decoded;
        Assert.Equal("1234567890", result.SourceAddr);
        Assert.Equal("0987654321", result.DestinationAddr);
        Assert.Equal("Hello World", System.Text.Encoding.ASCII.GetString(result.ShortMessage));
        Assert.Equal(DataCoding.GSM7Bit, result.DataCoding);
    }

    [Fact]
    public void EncodeDecode_SubmitSmResp_RoundTrip()
    {
        var original = new SubmitSmRespPdu
        {
            MessageId = "msg_12345"
        };
        original.SequenceNumber = 789;

        var encoded = _codec.Encode(original);
        var decoded = _codec.Decode(encoded);

        Assert.IsType<SubmitSmRespPdu>(decoded);
        var result = (SubmitSmRespPdu)decoded;
        Assert.Equal("msg_12345", result.MessageId);
    }

    [Fact]
    public void EncodeDecode_DeliverSm_RoundTrip()
    {
        var original = new DeliverSmPdu
        {
            SourceAddr = "sender",
            DestinationAddr = "receiver",
            ShortMessage = System.Text.Encoding.ASCII.GetBytes("Test DLR"),
            ShortMessageLength = 8,
            DataCoding = DataCoding.GSM7Bit
        };
        original.SequenceNumber = 111;

        var encoded = _codec.Encode(original);
        var decoded = _codec.Decode(encoded);

        Assert.IsType<DeliverSmPdu>(decoded);
        var result = (DeliverSmPdu)decoded;
        Assert.Equal("sender", result.SourceAddr);
        Assert.Equal("receiver", result.DestinationAddr);
    }

    [Fact]
    public void EncodeDecode_EnquireLink_RoundTrip()
    {
        var original = new EnquireLinkPdu { SequenceNumber = 999 };
        var encoded = _codec.Encode(original);
        var decoded = _codec.Decode(encoded);

        Assert.IsType<EnquireLinkPdu>(decoded);
        Assert.Equal(999u, decoded.SequenceNumber);
    }

    [Fact]
    public void EncodeDecode_Unbind_RoundTrip()
    {
        var original = new UnbindPdu { SequenceNumber = 888 };
        var encoded = _codec.Encode(original);
        var decoded = _codec.Decode(encoded);

        Assert.IsType<UnbindPdu>(decoded);
        Assert.Equal(888u, decoded.SequenceNumber);
    }
}
