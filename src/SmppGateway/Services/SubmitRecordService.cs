using SmppClient.Services;
using SmppStorage.Entities;
using SmppStorage.Repositories;
using SmppStorage.Data;
using Microsoft.EntityFrameworkCore;

namespace SmppGateway.Services;

public interface ISubmitRecordService
{
    Task<SmsSubmitEntity> CreateSubmitRecordAsync(
        Guid userId,
        string localId,
        string mobile,
        string content,
        int segmentCount,
        string? accountId = null);
    
    Task UpdateSubmitStatusAsync(string localId, string messageId, string? accountId, SmsStatus status);
    
    Task<SmsSubmitEntity?> GetSubmitByLocalIdAsync(string localId);
    
    Task<SmsSubmitEntity?> GetSubmitByMessageIdAsync(string messageId);
    
    Task<IEnumerable<SmsSubmitEntity>> GetUserSubmitsAsync(Guid userId, DateTime? from = null, DateTime? to = null, int limit = 100);
}

public class SubmitRecordService : ISubmitRecordService
{
    private readonly SmppDbContext _context;
    private readonly ILogger<SubmitRecordService> _logger;

    public SubmitRecordService(SmppDbContext context, ILogger<SubmitRecordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SmsSubmitEntity> CreateSubmitRecordAsync(
        Guid userId,
        string localId,
        string mobile,
        string content,
        int segmentCount,
        string? accountId = null)
    {
        var submit = new SmsSubmitEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LocalId = localId,
            MessageId = string.Empty,
            Mobile = mobile,
            Content = content,
            SegmentCount = segmentCount,
            AccountId = accountId,
            Status = SmsStatus.Pending,
            SubmitTime = DateTime.UtcNow
        };

        _context.SmsSubmits.Add(submit);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Created submit record: {LocalId}", localId);
        return submit;
    }

    public async Task UpdateSubmitStatusAsync(string localId, string messageId, string? accountId, SmsStatus status)
    {
        var submit = await _context.SmsSubmits.FirstOrDefaultAsync(s => s.LocalId == localId);
        if (submit != null)
        {
            if (!string.IsNullOrEmpty(messageId))
                submit.MessageId = messageId;
            if (!string.IsNullOrEmpty(accountId))
                submit.AccountId = accountId;
            submit.Status = status;
            if (status == SmsStatus.Delivered || status == SmsStatus.Failed)
            {
                submit.DlrTime = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            _logger.LogDebug("Updated submit status: {LocalId} -> {Status}", localId, status);
        }
    }

    public async Task<SmsSubmitEntity?> GetSubmitByLocalIdAsync(string localId)
    {
        return await _context.SmsSubmits
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.LocalId == localId);
    }

    public async Task<SmsSubmitEntity?> GetSubmitByMessageIdAsync(string messageId)
    {
        return await _context.SmsSubmits
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.MessageId == messageId);
    }

    public async Task<IEnumerable<SmsSubmitEntity>> GetUserSubmitsAsync(Guid userId, DateTime? from = null, DateTime? to = null, int limit = 100)
    {
        var query = _context.SmsSubmits.Where(s => s.UserId == userId);

        if (from.HasValue)
            query = query.Where(s => s.SubmitTime >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.SubmitTime <= to.Value);

        return await query
            .OrderByDescending(s => s.SubmitTime)
            .Take(limit)
            .ToListAsync();
    }
}
