using SmppClient.Services;
using SmppStorage.Entities;
using SmppStorage.Repositories;
using SmppStorage.Data;
using Microsoft.EntityFrameworkCore;

namespace SmppGateway.Services;

public interface IDlrRecordService
{
    Task<DlrRecordEntity> CreateDlrRecordAsync(
        Guid userId,
        string messageId,
        string localId,
        string mobile,
        string content);

    Task UpdateDlrStatusAsync(string messageId, DlrStatus status, DateTime dlrTime, string? errorCode = null, string? networkErrorCode = null);

    Task<DlrRecordEntity?> GetDlrByMessageIdAsync(string messageId);

    Task<DlrRecordEntity?> GetDlrByLocalIdAsync(string localId);

    Task<int> GetPendingDlrCountAsync();
}

public class DlrRecordService : IDlrRecordService
{
    private readonly SmppDbContext _context;
    private readonly ILogger<DlrRecordService> _logger;

    public DlrRecordService(SmppDbContext context, ILogger<DlrRecordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DlrRecordEntity> CreateDlrRecordAsync(
        Guid userId,
        string messageId,
        string localId,
        string mobile,
        string content)
    {
        var existing = await _context.DlrRecords.FirstOrDefaultAsync(d => d.MessageId == messageId);
        if (existing != null)
        {
            return existing;
        }

        var dlr = new DlrRecordEntity
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            LocalId = localId,
            Mobile = mobile,
            Content = content,
            Status = DlrStatus.Pending,
            SubmitTime = DateTime.UtcNow,
            UserId = userId
        };

        _context.DlrRecords.Add(dlr);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created DLR record: {MessageId}", messageId);
        return dlr;
    }

    public async Task UpdateDlrStatusAsync(string messageId, DlrStatus status, DateTime dlrTime, string? errorCode = null, string? networkErrorCode = null)
    {
        var dlr = await _context.DlrRecords.FirstOrDefaultAsync(d => d.MessageId == messageId);
        if (dlr != null)
        {
            dlr.Status = status;
            dlr.DlrTime = dlrTime;
            if (errorCode != null)
                dlr.ErrorCode = errorCode;
            if (networkErrorCode != null)
                dlr.NetworkErrorCode = networkErrorCode;
            await _context.SaveChangesAsync();
            _logger.LogDebug("Updated DLR status: {MessageId} -> {Status}", messageId, status);
        }
    }

    public async Task<DlrRecordEntity?> GetDlrByMessageIdAsync(string messageId)
    {
        return await _context.DlrRecords
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.MessageId == messageId);
    }

    public async Task<DlrRecordEntity?> GetDlrByLocalIdAsync(string localId)
    {
        return await _context.DlrRecords
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.LocalId == localId);
    }

    public async Task<int> GetPendingDlrCountAsync()
    {
        return await _context.DlrRecords.CountAsync(d => d.Status == DlrStatus.Pending);
    }
}
