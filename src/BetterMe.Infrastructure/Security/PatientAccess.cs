using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Data;

namespace BetterMe.Infrastructure.Security;

public static class PatientAccess
{
    public static Task<bool> CanAccessAsync(
        AppDbContext db,
        string callerId,
        bool isAdmin,
        string patientId,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin)
            return Task.FromResult(true);

        return db.Sessions.AnyAsync(
            s => s.PsychologistId == callerId && s.PatientId == patientId,
            cancellationToken);
    }
}
