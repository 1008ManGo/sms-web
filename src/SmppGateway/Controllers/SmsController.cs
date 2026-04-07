using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmppClient.Protocol;
using SmppGateway.Models;
using SmppGateway.Services;
using SmppStorage.Entities;

namespace SmppGateway.Controllers;

[ApiController]
[Route("api/v1/sms")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmppClientManager _smppClientManager;
    private readonly IPermissionService _permissionService;
    private readonly IBillingService _billingService;
    private readonly ISubmitRecordService _submitRecordService;
    private readonly IDlrRecordService _dlrRecordService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(
        ISmppClientManager smppClientManager,
        IPermissionService permissionService,
        IBillingService billingService,
        ISubmitRecordService submitRecordService,
        IDlrRecordService dlrRecordService,
        ILogger<SmsController> logger)
    {
        _smppClientManager = smppClientManager;
        _permissionService = permissionService;
        _billingService = billingService;
        _submitRecordService = submitRecordService;
        _dlrRecordService = dlrRecordService;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<ApiResponse<SubmitSmsResponse>> Submit([FromBody] SubmitSmsRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return ApiResponse<SubmitSmsResponse>.Fail(401, "Unauthorized");
        }

        try
        {
            var permissionResult = await _permissionService.CheckSendPermissionAsync(userId, request.Mobile);
            if (!permissionResult.Allowed)
            {
                return ApiResponse<SubmitSmsResponse>.Fail(2, permissionResult.ErrorMessage ?? "Permission denied");
            }

            var submitService = _smppClientManager.GetSubmitService();
            var submitRequest = new SubmitRequest
            {
                UserId = userId,
                Mobile = request.Mobile,
                Content = request.Content,
                Ext = request.Ext
            };

            var result = await submitService.SubmitAsync(submitRequest);

            if (result.Success)
            {
                await _submitRecordService.UpdateSubmitStatusAsync(
                    result.LocalId,
                    result.MessageId,
                    null,
                    SmsStatus.Submitted);

                await _dlrRecordService.CreateDlrRecordAsync(
                    userId,
                    result.MessageId,
                    result.LocalId,
                    request.Mobile,
                    request.Content);

                _logger.LogInformation("SMS submitted: {MessageId} to {Mobile}", 
                    result.MessageId, request.Mobile);

                return ApiResponse<SubmitSmsResponse>.Success(new SubmitSmsResponse
                {
                    MessageId = result.LocalId,
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
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return ApiResponse<BatchSubmitSmsResponse>.Fail(401, "Unauthorized");
        }

        var results = new List<SubmitSmsResponse>();
        var successCount = 0;
        var failCount = 0;

        var submitService = _smppClientManager.GetSubmitService();

        foreach (var item in request.List)
        {
            try
            {
                var permissionResult = await _permissionService.CheckSendPermissionAsync(userId, item.Mobile);
                if (!permissionResult.Allowed)
                {
                    failCount++;
                    continue;
                }

                var submitRequest = new SubmitRequest
                {
                    UserId = userId,
                    Mobile = item.Mobile,
                    Content = item.Content,
                    Ext = item.Ext
                };

                var result = await submitService.SubmitAsync(submitRequest);

                if (result.Success)
                {
                    await _submitRecordService.UpdateSubmitStatusAsync(
                        result.LocalId,
                        result.MessageId,
                        null,
                        SmsStatus.Submitted);

                    await _dlrRecordService.CreateDlrRecordAsync(
                        userId,
                        result.MessageId,
                        result.LocalId,
                        item.Mobile,
                        item.Content);

                    results.Add(new SubmitSmsResponse
                    {
                        MessageId = result.LocalId,
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

    [HttpGet("status/{localId}")]
    public async Task<ApiResponse<object>> GetStatus(string localId)
    {
        var submit = await _submitRecordService.GetSubmitByLocalIdAsync(localId);

        if (submit == null)
        {
            var dlr = await _dlrRecordService.GetDlrByLocalIdAsync(localId);
            if (dlr == null)
            {
                return ApiResponse<object>.Fail(1, "Message not found");
            }

            return ApiResponse<object>.Success(new
            {
                dlr.LocalId,
                dlr.Mobile,
                Status = dlr.Status.ToString(),
                dlr.SubmitTime,
                dlr.DlrTime,
                Delay = dlr.DlrTime.HasValue
                    ? (dlr.DlrTime.Value - dlr.SubmitTime).TotalSeconds
                    : (double?)null,
                dlr.ErrorCode
            });
        }

        return ApiResponse<object>.Success(new
        {
            submit.LocalId,
            submit.Mobile,
            Status = submit.Status.ToString(),
            submit.SubmitTime,
            submit.DlrTime,
            submit.ErrorCode,
            Delay = submit.DlrTime.HasValue
                ? (submit.DlrTime.Value - submit.SubmitTime).TotalSeconds
                : (double?)null
        });
    }

    [HttpGet("history")]
    public async Task<ApiResponse<object>> GetHistory(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return ApiResponse<object>.Fail(401, "Unauthorized");
        }

        var submits = await _submitRecordService.GetUserSubmitsAsync(userId, from, to, limit);

        var result = submits.Select(s => new
        {
            s.LocalId,
            s.Mobile,
            s.Content,
            s.Status,
            s.SubmitTime,
            s.DlrTime,
            s.ErrorCode,
            Delay = s.DlrTime.HasValue
                ? (s.DlrTime.Value - s.SubmitTime).TotalSeconds
                : (double?)null
        });

        return ApiResponse<object>.Success(result);
    }

    [HttpGet("countries")]
    public IActionResult GetAllowedCountries()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return ApiResponse<object>.Fail(401, "Unauthorized");
        }

        var countries = CountryCodeMapper.GetAllCountryCodes()
            .Select(code => CountryCodeMapper.GetCountryInfo(code))
            .Where(c => c != null)
            .Select(c => new { c!.CountryCode, c.Name, c.Prefix })
            .OrderBy(c => c.CountryCode);

        return ApiResponse<object>.Success(countries);
    }
}
