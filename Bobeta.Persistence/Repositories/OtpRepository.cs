using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public class OtpRepository : IOtpRepository
{
    private readonly BobetaDbContext _db;

    public OtpRepository(BobetaDbContext db) => _db = db;

    public async Task<OtpCode?> GetLatestByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        await _db.OtpCodes
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OtpCode> AddAsync(OtpCode otp, CancellationToken cancellationToken = default)
    {
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync(cancellationToken);
        return otp;
    }

    public async Task InvalidateAsync(OtpCode otp, CancellationToken cancellationToken = default)
    {
        otp.IsUsed = true;
        _db.OtpCodes.Update(otp);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
