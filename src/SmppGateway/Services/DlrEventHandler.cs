using SmppClient.Services;
using SmppStorage.Entities;

namespace SmppGateway.Services;

public class DlrEventHandler : IDisposable
{
    private readonly DlrProcessor _dlrProcessor;
    private readonly IDlrRecordService _dlrRecordService;
    private readonly ISubmitRecordService _submitRecordService;
    private readonly ILogger<DlrEventHandler> _logger;

    public DlrEventHandler(
        DlrProcessor dlrProcessor,
        IDlrRecordService dlrRecordService,
        ISubmitRecordService submitRecordService,
        ILogger<DlrEventHandler> logger)
    {
        _dlrProcessor = dlrProcessor;
        _dlrRecordService = dlrRecordService;
        _submitRecordService = submitRecordService;
        _logger = logger;

        _dlrProcessor.DlrReceived += OnDlrReceived;
    }

    private async void OnDlrReceived(object? sender, DlrCallback callback)
    {
        try
        {
            _logger.LogInformation(
                "DLR event: MessageId={MessageId}, LocalId={LocalId}, Status={Status}",
                callback.MessageId, callback.LocalId, callback.Status);

            var dlrStatus = MapToEntityStatus(callback.Status);

            await _dlrRecordService.UpdateDlrStatusAsync(
                callback.MessageId,
                dlrStatus,
                callback.DlrTime,
                callback.ErrorCode);

            var submit = await _submitRecordService.GetSubmitByLocalIdAsync(callback.LocalId);
            if (submit != null)
            {
                var submitStatus = MapToSubmitStatus(callback.Status);
                await _submitRecordService.UpdateSubmitStatusAsync(
                    callback.LocalId,
                    callback.MessageId,
                    submit.AccountId,
                    submitStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling DLR event for MessageId={MessageId}", callback.MessageId);
        }
    }

    private static SmppStorage.Entities.DlrStatus MapToEntityStatus(SmppClient.Services.DlrStatus status)
    {
        return status switch
        {
            SmppClient.Services.DlrStatus.Delivered => SmppStorage.Entities.DlrStatus.Delivered,
            SmppClient.Services.DlrStatus.Failed => SmppStorage.Entities.DlrStatus.Failed,
            SmppClient.Services.DlrStatus.Expired => SmppStorage.Entities.DlrStatus.Expired,
            SmppClient.Services.DlrStatus.Rejected => SmppStorage.Entities.DlrStatus.Rejected,
            SmppClient.Services.DlrStatus.Unknown => SmppStorage.Entities.DlrStatus.Unknown,
            _ => SmppStorage.Entities.DlrStatus.Unknown
        };
    }

    private static SmsStatus MapToSubmitStatus(SmppClient.Services.DlrStatus status)
    {
        return status switch
        {
            SmppClient.Services.DlrStatus.Delivered => SmsStatus.Delivered,
            SmppClient.Services.DlrStatus.Failed => SmsStatus.Failed,
            SmppClient.Services.DlrStatus.Expired => SmsStatus.Expired,
            SmppClient.Services.DlrStatus.Rejected => SmsStatus.Failed,
            _ => SmsStatus.Unknown
        };
    }

    public void Dispose()
    {
        _dlrProcessor.DlrReceived -= OnDlrReceived;
    }
}
