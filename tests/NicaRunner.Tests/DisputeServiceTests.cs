using Moq;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Application.Controversias;
using NicaRunner.Application.Controversias.Dtos;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class DisputeServiceTests
{
    private readonly Mock<ITimingDisputeRepository> _disputes = new();
    private readonly Mock<IRaceRepository> _races = new();
    private readonly Mock<IResultRepository> _results = new();
    private readonly Mock<IResultAuditRepository> _audits = new();

    private DisputeService BuildService() =>
        new(_disputes.Object, _races.Object, _results.Object, _audits.Object);

    private static Race RaceWithStart() => new()
    {
        Id = 1,
        Nombre = "5K",
        RaceStartUtc = new DateTime(2026, 8, 2, 14, 0, 0, DateTimeKind.Utc),
        AdminId = 1
    };

    private static TimingDispute SampleDispute(
        double? chip = 100, double? checkpoint = 101, double? camera = null) => new()
    {
        Id = 7,
        RaceId = 1,
        ResultId = 40,
        RunnerId = 5,
        Dorsal = "118",
        CorredorNombre = "Ana Pérez",
        ChipSeconds = chip,
        CheckpointSeconds = checkpoint,
        CameraSeconds = camera,
        Estado = DisputeEstado.Disputa,
        CapturistaNombre = "M. López"
    };

    private void SetupRaceAndDispute(TimingDispute? dispute = null)
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceWithStart());
        _disputes.Setup(d => d.GetByIdAsync(1, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute ?? SampleDispute());
    }

    [Fact]
    public async Task ListAsync_FiltraPorEstado()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceWithStart());
        _disputes.Setup(d => d.GetByRaceAsync(1, DisputeEstado.Disputa, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([SampleDispute()]);

        var list = await BuildService().ListAsync(1, DisputeEstado.Disputa);

        Assert.Single(list);
        Assert.Equal(DisputeEstado.Disputa, list[0].Estado);
        Assert.Equal("118", list[0].Dorsal);
    }

    [Fact]
    public async Task CountOpenAsync_CuentaRevisionYDisputa()
    {
        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(RaceWithStart());
        var open = SampleDispute();
        open.Estado = DisputeEstado.Revision;
        var closed = SampleDispute();
        closed.Id = 8;
        closed.Estado = DisputeEstado.Oficial;
        _disputes.Setup(d => d.GetByRaceAsync(1, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([open, closed, SampleDispute()]);

        var count = await BuildService().CountOpenAsync(1);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ResolveAsync_Lector_LanzaForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            BuildService().ResolveAsync(1, 7, new ResolveDisputeRequest(DisputeEstado.Disputa),
                actorUserId: 2, actorRole: UserRole.Lector));

        _disputes.Verify(d => d.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, TimingSource.Checkpoint)] // sin confirm
    [InlineData(true, null)] // sin fuente
    public async Task ResolveAsync_OficialIncompleto_LanzaValidation(bool confirm, TimingSource? source)
    {
        SetupRaceAndDispute();
        var request = new ResolveDisputeRequest(DisputeEstado.Oficial, source, confirm);

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().ResolveAsync(1, 7, request, actorUserId: 9, actorRole: UserRole.Administrador));
    }

    [Fact]
    public async Task ResolveAsync_OficialConFuenteNula_LanzaValidation()
    {
        SetupRaceAndDispute(SampleDispute(camera: null));
        var request = new ResolveDisputeRequest(DisputeEstado.Oficial, TimingSource.Camera, true);

        await Assert.ThrowsAsync<ValidationException>(() =>
            BuildService().ResolveAsync(1, 7, request, actorUserId: 9, actorRole: UserRole.Administrador));
    }

    [Fact]
    public async Task ResolveAsync_OficialConfirmado_AplicaResultYAudit()
    {
        var race = RaceWithStart();
        var dispute = SampleDispute(checkpoint: 101.5);
        var result = new Result
        {
            Id = 40,
            RaceId = 1,
            TiempoLlegada = race.RaceStartUtc!.Value.AddSeconds(99),
            CapturistaId = 1
        };
        ResultAudit? audit = null;

        _races.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(race);
        _disputes.Setup(d => d.GetByIdAsync(1, 7, It.IsAny<CancellationToken>())).ReturnsAsync(dispute);
        _results.Setup(r => r.GetByIdAsync(1, 40, It.IsAny<CancellationToken>())).ReturnsAsync(result);
        _audits.Setup(a => a.AddAsync(It.IsAny<ResultAudit>(), It.IsAny<CancellationToken>()))
            .Callback<ResultAudit, CancellationToken>((a, _) => audit = a)
            .Returns(Task.CompletedTask);

        var dto = await BuildService().ResolveAsync(
            1, 7,
            new ResolveDisputeRequest(DisputeEstado.Oficial, TimingSource.Checkpoint, true),
            actorUserId: 9, actorRole: UserRole.Administrador);

        var expected = race.RaceStartUtc.Value.AddSeconds(101.5);
        Assert.Equal(DisputeEstado.Oficial, dto.Estado);
        Assert.Equal(9, dto.ResolvedByUserId);
        Assert.NotNull(dto.ResolvedAt);
        Assert.Equal(expected, result.TiempoLlegada);
        Assert.NotNull(audit);
        Assert.Equal("TiempoLlegada", audit!.CampoModificado);
        Assert.Equal("Controversia resuelta: Checkpoint", audit.Razon);
        Assert.Equal(9, audit.ActorUserId);
        _disputes.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_AdminADisputa_NoTocaResult()
    {
        SetupRaceAndDispute();

        var dto = await BuildService().ResolveAsync(
            1, 7, new ResolveDisputeRequest(DisputeEstado.Disputa),
            actorUserId: 9, actorRole: UserRole.Administrador);

        Assert.Equal(DisputeEstado.Disputa, dto.Estado);
        Assert.Null(dto.ResolvedByUserId);
        _results.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _audits.Verify(a => a.AddAsync(It.IsAny<ResultAudit>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
