namespace SmppClient;

public enum CommandId : uint
{
    GenericNack = 0x80000000,
    BindReceiver = 0x00000001,
    BindReceiverResp = 0x80000001,
    BindTransmitter = 0x00000002,
    BindTransmitterResp = 0x80000002,
    QuerySm = 0x00000003,
    QuerySmResp = 0x80000003,
    SubmitSm = 0x00000004,
    SubmitSmResp = 0x80000004,
    DeliverSm = 0x00000005,
    DeliverSmResp = 0x80000005,
    Unbind = 0x00000006,
    UnbindResp = 0x80000006,
    Reserved = 0x00000007,
    ReplaceSm = 0x00000007,
    Reserved2 = 0x80000007,
    CancelSm = 0x00000008,
    CancelSmResp = 0x80000008,
    BindTransceiver = 0x00000009,
    BindTransceiverResp = 0x80000009,
    Outbind = 0x0000000B,
    EnquireLink = 0x00000015,
    EnquireLinkResp = 0x80000015,
    SubmitMulti = 0x00000021,
    SubmitMultiResp = 0x80000021,
    DataSm = 0x00000103,
    DataSmResp = 0x80000103
}

public enum CommandStatus : uint
{
    ESME_ROK = 0x00000000,
    ESME_RINVMSGLEN = 0x00000001,
    ESME_RINVCMDLEN = 0x00000002,
    ESME_RINVCMDID = 0x00000003,
    ESME_RINVBNDSTS = 0x00000004,
    ESME_RALYBND = 0x00000005,
    ESME_RINVPRTFLG = 0x00000009,
    ESME_RINVREGDLVFLG = 0x0000000B,
    ESME_RSYSERR = 0x0000000D,
    ESME_RINVSRCADR = 0x0000000F,
    ESME_RINVDSTADR = 0x00000011,
    ESME_RINVMSGID = 0x00000033,
    ESME_RBINDFAIL = 0x0000000D,
    ESME_RINVPASWD = 0x0000000E,
    ESME_RINVSYSID = 0x0000000F,
    ESME_RCANCELFAIL = 0x0000001B,
    ESME_RREPLACEFAIL = 0x0000001D,
    ESME_RMSGQFUL = 0x00000014,
    ESME_RTHROTTLED = 0x00000058
}

public enum DataCoding : byte
{
    GSM7Bit = 0x00,
    ASCII = 0x01,
    IA5 = 0x02,
    ISO88591 = 0x03,
    ISO88592 = 0x04,
    ISO88593 = 0x05,
    ISO88594 = 0x06,
    ISO88595 = 0x07,
    ISO88596 = 0x08,
    ISO88597 = 0x09,
    ISO88598 = 0x0A,
    ISO88599 = 0x0B,
    JIS = 0x0D,
    Cyrillic = 0x0C,
    KS_C_5601 = 0x0E,
    UCS2 = 0x10,
    Pictogram = 0x11,
    ISO2022JP = 0x12,
    Reserved = 0x13
}

public enum Ton : byte
{
    Unknown = 0x00,
    International = 0x01,
    National = 0x02,
    NetworkSpecific = 0x03,
    SubscriberNumber = 0x04,
    Alphanumeric = 0x05,
    Abbreviated = 0x06
}

public enum Npi : byte
{
    Unknown = 0x00,
    E164 = 0x01,
    E212 = 0x02,
    X400 = 0x03,
    Telex = 0x04,
    SC = 0x06,
    LandMobile = 0x09,
    National = 0x10,
    Private = 0x0E,
    ERMES = 0x0D,
    Internet = 0x0F
}

public enum MessageType : byte
{
    Default = 0x00,
    SMEToSME = 0x01,
    DeliveryAcknowledgement = 0x02,
    ManualAcknowledgement = 0x03,
    ConversationAbort = 0x21,
    IntermediateNotification = 0x08
}

public enum RegisteredDelivery : byte
{
    NoDeliveryReceipt = 0x00,
    FinalDeliveryReceipt = 0x01,
    FailureDeliveryReceipt = 0x02,
    FinalAndFailureDeliveryReceipt = 0x03
}

public enum ReplaceIfPresent : byte
{
    DoNotReplace = 0x00,
    Replace = 0x01
}
