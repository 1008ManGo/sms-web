using System.Text;

namespace SmppClient.Protocol;

public class PduCodec
{
    private const int HeaderLength = 16;

    public byte[] Encode(Pdu pdu)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var bodyAndTlv = EncodeBody(pdu);
        var commandLength = HeaderLength + bodyAndTlv.Length;

        writer.Write(commandLength);
        writer.Write((uint)pdu.CommandId);
        writer.Write((uint)pdu.Status);
        writer.Write(pdu.SequenceNumber);
        writer.Write(bodyAndTlv);

        return ms.ToArray();
    }

    private byte[] EncodeBody(Pdu pdu)
    {
        return pdu switch
        {
            BindTransmitterPdu p => EncodeBindPdu(p),
            BindTransmitterRespPdu p => EncodeBindRespPdu(p),
            BindReceiverPdu p => EncodeBindPdu(p),
            BindReceiverRespPdu p => EncodeBindRespPdu(p),
            BindTransceiverPdu p => EncodeBindPdu(p),
            BindTransceiverRespPdu p => EncodeBindRespPdu(p),
            SubmitSmPdu p => EncodeSubmitSm(p),
            SubmitSmRespPdu p => EncodeSubmitSmResp(p),
            DeliverSmPdu p => EncodeDeliverSm(p),
            DeliverSmRespPdu p => EncodeDeliverSmResp(p),
            EnquireLinkPdu => Array.Empty<byte>(),
            EnquireLinkRespPdu => Array.Empty<byte>(),
            UnbindPdu => Array.Empty<byte>(),
            UnbindRespPdu => Array.Empty<byte>(),
            _ => throw new NotSupportedException($"PDU type {pdu.GetType().Name} not supported")
        };
    }

    private byte[] EncodeBindPdu(BindTransceiverPdu p)
    {
        var body = new List<byte>();
        body.AddRange(EncodeCString(p.ServiceType, 6));
        body.AddRange(EncodeCString(p.Password, 16));
        body.AddRange(EncodeCString(p.SystemType, 13));
        body.Add((byte)p.InterfaceVersion);
        body.Add((byte)p.AddressTon);
        body.Add((byte)p.AddressNpi);
        body.AddRange(EncodeCString(p.AddressRange, 41));
        return body.ToArray();
    }

    private byte[] EncodeBindRespPdu(BindTransceiverRespPdu p)
    {
        var body = new List<byte>();
        body.AddRange(EncodeCString(p.SystemId, 16));
        if (!string.IsNullOrEmpty(p.ScInterfaceVersion))
        {
            body.AddRange(EncodeCString(p.ScInterfaceVersion, 6));
        }
        return body.ToArray();
    }

    private byte[] EncodeSubmitSm(SubmitSmPdu p)
    {
        var body = new List<byte>();
        body.AddRange(EncodeCString(p.ServiceType, 6));
        body.Add((byte)p.SourceAddrTon);
        body.Add((byte)p.SourceAddrNpi);
        body.AddRange(EncodeCString(p.SourceAddr, 65));
        body.Add((byte)p.DestAddrTon);
        body.Add((byte)p.DestAddrNpi);
        body.AddRange(EncodeCString(p.DestinationAddr, 65));
        body.Add(p.EsmClass);
        body.Add(p.ProtocolId);
        body.Add(p.PriorityFlag);
        body.AddRange(EncodeCString(p.ScheduleDeliveryTime, 17));
        body.AddRange(EncodeCString(p.ValidityPeriod, 17));
        body.Add((byte)p.RegisteredDelivery);
        body.Add((byte)p.ReplaceIfPresent);
        body.Add((byte)p.DataCoding);
        body.Add(p.DefaultMsgId);
        body.Add(p.ShortMessageLength);
        body.AddRange(p.ShortMessage);
        return body.ToArray();
    }

    private byte[] EncodeSubmitSmResp(SubmitSmRespPdu p)
    {
        return EncodeCString(p.MessageId, 65).ToArray();
    }

    private byte[] EncodeDeliverSm(DeliverSmPdu p)
    {
        var body = new List<byte>();
        body.AddRange(EncodeCString(p.ServiceType, 6));
        body.Add((byte)p.SourceAddrTon);
        body.Add((byte)p.SourceAddrNpi);
        body.AddRange(EncodeCString(p.SourceAddr, 65));
        body.Add((byte)p.DestAddrTon);
        body.Add((byte)p.DestAddrNpi);
        body.AddRange(EncodeCString(p.DestinationAddr, 65));
        body.Add(p.EsmClass);
        body.Add(p.ProtocolId);
        body.Add(p.PriorityFlag);
        body.AddRange(EncodeCString(p.ScheduleDeliveryTime, 17));
        body.AddRange(EncodeCString(p.ValidityPeriod, 17));
        body.Add((byte)p.RegisteredDelivery);
        body.Add((byte)p.ReplaceIfPresent);
        body.Add((byte)p.DataCoding);
        body.Add(p.DefaultMsgId);
        body.Add(p.ShortMessageLength);
        body.AddRange(p.ShortMessage);
        return body.ToArray();
    }

    private byte[] EncodeDeliverSmResp(DeliverSmRespPdu p)
    {
        return EncodeCString(p.MessageId, 65).ToArray();
    }

    public Pdu Decode(byte[] data)
    {
        if (data.Length < HeaderLength)
            throw new ArgumentException($"Data too short: {data.Length} bytes");

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var commandLength = reader.ReadUInt32();
        var commandId = (CommandId)reader.ReadUInt32();
        var status = (CommandStatus)reader.ReadUInt32();
        var sequenceNumber = reader.ReadUInt32();

        var bodyLength = (int)commandLength - HeaderLength;
        var body = reader.ReadBytes(bodyLength);

        var pdu = Pdu.Create(commandId);
        pdu.SequenceNumber = sequenceNumber;
        pdu.Status = status;

        DecodeBody(pdu, body);
        DecodeTlvs(pdu, body, bodyLength);

        return pdu;
    }

    private void DecodeBody(Pdu pdu, byte[] body)
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
                    Buffer.BlockCopy(body, pos, sp.ShortMessage, 0, sp.ShortMessageLength);
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
                    Buffer.BlockCopy(body, pos, dp.ShortMessage, 0, dp.ShortMessageLength);
                }
                break;

            case DeliverSmRespPdu dp:
                (dp.MessageId, pos) = ReadCString(body, pos, 65);
                break;
        }
    }

    private void DecodeTlvs(Pdu pdu, byte[] body, int bodyLength)
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
            Buffer.BlockCopy(body, pos, value, 0, len);
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

    private byte[] EncodeCString(string? value, int maxLength)
    {
        var bytes = new byte[maxLength];
        if (!string.IsNullOrEmpty(value))
        {
            var encoding = Encoding.ASCII;
            var encoded = encoding.GetBytes(value);
            var copyLength = Math.Min(encoded.Length, maxLength - 1);
            Buffer.BlockCopy(encoded, 0, bytes, 0, copyLength);
        }
        return bytes;
    }

    private (string Value, int Pos) ReadCString(byte[] data, int pos, int maxLength)
    {
        var endPos = pos;
        while (endPos < data.Length && endPos < pos + maxLength && data[endPos] != 0)
            endPos++;

        var length = endPos - pos;
        var value = length > 0 ? Encoding.ASCII.GetString(data, pos, length) : string.Empty;
        return (value, Math.Min(endPos + 1, data.Length));
    }
}
