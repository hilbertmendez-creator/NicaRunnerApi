using NicaRunner.Domain.Entities;

namespace NicaRunner.Application.Common.Interfaces;

public record GeneratedToken(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    GeneratedToken GenerateToken(User user);
}
