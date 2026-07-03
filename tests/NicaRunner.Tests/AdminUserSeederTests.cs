using Microsoft.EntityFrameworkCore;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;
using NicaRunner.Infrastructure.Data;
using NicaRunner.Infrastructure.Repositories;
using NicaRunner.Infrastructure.Security;
using NicaRunner.Infrastructure.Seed;

namespace NicaRunner.Tests;

public class AdminUserSeederTests
{
    private static NicaRunnerDbContext BuildInMemoryDbContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<NicaRunnerDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new NicaRunnerDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task SeedAsync_BdVacia_CreaLosTresAdministradores()
    {
        using var db = BuildInMemoryDbContext();
        IUserRepository users = new UserRepository(db);
        IPasswordHasher hasher = new PasswordHasher();

        await AdminUserSeeder.SeedAsync(users, hasher, "temporal123");

        var admins = await db.Users.ToListAsync();
        Assert.Equal(3, admins.Count);
        Assert.All(admins, u => Assert.Equal(UserRole.Administrador, u.Role));
        Assert.All(admins, u => Assert.True(u.MustChangePassword));
        Assert.Contains(admins, u => u.Email == "hilbert.mendez@gmail.com");
        Assert.Contains(admins, u => u.Email == "evr86.skip@gmail.com");
        Assert.Contains(admins, u => u.Email == "edufisica@ymail.com");
    }

    [Fact]
    public async Task SeedAsync_EjecutadoDosVeces_NoDuplicaUsuarios()
    {
        using var db = BuildInMemoryDbContext();
        IUserRepository users = new UserRepository(db);
        IPasswordHasher hasher = new PasswordHasher();

        await AdminUserSeeder.SeedAsync(users, hasher, "temporal123");
        await AdminUserSeeder.SeedAsync(users, hasher, "temporal123");

        Assert.Equal(3, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_UsuarioYaExiste_NoLoSobreescribe()
    {
        using var db = BuildInMemoryDbContext();
        IUserRepository users = new UserRepository(db);
        IPasswordHasher hasher = new PasswordHasher();
        db.Users.Add(new User
        {
            Email = "hilbert.mendez@gmail.com",
            Nombre = "Hilbert (ya editado)",
            Role = UserRole.Administrador,
            PasswordHash = hasher.Hash("password-personalizada"),
            MustChangePassword = false
        });
        await db.SaveChangesAsync();

        await AdminUserSeeder.SeedAsync(users, hasher, "temporal123");

        var existing = await db.Users.SingleAsync(u => u.Email == "hilbert.mendez@gmail.com");
        Assert.Equal("Hilbert (ya editado)", existing.Nombre);
        Assert.False(existing.MustChangePassword);
        Assert.Equal(3, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_SinPasswordConfigurada_NoCreaUsuarios()
    {
        using var db = BuildInMemoryDbContext();
        IUserRepository users = new UserRepository(db);
        IPasswordHasher hasher = new PasswordHasher();

        await AdminUserSeeder.SeedAsync(users, hasher, defaultPassword: null);

        Assert.Equal(0, await db.Users.CountAsync());
    }
}
