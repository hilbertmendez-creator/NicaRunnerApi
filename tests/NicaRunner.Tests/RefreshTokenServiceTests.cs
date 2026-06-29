using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Security;

namespace NicaRunner.Tests;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repo = new();
    private readonly JwtSettings _settings = new()
    {
        Key = "test",
        Issuer = "test",
        Audience = "test",
        ExpiryMinutes = 60,
        RefreshExpiryDays = 30
    };

    private RefreshTokenService BuildService() =>
        new(_repo.Object, Options.Create(_settings));

    private static User ActiveUser(int id = 1) =>
        new() { Id = id, Email = "test@test", Nombre = "Test", IsActive = true };

    private static string Hash(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // IssueAsync sin familyId abre una familia nueva, hashea, y lo agrega al repo.
    [Fact]
    public async Task IssueAsync_SinFamilyId_AbreFamiliaYHashea()
    {
        RefreshToken? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        var result = await BuildService().IssueAsync(ActiveUser(7));

        Assert.NotEqual(Guid.Empty, result.FamilyId);
        Assert.NotNull(captured);
        Assert.Equal(7, captured!.UserId);
        Assert.Equal(result.FamilyId, captured.FamilyId);
        Assert.Equal(Hash(result.Token), captured.TokenHash);
        // El token plano nunca debe coincidir con el hash persistido.
        Assert.NotEqual(result.Token, captured.TokenHash);
    }

    // IssueAsync con familyId reusa la familia (caso rotación).
    [Fact]
    public async Task IssueAsync_ConFamilyId_PreservaFamilia()
    {
        var existingFamily = Guid.NewGuid();
        RefreshToken? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        var result = await BuildService().IssueAsync(ActiveUser(), existingFamily);

        Assert.Equal(existingFamily, result.FamilyId);
        Assert.Equal(existingFamily, captured!.FamilyId);
    }

    // Replay detection: un token ya revocado por rotación dispara RevokeFamilyAsync
    // con razón ReplayDetected y se rechaza la petición.
    [Fact]
    public async Task ValidateAndRotate_TokenYaRevocado_RevocaFamiliaYRechaza()
    {
        var familyId = Guid.NewGuid();
        var raw = "stolen-token";
        _repo.Setup(r => r.GetByHashAsync(Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                User = ActiveUser(),
                TokenHash = Hash(raw),
                FamilyId = familyId,
                ExpiresAt = DateTime.UtcNow.AddDays(15),
                RevokedAt = DateTime.UtcNow.AddMinutes(-1),
                RevokedReason = RefreshTokenRevokedReason.Rotated
            });

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ValidateAndRotateAsync(raw));

        _repo.Verify(r => r.RevokeFamilyAsync(familyId, RefreshTokenRevokedReason.ReplayDetected, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Token expirado: rechazo, sin tocar la familia (no es replay, solo expiró —
    // el cliente puede tener una copia legítima vieja).
    [Fact]
    public async Task ValidateAndRotate_TokenExpirado_LanzaSinRevocarFamilia()
    {
        var raw = "expired";
        _repo.Setup(r => r.GetByHashAsync(Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                User = ActiveUser(),
                TokenHash = Hash(raw),
                FamilyId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ValidateAndRotateAsync(raw));

        _repo.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Token desconocido: rechazo limpio sin lookup en el repo de revocación.
    [Fact]
    public async Task ValidateAndRotate_TokenDesconocido_Lanza()
    {
        _repo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ValidateAndRotateAsync("does-not-exist"));

        _repo.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Usuario inactivo: aunque el token esté vigente, la cuenta fue deshabilitada
    // — no emite token nuevo.
    [Fact]
    public async Task ValidateAndRotate_UsuarioInactivo_Lanza()
    {
        var raw = "valid-but-user-disabled";
        _repo.Setup(r => r.GetByHashAsync(Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                User = new User { Id = 1, Email = "x@x", Nombre = "X", IsActive = false },
                TokenHash = Hash(raw),
                FamilyId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(15)
            });

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => BuildService().ValidateAndRotateAsync(raw));
    }

    // Happy path: token válido → rota, marca el viejo como Rotated con
    // ReplacedByTokenHash apuntando al nuevo, emite uno nuevo en la misma familia.
    [Fact]
    public async Task ValidateAndRotate_TokenValido_RotaPreservandoFamilia()
    {
        var familyId = Guid.NewGuid();
        var raw = "valid";
        var existing = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            User = ActiveUser(),
            TokenHash = Hash(raw),
            FamilyId = familyId,
            ExpiresAt = DateTime.UtcNow.AddDays(15)
        };
        _repo.Setup(r => r.GetByHashAsync(Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        RefreshToken? newlyAdded = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => newlyAdded = t)
            .Returns(Task.CompletedTask);

        var result = await BuildService().ValidateAndRotateAsync(raw);

        Assert.NotNull(newlyAdded);
        Assert.Equal(familyId, newlyAdded!.FamilyId);
        Assert.Equal(familyId, result.NewToken.FamilyId);

        // El viejo queda marcado como rotated apuntando al nuevo.
        Assert.NotNull(existing.RevokedAt);
        Assert.Equal(RefreshTokenRevokedReason.Rotated, existing.RevokedReason);
        Assert.Equal(Hash(result.NewToken.Token), existing.ReplacedByTokenHash);

        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Logout: revoca la familia entera, idempotente.
    [Fact]
    public async Task Logout_TokenValido_RevocaFamiliaConRazonLogout()
    {
        var familyId = Guid.NewGuid();
        var raw = "active-token";
        _repo.Setup(r => r.GetByHashAsync(Hash(raw), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                TokenHash = Hash(raw),
                FamilyId = familyId,
                ExpiresAt = DateTime.UtcNow.AddDays(15)
            });

        await BuildService().LogoutAsync(raw);

        _repo.Verify(r => r.RevokeFamilyAsync(familyId, RefreshTokenRevokedReason.Logout, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Logout de un token desconocido: no-op idempotente (no lanza).
    [Fact]
    public async Task Logout_TokenDesconocido_NoOp()
    {
        _repo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await BuildService().LogoutAsync("garbage");

        _repo.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Logout con string vacío: no llega ni al repo.
    [Fact]
    public async Task Logout_TokenVacio_NoTocaRepo()
    {
        await BuildService().LogoutAsync("");
        _repo.Verify(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
