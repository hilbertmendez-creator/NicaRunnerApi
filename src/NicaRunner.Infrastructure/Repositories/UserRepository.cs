using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;

namespace NicaRunner.Infrastructure.Repositories;

public class UserRepository(NicaRunnerDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await context.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
