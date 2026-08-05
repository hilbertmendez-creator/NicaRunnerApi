using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

// design.md D2/D3: TryClaimAsync/TryReserveSlotAsync/ReleaseSlotAsync/ReleaseClaimAsync
// son ExecuteUpdateAsync sin transacción ambiente — el rows-affected de cada uno ES el
// veredicto atómico (ni SELECT-then-UPDATE, ni lock explícito). TryReserveSlotAsync y
// ReleaseSlotAsync tocan RaceCategories (no Registrations) porque el cupo pertenece al
// pipeline de confirmación de Registration, no a la asociación RaceCategory en sí
// (tasks.md 2.1 agrupa los tres métodos bajo este archivo + RegistrationLinkRepository).
public class RegistrationRepository(NicaRunnerDbContext context) : IRegistrationRepository
{
    public Task<Registration?> GetByIdAsync(int raceId, int registrationId, CancellationToken ct = default) =>
        context.Registrations
            .Include(r => r.RaceCategory)
            .FirstOrDefaultAsync(r => r.RaceId == raceId && r.Id == registrationId, ct);

    public Task<List<Registration>> GetAllByRaceAsync(int raceId, RegistrationStatus? estado, CancellationToken ct = default)
    {
        var query = context.Registrations.Where(r => r.RaceId == raceId);
        if (estado is not null)
            query = query.Where(r => r.Estado == estado);

        return query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(Registration registration, CancellationToken ct = default) =>
        await context.Registrations.AddAsync(registration, ct);

    public async Task<int> TryClaimAsync(int registrationId, int adminId, DateTime now, CancellationToken ct = default) =>
        await context.Registrations
            .Where(r => r.Id == registrationId && r.Estado == RegistrationStatus.ComprobanteSubido)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Estado, RegistrationStatus.Confirmada)
                .SetProperty(r => r.RevisadoPorUserId, adminId)
                .SetProperty(r => r.RevisadoAt, now), ct);

    public async Task<int> ReleaseClaimAsync(int registrationId, CancellationToken ct = default) =>
        await context.Registrations
            .Where(r => r.Id == registrationId && r.Estado == RegistrationStatus.Confirmada && r.RunnerId == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Estado, RegistrationStatus.ComprobanteSubido)
                .SetProperty(r => r.RevisadoPorUserId, (int?)null)
                .SetProperty(r => r.RevisadoAt, (DateTime?)null), ct);

    public async Task<int> TryReserveSlotAsync(int raceCategoryId, CancellationToken ct = default) =>
        await context.RaceCategories
            .Where(rc => rc.Id == raceCategoryId &&
                         (rc.Capacidad == null || rc.ConfirmedCount < rc.Capacidad))
            .ExecuteUpdateAsync(s => s.SetProperty(rc => rc.ConfirmedCount, rc => rc.ConfirmedCount + 1), ct);

    public async Task<int> ReleaseSlotAsync(int raceCategoryId, CancellationToken ct = default) =>
        await context.RaceCategories
            .Where(rc => rc.Id == raceCategoryId && rc.ConfirmedCount > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(rc => rc.ConfirmedCount, rc => rc.ConfirmedCount - 1), ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
