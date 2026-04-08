using System.Buffers;
using System.Text;

namespace SmppClient.Protocol;

public class PduCodec
{
    private const int HeaderLength = 16;
    private const int DefaultBufferSize = 1024;
    private const int MaxPduSize = 65536;

    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

    public byte[] Encode(Pdu pdu)
    {
        var buffer = BytePool.Rent(DefaultBufferSize);
        try
        {
            var length = EncodeToBuffer(pdu, buffer);
            var result = new byte[length];
            Buffer.BlockCopy(buffer, 0, result, 0, length);
            return result;
        }
        finally
        {
            BytePool.Return(buffer);
        }
    }

    public int EncodeToBuffer(Pdu pdu, byte[] buffer)
    {
        var bodyLength = EncodeBody(pdu, buffer.AsSpan(HeaderLength));
        var totalLength = HeaderLength + bodyLength;

        var span = buffer.AsSpan();
        BitConverter.GetBytes(totalLength).CopyTo(span[0..4]);
        BitConverter.GetBytes((uint)pdu.CommandId).CopyTo(span[4..8]);
        BitConverter.GetBytes((uint)pdu.Status).CopyTo(span[8..12]);
        BitConverter.GetBytes(pdu.SequenceNumber).CopyTo(span[12..16]);

        return totalLength;
    }

    private int EncodeBody(Pdu pdu, Span<byte> buffer)
    {
        return pdu switch
        {
            BindTransmitterPdu p => EncodeBindPdu(p, buffer),
            BindTransmitterRespPdu p => EncodeBindRespPdu(p, buffer),
            BindReceiverPdu p => EncodeBindPdu(p, buffer),
            BindReceiverRespPdu p => EncodeBindRespPdu(p, buffer),
            BindTransceiverPdu p => EncodeBindPdu(p, buffer),
            BindTransceiverRespPdu p => EncodeBindRespPdu(p, buffer),
            SubmitSmPdu p => EncodeSubmitSm(p, buffer),
            SubmitSmRespPdu p => EncodeSubmitSmResp(p, buffer),
            DeliverSmPdu p => EncodeDeliverSm(p, buffer),
            DeliverSmRespPdu p => EncodeDeliverSmResp(p, buffer),
            EnquireLinkPdu => 0,
            EnquireLinkRespPdu => 0,
            UnbindPdu => 0,
            UnbindRespPdu => 0,
            _ => throw new NotSupportedException($"PDU type {pdu.GetType().Name} not supported")
        };
    }

    private int EncodeBindPdu(Pdu p, Span<byte> buffer)
    {
        var offset = 0;
        offset += EncodeCString(GetServiceType(p), 6, buffer[offset..]);
        offset += EncodeCString(GetPassword(p), 16, buffer[offset..]);
        offset += EncodeCString(GetSystemType(p), 13, buffer[offset..]);
        buffer[offset++] = (byte)GetInterfaceVersion(p);
        buffer[offset++] = (byte)GetAddressTon(p);
        buffer[offset++] = (byte)GetAddressNpi(p);
        offset += EncodeCString(GetAddressRange(p), 41, buffer[offset..]);
        return offset;
    }

    private string GetServiceType(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.SystemType,
        BindReceiverPdu bp => bp.SystemType,
        BindTransceiverPdu bp => bp.SystemType,
        SubmitSmPdu sp => sp.ServiceType,
        _ => string.Empty
    };

    private string GetPassword(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.Password,
        BindReceiverPdu bp => bp.Password,
        BindTransceiverPdu bp => bp.Password,
        _ => string.Empty
    };

    private string GetSystemType(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.SystemType,
        BindReceiverPdu bp => bp.SystemType,
        BindTransceiverPdu bp => bp.SystemType,
        _ => string.Empty
    };

    private Ton GetInterfaceVersion(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.InterfaceVersion,
        BindReceiverPdu bp => bp.InterfaceVersion,
        BindTransceiverPdu bp => bp.InterfaceVersion,
        _ => Ton.Unknown
    };

    private Ton GetAddressTon(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.AddressTon,
        BindReceiverPdu bp => bp.AddressTon,
        BindTransceiverPdu bp => bp.AddressTon,
        _ => Ton.Unknown
    };

    private Npi GetAddressNpi(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.AddressNpi,
        BindReceiverPdu bp => bp.AddressNpi,
        BindTransceiverPdu bp => bp.AddressNpi,
        _ => Npi.Unknown
    };

    private string GetAddressRange(Pdu p) => p switch
    {
        BindTransmitterPdu bp => bp.AddressRange,
        BindReceiverPdu bp => bp.AddressRange,
        BindTransceiverPdu bp => bp.AddressRange,
        _ => string.Empty
    };

    private int EncodeBindRespPdu(Pdu p, Span<byte> buffer)
    {
        var offset = 0;
        offset += EncodeCString(GetRespSystemId(p), 16, buffer[offset..]);
        offset += EncodeCString(GetScInterfaceVersion(p), 4, buffer[offset..]);
        return offset;
    }

    private string GetRespSystemId(Pdu p) => p switch
    {
        BindTransmitterRespPdu bp => bp.SystemId,
        BindReceiverRespPdu bp => bp.SystemId,
        BindTransceiverRespPdu bp => bp.SystemId,
        _ => string.Empty
    };

    private string GetScInterfaceVersion(Pdu p) => p switch
    {
        BindTransmitterRespPdu bp => bp.ScInterfaceVersion,
        BindReceiverRespPdu bp => bp.ScInterfaceVersion,
        BindTransceiverRespPdu bp => bp.ScInterfaceVersion,
        _ => string.Empty
    };

    private int EncodeSubmitSm(SubmitSmPdu p, Span<byte> buffer)
    {
        var offset = 0;
        offset += EncodeCString(p.ServiceType, 6, buffer[offset..]);
        buffer[offset++] = (byte)p.SourceAddrTon;
        buffer[offset++] = (byte)p.SourceAddrNpi;
        offset += EncodeCString(p.SourceAddr, 65, buffer[offset..]);
        buffer[offset++] = (byte)p.DestAddrTon;
        buffer[offset++] = (byte)p.DestAddrNpi;
        offset += EncodeCString(p.DestinationAddr, 65, buffer[offset..]);
        buffer[offset++] = p.EsmClass;
        buffer[offset++] = p.ProtocolId;
        buffer[offset++] = p.PriorityFlag;
        offset += EncodeCString(p.ScheduleDeliveryTime, 17, buffer[offset..]);
        offset += EncodeCString(p.ValidityPeriod, 17, buffer[offset..]);
        buffer[offset++] = (byte)p.RegisteredDelivery;
        buffer[offset++] = (byte)p.ReplaceIfPresent;
        buffer[offset++] = (byte)p.DataCoding;
        buffer[offset++] = p.DefaultMsgId;
        buffer[offset++] = p.ShortMessageLength;
        p.ShortMessage.CopyTo(buffer[offset..]);
        offset += p.ShortMessage.Length;
        return offset;
    }

    private int EncodeSubmitSmResp(SubmitSmRespPdu p, Span<byte> buffer)
    {
        return EncodeCString(p.MessageId, 65, buffer);
    }

    private int EncodeDeliverSm(DeliverSmPdu p, Span<byte> buffer)
    {
        var offset = 0;
        offset += EncodeCString(p.ServiceType, 6, buffer[offset..]);
        buffer[offset++] = (byte)p.SourceAddrTon;
        buffer[offset++] = (byte)p.SourceAddrNpi;
        offset += EncodeCString(p.SourceAddr, 65, buffer[offset..]);
        buffer[offset++] = (byte)p.DestAddrTon;
        buffer[offset++] = (byte)p.DestAddrNpi;
        offset += EncodeCString(p.DestinationAddr, 65, buffer[offset..]);
        buffer[offset++] = p.EsmClass;
        buffer[offset++] = p.ProtocolId;
        buffer[offset++] = p.PriorityFlag;
        offset += EncodeCString(p.ScheduleDeliveryTime, 17, buffer[offset..]);
        offset += EncodeCString(p.ValidityPeriod, 17, buffer[offset..]);
        buffer[offset++] = (byte)p.RegisteredDelivery;
        buffer[offset++] = (byte)p.ReplaceIfPresent;
        buffer[offset++] = (byte)p.DataCoding;
        buffer[offset++] = p.DefaultMsgId;
        buffer[offset++] = p.ShortMessageLength;
        p.ShortMessage.CopyTo(buffer[offset..]);
        offset += p.ShortMessage.Length;
        return offset;
    }

    private int EncodeDeliverSmResp(DeliverSmRespPdu p, Span<byte> buffer)
    {
        return EncodeCString(p.MessageId, 65, buffer);
    }

    public Pdu Decode(byte[] data)
    {
        if (data.Length < HeaderLength)
            throw new ArgumentException($"Data too short: {data.Length} bytes");

        var commandLength = BitConverter.ToUInt32(data, 0);
        var commandId = (CommandId)BitConverter.ToUInt32(data, 4);
        var status = (CommandStatus)BitConverter.ToUInt32(data, 8);
        var sequenceNumber = BitConverter.ToUInt32(data, 12);

        var bodyLength = (int)commandLength - HeaderLength;
        var body = bodyLength > 0 ? data.AsSpan(HeaderLength, bodyLength) : Span<byte>.Empty;

        var pdu = Pdu.Create(commandId);
        pdu.SequenceNumber = sequenceNumber;
        pdu.Status = status;

        DecodeBody(pdu, body);
        DecodeTlvs(pdu, body, bodyLength);

        return pdu;
    }

    private void DecodeBody(Pdu pdu, Span<byte> body)
    {
        var pos = 0;

        switch (pdu)
        {
            case BindTransceiverPdu bp:
                (bp.SystemId, pos) = ReadCString(body, pos, 16);
                (bp.Password, pos) = ReadCString(body, pos, 16);
                (bp.SystemType, pos) = ReadCString(body, pos, 13);
                bp.InterfaceVersion = (Ton)body[pos++];
                bp.AddressTon = (Ton)body[pos++];
                bp.AddressNpi = (Npi)body[pos++];
                (bp.AddressRange, pos) = ReadCString(body, pos, 41);
                break;

            case BindTransceiverRespPdu bp:
                (bp.SystemId, pos) = ReadCString(body, pos, 16);
                if (pos < body.Length)
                    (bp.ScInterfaceVersion, pos) = ReadCString(body, pos, 6);
                break;

            case SubmitSmPdu sp:
                (sp.ServiceType, pos) = ReadCString(body, pos, 6);
                sp.SourceAddrTon = (Ton)body[pos++];
                sp.SourceAddrNpi = (Npi)body[pos++];
                (sp.SourceAddr, pos) = ReadCString(body, pos, 65);
                sp.DestAddrTon = (Ton)body[pos++];
                sp.DestAddrNpi = (Npi)body[pos++];
                (sp.DestinationAddr, pos) = ReadCString(body, pos, 65);
                sp.EsmClass = body[pos++];
                sp.ProtocolId = body[pos++];
                sp.PriorityFlag = body[pos++];
                (sp.ScheduleDeliveryTime, pos) = ReadCString(body, pos, 17);
                (sp.ValidityPeriod, pos) = ReadCString(body, pos, 17);
                sp.RegisteredDelivery = (RegisteredDelivery)body[pos++];
                sp.ReplaceIfPresent = (ReplaceIfPresent)body[pos++];
                sp.DataCoding = (DataCoding)body[pos++];
                sp.DefaultMsgId = body[pos++];
                sp.ShortMessageLength = body[pos++];
                if (sp.ShortMessageLength > 0)
                {
                    sp.ShortMessage = new byte[sp.ShortMessageLength];
                    body.Slice(pos, sp.ShortMessageLength).CopyTo(sp.ShortMessage);
                }
                break;

            case SubmitSmRespPdu sp:
                (sp.MessageId, pos) = ReadCString(body, pos, 65);
                break;

            case DeliverSmPdu dp:
                (dp.ServiceType, pos) = ReadCString(body, pos, 6);
                dp.SourceAddrTon = (Ton)body[pos++];
                dp.SourceAddrNpi = (Npi)body[pos++];
                (dp.SourceAddr, pos) = ReadCString(body, pos, 65);
                dp.DestAddrTon = (Ton)body[pos++];
                dp.DestAddrNpi = (Npi)body[pos++];
                (dp.DestinationAddr, pos) = ReadCString(body, pos, 65);
                dp.EsmClass = body[pos++];
                dp.ProtocolId = body[pos++];
                dp.PriorityFlag = body[pos++];
                (dp.ScheduleDeliveryTime, pos) = ReadCString(body, pos, 17);
                (dp.ValidityPeriod, pos) = ReadCString(body, pos, 17);
                dp.RegisteredDelivery = (RegisteredDelivery)body[pos++];
                dp.ReplaceIfPresent = (ReplaceIfPresent)body[pos++];
                dp.DataCoding = (DataCoding)body[pos++];
                dp.DefaultMsgId = body[pos++];
                dp.ShortMessageLength = body[pos++];
                if (dp.ShortMessageLength > 0)
                {
                    dp.ShortMessage = new byte[dp.ShortMessageLength];
                    body.Slice(pos, dp.ShortMessageLength).CopyTo(dp.ShortMessage);
                }
                break;

            case DeliverSmRespPdu dp:
                (dp.MessageId, pos) = ReadCString(body, pos, 65);
                break;
        }
    }

    private void DecodeTlvs(Pdu pdu, Span<byte> body, int bodyLength)
    {
        var fixedPartLength = GetFixedPartLength(pdu);
        if (body.Length <= fixedPartLength)
            return;

        var pos = fixedPartLength;
        while (pos + 4 <= bodyLength)
        {
            var tag = (TlvTag)((body[pos] << 8) | body[pos + 1]);
            var len = (ushort)((body[pos + 2] << 8) | body[pos + 3]);
            pos += 4;

            if (pos + len > bodyLength)
                break;

            var value = new byte[len];
            body.Slice(pos, len).CopyTo(value);
            pos += len;

            pdu.OptionalParameters.Add(new Tlv(tag, value));
        }
    }

    private int GetFixedPartLength(Pdu pdu)
    {
        return pdu switch
        {
            BindTransmitterPdu or BindReceiverPdu or BindTransceiverPdu => 17 + 16 + 16 + 13 + 1 + 1 + 1 + 41,
            BindTransmitterRespPdu or BindReceiverRespPdu or BindTransceiverRespPdu => 16 + 1,
            SubmitSmPdu or DeliverSmPdu => 6 + 1 + 1 + 65 + 1 + 1 + 65 + 1 + 1 + 1 + 17 + 17 + 1 + 1 + 1 + 1 + 1 + 1,
            SubmitSmRespPdu or DeliverSmRespPdu => 4 + 65,
            _ => 0
        };
    }

    private int EncodeCString(string? value, int maxLength, Span<byte> buffer)
    {
        if (buffer.Length < maxLength)
            throw new ArgumentException($"Buffer too small for C-String, need {maxLength}");

        buffer.Clear();
        if (!string.IsNullOrEmpty(value))
        {
            var encoding = Encoding.ASCII;
            var encoded = encoding.GetBytes(value);
            var copyLength = Math.Min(encoded.Length, maxLength - 1);
            encoded.AsSpan(0, copyLength).CopyTo(buffer);
        }
        return maxLength;
    }

    private (string Value, int Pos) ReadCString(Span<byte> data, int pos, int maxLength)
    {
        var endPos = pos;
        while (endPos < data.Length && endPos < pos + maxLength && data[endPos] != 0)
            endPos++;

        var length = endPos - pos;
        var value = length > 0 ? Encoding.ASCII.GetString(data.Slice(pos, length)) : string.Empty;
        return (value, Math.Min(endPos + 1, data.Length));
    }
}
