using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmppClient.Protocol;
using SmppGateway.Models;
using SmppGateway.Services;

namespace SmppGateway.Controllers;

[ApiController]
[Route("api/v1/sms")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmppClientManager _smppClientManager;
    private readonly ILogger<SmsController> _logger;

    public SmsController(
        ISmppClientManager smppClientManager,
        ILogger<SmsController> logger)
    {
        _smppClientManager = smppClientManager;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<ApiResponse<SubmitSmsResponse>> Submit([FromBody] SubmitSmsRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var submitService = _smppClientManager.GetSubmitService();
            var submitRequest = new SubmitRequest
            {
                Mobile = request.Mobile,
                Content = request.Content,
                Ext = request.Ext
            };

            var result = await submitService.SubmitAsync(submitRequest);

            if (result.Success)
            {
                _logger.LogInformation("SMS submitted: {MessageId} to {Mobile}", 
                    result.MessageId, request.Mobile);

                return ApiResponse<SubmitSmsResponse>.Success(new SubmitSmsResponse
                {
                    MessageId = result.MessageId,
                    Mobile = request.Mobile
                });
            }

            return ApiResponse<SubmitSmsResponse>.Fail(1, result.ErrorMessage ?? "Submit failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit SMS to {Mobile}", request.Mobile);
            return ApiResponse<SubmitSmsResponse>.Fail(1, ex.Message);
        }
    }

    [HttpPost("batch")]
    public async Task<ApiResponse<BatchSubmitSmsResponse>> BatchSubmit([FromBody] BatchSubmitSmsRequest request)
    {
        var results = new List<SubmitSmsResponse>();
        var successCount = 0;
        var failCount = 0;

        var submitService = _smppClientManager.GetSubmitService();

        foreach (var item in request.List)
        {
            try
            {
                var submitRequest = new SubmitRequest
                {
                    Mobile = item.Mobile,
                    Content = item.Content,
                    Ext = item.Ext
                };

                var result = await submitService.SubmitAsync(submitRequest);

                if (result.Success)
                {
                    results.Add(new SubmitSmsResponse
                    {
                        MessageId = result.MessageId,
                        Mobile = item.Mobile
                    });
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch item failed for {Mobile}", item.Mobile);
                failCount++;
            }
        }

        return ApiResponse<BatchSubmitSmsResponse>.Success(new BatchSubmitSmsResponse
        {
            SuccessCount = successCount,
            FailCount = failCount,
            Results = results
        });
    }

    [HttpGet("status/{messageId}")]
    public async Task<ApiResponse<object>> GetStatus(string messageId)
    {
        var dlrProcessor = _smppClientManager.GetDlrProcessor();
        var record = dlrProcessor.GetRecord(messageId);

        if (record == null)
        {
            return ApiResponse<object>.Fail(1, "Message not found");
        }

        return ApiResponse<object>.Success(new
        {
            record.MessageId,
            record.Mobile,
            Status = record.Status.ToString(),
            record.SubmitTime,
            record.DlrTime,
            Delay = record.DlrTime.HasValue 
                ? (record.DlrTime.Value - record.SubmitTime).TotalSeconds 
                : (double?)null
        });
    }
}
