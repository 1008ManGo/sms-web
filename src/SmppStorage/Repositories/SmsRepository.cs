using Microsoft.EntityFrameworkCore;
using SmppStorage.Data;
using SmppStorage.Entities;

namespace SmppStorage.Repositories;

public interface ISmsSubmitRepository
{
    Task<SmsSubmitEntity> CreateAsync(SmsSubmitEntity submit);
    Task<SmsSubmitEntity?> GetByIdAsync(Guid id);
    Task<SmsSubmitEntity?> GetByLocalIdAsync(string localId);
    Task<SmsSubmitEntity?> GetByMessageIdAsync(string messageId);
    Task UpdateStatusAsync(Guid id, SmsStatus status, DateTime? dlrTime = null, string? errorCode = null);
    Task<IEnumerable<SmsSubmitEntity>> GetByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, int limit = 100);
    Task<int> GetSubmitCountByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null);
}

public class SmsSubmitRepository : ISmsSubmitRepository
{
    private readonly SmppDbContext _context;

    public SmsSubmitRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<SmsSubmitEntity> CreateAsync(SmsSubmitEntity submit)
    {
        _context.SmsSubmits.Add(submit);
        await _context.SaveChangesAsync();
        return submit;
    }

    public async Task<SmsSubmitEntity?> GetByIdAsync(Guid id)
    {
        return await _context.SmsSubmits
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SmsSubmitEntity?> GetByLocalIdAsync(string localId)
    {
        return await _context.SmsSubmits
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.LocalId == localId);
    }

    public async Task<SmsSubmitEntity?> GetByMessageIdAsync(string messageId)
    {
        return await _context.SmsSubmits
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.MessageId == messageId);
    }

    public async Task UpdateStatusAsync(Guid id, SmsStatus status, DateTime? dlrTime = null, string? errorCode = null)
    {
        var submit = await _context.SmsSubmits.FindAsync(id);
        if (submit != null)
        {
            submit.Status = status;
            if (dlrTime.HasValue)
                submit.DlrTime = dlrTime;
            if (errorCode != null)
                submit.ErrorCode = errorCode;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<SmsSubmitEntity>> GetByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null, int limit = 100)
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

    public async Task<int> GetSubmitCountByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.SmsSubmits.Where(s => s.UserId == userId);

        if (from.HasValue)
            query = query.Where(s => s.SubmitTime >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.SubmitTime <= to.Value);

        return await query.CountAsync();
    }
}

public interface IDlrRepository
{
    Task<DlrRecordEntity> CreateAsync(DlrRecordEntity dlr);
    Task<DlrRecordEntity?> GetByMessageIdAsync(string messageId);
    Task<DlrRecordEntity?> GetByLocalIdAsync(string localId);
    Task UpdateStatusAsync(string messageId, DlrStatus status, DateTime dlrTime, string? errorCode = null);
    Task<int> GetPendingCountAsync();
}

public class DlrRepository : IDlrRepository
{
    private readonly SmppDbContext _context;

    public DlrRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<DlrRecordEntity> CreateAsync(DlrRecordEntity dlr)
    {
        _context.DlrRecords.Add(dlr);
        await _context.SaveChangesAsync();
        return dlr;
    }

    public async Task<DlrRecordEntity?> GetByMessageIdAsync(string messageId)
    {
        return await _context.DlrRecords
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.MessageId == messageId);
    }

    public async Task<DlrRecordEntity?> GetByLocalIdAsync(string localId)
    {
        return await _context.DlrRecords
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.LocalId == localId);
    }

    public async Task UpdateStatusAsync(string messageId, DlrStatus status, DateTime dlrTime, string? errorCode = null)
    {
        var dlr = await _context.DlrRecords.FirstOrDefaultAsync(d => d.MessageId == messageId);
        if (dlr != null)
        {
            dlr.Status = status;
            dlr.DlrTime = dlrTime;
            if (errorCode != null)
                dlr.ErrorCode = errorCode;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.DlrRecords.CountAsync(d => d.Status == DlrStatus.Pending);
    }
}
