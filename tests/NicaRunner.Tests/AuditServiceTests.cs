using NicaRunner.Application.Auditing;
using NicaRunner.Domain.Constants;

namespace NicaRunner.Tests;

public class AuditServiceTests
{
    private readonly FakeAuditLogRepository _repo = new();
    private AuditService BuildService() => new(_repo);

    [Fact]
    public void TrackChanges_CamposSinCambios_NoEncolaNada()
    {
        BuildService().TrackChanges(AuditEntityTypes.Race, 1, autorId: 5,
        [
            new("Nombre", "Igual", "Igual"),
            new("Estado", "Planeada", "Planeada"),
        ]);

        Assert.Empty(_repo.Entries);
    }

    [Fact]
    public void TrackChanges_SoloElCampoQueCambio_EncolaUnaEntrada()
    {
        BuildService().TrackChanges(AuditEntityTypes.Race, 1, autorId: 5,
        [
            new("Nombre", "Igual", "Igual"),
            new("Estado", "Planeada", "EnCurso"),
        ]);

        var entry = Assert.Single(_repo.Entries);
        Assert.Equal("Estado", entry.Campo);
        Assert.Equal("Planeada", entry.ValorAnterior);
        Assert.Equal("EnCurso", entry.ValorNuevo);
        Assert.Equal(5, entry.AutorId);
        Assert.Equal(AuditEntityTypes.Race, entry.EntityType);
        Assert.Equal(1, entry.EntityId);
    }

    [Fact]
    public void TrackChanges_ValorNuloAValor_SeRegistraComoNullLiteral()
    {
        BuildService().TrackChanges(AuditEntityTypes.Category, 2, autorId: 1,
        [
            new("Descripcion", null, "Nueva descripción"),
        ]);

        var entry = Assert.Single(_repo.Entries);
        Assert.Null(entry.ValorAnterior);
        Assert.Equal("Nueva descripción", entry.ValorNuevo);
    }

    [Fact]
    public void TrackChanges_ValorExcedeAnchoDeColumna_SeTrunca()
    {
        var largo = new string('x', 2000);

        BuildService().TrackChanges(AuditEntityTypes.User, 1, autorId: 1, [new("Nombre", "a", largo)]);

        var entry = Assert.Single(_repo.Entries);
        Assert.Equal(1024, entry.ValorNuevo!.Length);
    }

    [Fact]
    public async Task GetHistoryAsync_DelegaAlRepositorio()
    {
        BuildService().TrackChanges(AuditEntityTypes.User, 3, autorId: 1, [new("Nombre", "A", "B")]);

        var history = await BuildService().GetHistoryAsync(AuditEntityTypes.User, 3);

        var entry = Assert.Single(history);
        Assert.Equal("Nombre", entry.Campo);
    }
}
