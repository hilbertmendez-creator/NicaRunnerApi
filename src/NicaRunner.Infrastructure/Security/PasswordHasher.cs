using Microsoft.AspNetCore.Identity;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(default!, password);

    public bool Verify(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(default!, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
