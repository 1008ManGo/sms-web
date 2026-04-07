namespace SmppClient;

public enum TlvTag : ushort
{
    DestAddrSubunit = 0x0005,
    DestNetworkType = 0x0006,
    DestBearerType = 0x0007,
    DestTelemetryProtocol = 0x0008,
    SourceAddrSubunit = 0x000D,
    SourceNetworkType = 0x000E,
    SourceBearerType = 0x000F,
    SourceTelemetryProtocol = 0x0010,
    QosTimeToLive = 0x0017,
    PayloadType = 0x0019,
    AdditionalStatusInfo = 0x001D,
    ReceiptedMessageId = 0x001E,
    SourcePort = 0x0200,
    DestinationPort = 0x0201,
    SarMsgRefNum = 0x0202,
    SarTotalSegments = 0x0203,
    SarSegmentSeqnum = 0x0204,
    UserMessageReference = 0x0205,
    UserResponseCode = 0x0206,
    SourceNetworkId = 0x0207,
    DestinationNetworkId = 0x0208,
    SourceNodeId = 0x0209,
    DestinationNodeId = 0x020A,
    BillingIdentifier = 0x020B,
    DestinationSequenceNumber = 0x020C,
    NetworkErrorCode = 0x020D,
    MessagePayload = 0x020E,
    DeliveryFailureReason = 0x020F,
    MoreMessagesToSend = 0x0210,
    MessageState = 0x0211,
    UssdServiceOp = 0x0212,
    DisplayTime = 0x0301,
    SmsSignal = 0x0302,
    MsValidity = 0x0311,
    MsMsgWaitFacilities = 0x0321,
    NumberOfMessages = 0x0322,
    AlertOnMessageDelivery = 0x0381,
    LanguageIndicator = 0x03D1
}

public class Tlv
{
    public TlvTag Tag { get; }
    public ushort Length { get; }
    public byte[] Value { get; }

    public Tlv(TlvTag tag, byte[] value)
    {
        Tag = tag;
        Length = (ushort)value.Length;
        Value = value;
    }

    public static Tlv FromBytes(TlvTag tag, byte[] value)
    {
        return new Tlv(tag, value);
    }

    public byte[] ToBytes()
    {
        var result = new byte[4 + Length];
        result[0] = (byte)((ushort)Tag >> 8);
        result[1] = (byte)((ushort)Tag & 0xFF);
        result[2] = (byte)(Length >> 8);
        result[3] = (byte)(Length & 0xFF);
        Buffer.BlockCopy(Value, 0, result, 4, Length);
        return result;
    }

    public string GetString() => System.Text.Encoding.UTF8.GetString(Value);
    public ushort GetUInt16() => (ushort)((Value[0] << 8) | Value[1]);
    public byte GetByte() => Value[0];
}

public class TlvCollection
{
    private readonly Dictionary<TlvTag, Tlv> _tlvs = new();

    public void Add(Tlv tlv) => _tlvs[tlv.Tag] = tlv;

    public Tlv? Get(TlvTag tag) => _tlvs.GetValueOrDefault(tag);

    public bool TryGet(TlvTag tag, out Tlv? tlv) => _tlvs.TryGetValue(tag, out tlv);

    public int Count => _tlvs.Count;

    public IEnumerable<TlvTag> Tags => _tlvs.Keys;
}
