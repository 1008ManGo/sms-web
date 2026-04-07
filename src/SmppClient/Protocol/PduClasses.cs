namespace SmppClient.Protocol;

public abstract class Pdu
{
    public CommandId CommandId { get; protected set; }
    public uint SequenceNumber { get; set; }
    public CommandStatus Status { get; set; }
    public TlvCollection OptionalParameters { get; } = new();

    public abstract int GetMinimalLength();

    public static Pdu Create(CommandId commandId)
    {
        return commandId switch
        {
            CommandId.BindTransmitter => new BindTransmitterPdu(),
            CommandId.BindTransmitterResp => new BindTransmitterRespPdu(),
            CommandId.BindReceiver => new BindReceiverPdu(),
            CommandId.BindReceiverResp => new BindReceiverRespPdu(),
            CommandId.BindTransceiver => new BindTransceiverPdu(),
            CommandId.BindTransceiverResp => new BindTransceiverRespPdu(),
            CommandId.SubmitSm => new SubmitSmPdu(),
            CommandId.SubmitSmResp => new SubmitSmRespPdu(),
            CommandId.DeliverSm => new DeliverSmPdu(),
            CommandId.DeliverSmResp => new DeliverSmRespPdu(),
            CommandId.EnquireLink => new EnquireLinkPdu(),
            CommandId.EnquireLinkResp => new EnquireLinkRespPdu(),
            CommandId.Unbind => new UnbindPdu(),
            CommandId.UnbindResp => new UnbindRespPdu(),
            _ => throw new ArgumentException($"Unknown PDU type: {commandId}")
        };
    }
}

public class BindTransmitterPdu : Pdu
{
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public Ton InterfaceVersion { get; set; } = Ton.Unknown;
    public Ton AddressTon { get; set; } = Ton.Unknown;
    public Npi AddressNpi { get; set; } = Npi.Unknown;
    public string AddressRange { get; set; } = string.Empty;

    public BindTransmitterPdu()
    {
        CommandId = CommandId.BindTransmitter;
    }

    public override int GetMinimalLength() => 17 + 16 + 16 + 13 + 1 + 1 + 1 + 41;
}

public class BindTransmitterRespPdu : Pdu
{
    public string SystemId { get; set; } = string.Empty;
    public string ScInterfaceVersion { get; set; } = string.Empty;

    public BindTransmitterRespPdu()
    {
        CommandId = CommandId.BindTransmitterResp;
    }

    public override int GetMinimalLength() => 16 + 1;
}

public class BindReceiverPdu : Pdu
{
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public Ton InterfaceVersion { get; set; } = Ton.Unknown;
    public Ton AddressTon { get; set; } = Ton.Unknown;
    public Npi AddressNpi { get; set; } = Npi.Unknown;
    public string AddressRange { get; set; } = string.Empty;

    public BindReceiverPdu()
    {
        CommandId = CommandId.BindReceiver;
    }

    public override int GetMinimalLength() => 17 + 16 + 16 + 13 + 1 + 1 + 1 + 41;
}

public class BindReceiverRespPdu : Pdu
{
    public string SystemId { get; set; } = string.Empty;
    public string ScInterfaceVersion { get; set; } = string.Empty;

    public BindReceiverRespPdu()
    {
        CommandId = CommandId.BindReceiverResp;
    }

    public override int GetMinimalLength() => 16 + 1;
}

public class BindTransceiverPdu : Pdu
{
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public Ton InterfaceVersion { get; set; } = Ton.Unknown;
    public Ton AddressTon { get; set; } = Ton.Unknown;
    public Npi AddressNpi { get; set; } = Npi.Unknown;
    public string AddressRange { get; set; } = string.Empty;

    public BindTransceiverPdu()
    {
        CommandId = CommandId.BindTransceiver;
    }

    public override int GetMinimalLength() => 17 + 16 + 16 + 13 + 1 + 1 + 1 + 41;
}

public class BindTransceiverRespPdu : Pdu
{
    public string SystemId { get; set; } = string.Empty;
    public string ScInterfaceVersion { get; set; } = string.Empty;

    public BindTransceiverRespPdu()
    {
        CommandId = CommandId.BindTransceiverResp;
    }

    public override int GetMinimalLength() => 16 + 1;
}

public class SubmitSmPdu : Pdu
{
    public string ServiceType { get; set; } = string.Empty;
    public Ton SourceAddrTon { get; set; } = Ton.Unknown;
    public Npi SourceAddrNpi { get; set; } = Npi.Unknown;
    public string SourceAddr { get; set; } = string.Empty;
    public Ton DestAddrTon { get; set; } = Ton.Unknown;
    public Npi DestAddrNpi { get; set; } = Npi.Unknown;
    public string DestinationAddr { get; set; } = string.Empty;
    public byte EsmClass { get; set; }
    public byte ProtocolId { get; set; }
    public byte PriorityFlag { get; set; }
    public string ScheduleDeliveryTime { get; set; } = string.Empty;
    public string ValidityPeriod { get; set; } = string.Empty;
    public RegisteredDelivery RegisteredDelivery { get; set; }
    public ReplaceIfPresent ReplaceIfPresent { get; set; }
    public DataCoding DataCoding { get; set; }
    public byte DefaultMsgId { get; set; }
    public byte ShortMessageLength { get; set; }
    public byte[] ShortMessage { get; set; } = Array.Empty<byte>();

    public SubmitSmPdu()
    {
        CommandId = CommandId.SubmitSm;
    }

    public override int GetMinimalLength() => 6 + 1 + 1 + 65 + 1 + 1 + 65 + 1 + 1 + 1 + 17 + 17 + 1 + 1 + 1 + 1 + 1 + 1;
}

public class SubmitSmRespPdu : Pdu
{
    public string MessageId { get; set; } = string.Empty;

    public SubmitSmRespPdu()
    {
        CommandId = CommandId.SubmitSmResp;
    }

    public override int GetMinimalLength() => 4 + 1;
}

public class DeliverSmPdu : Pdu
{
    public string ServiceType { get; set; } = string.Empty;
    public Ton SourceAddrTon { get; set; } = Ton.Unknown;
    public Npi SourceAddrNpi { get; set; } = Npi.Unknown;
    public string SourceAddr { get; set; } = string.Empty;
    public Ton DestAddrTon { get; set; } = Ton.Unknown;
    public Npi DestAddrNpi { get; set; } = Npi.Unknown;
    public string DestinationAddr { get; set; } = string.Empty;
    public byte EsmClass { get; set; }
    public byte ProtocolId { get; set; }
    public byte PriorityFlag { get; set; }
    public string ScheduleDeliveryTime { get; set; } = string.Empty;
    public string ValidityPeriod { get; set; } = string.Empty;
    public RegisteredDelivery RegisteredDelivery { get; set; }
    public ReplaceIfPresent ReplaceIfPresent { get; set; }
    public DataCoding DataCoding { get; set; }
    public byte DefaultMsgId { get; set; }
    public byte ShortMessageLength { get; set; }
    public byte[] ShortMessage { get; set; } = Array.Empty<byte>();

    public DeliverSmPdu()
    {
        CommandId = CommandId.DeliverSm;
    }

    public override int GetMinimalLength() => 6 + 1 + 1 + 65 + 1 + 1 + 65 + 1 + 1 + 1 + 17 + 17 + 1 + 1 + 1 + 1 + 1 + 1;
}

public class DeliverSmRespPdu : Pdu
{
    public string MessageId { get; set; } = string.Empty;

    public DeliverSmRespPdu()
    {
        CommandId = CommandId.DeliverSmResp;
    }

    public override int GetMinimalLength() => 4 + 1;
}

public class EnquireLinkPdu : Pdu
{
    public EnquireLinkPdu()
    {
        CommandId = CommandId.EnquireLink;
    }

    public override int GetMinimalLength() => 0;
}

public class EnquireLinkRespPdu : Pdu
{
    public EnquireLinkRespPdu()
    {
        CommandId = CommandId.EnquireLinkResp;
    }

    public override int GetMinimalLength() => 0;
}

public class UnbindPdu : Pdu
{
    public UnbindPdu()
    {
        CommandId = CommandId.Unbind;
    }

    public override int GetMinimalLength() => 0;
}

public class UnbindRespPdu : Pdu
{
    public UnbindRespPdu()
    {
        CommandId = CommandId.UnbindResp;
    }

    public override int GetMinimalLength() => 0;
}
