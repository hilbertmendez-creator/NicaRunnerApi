using Moq;
using NicaRunner.Application.Categories;
using NicaRunner.Application.Categories.Dtos;
using NicaRunner.Application.Common.Exceptions;
using NicaRunner.Application.Common.Interfaces;
using NicaRunner.Domain.Entities;

namespace NicaRunner.Tests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IRunnerRepository> _runners = new();

    private CategoryService BuildService() => new(_categories.Object, _runners.Object);

    private static CreateCategoryRequest ValidRequest(string codigo = "JUV") =>
        new(codigo, "Juvenil", "13 a 17 años", 5m, 13, 17, 1);

    [Fact]
    public async Task CreateAsync_DatosValidos_CreaCategoriaConCodigoEnMayusculas()
    {
        _categories.Setup(c => c.CodigoExistsAsync("juv", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Category? created = null;
        _categories.Setup(c => c.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, _) => created = c)
            .Returns(Task.CompletedTask);

        var dto = await BuildService().CreateAsync(ValidRequest("juv"));

        Assert.NotNull(created);
        Assert.Equal("JUV", created!.Codigo);
        Assert.Equal("JUV", dto.Codigo);
    }

    [Fact]
    public async Task CreateAsync_EdadMinimaMayorOIgualQueMaxima_LanzaValidation()
    {
        var request = ValidRequest() with { EdadMinima = 18, EdadMaxima = 18 };

        await Assert.ThrowsAsync<ValidationException>(() => BuildService().CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_CodigoDuplicado_LanzaConflict()
    {
        _categories.Setup(c => c.CodigoExistsAsync("juv", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => BuildService().CreateAsync(ValidRequest("juv")));
    }

    [Fact]
    public async Task GetByIdAsync_CategoriaInexistente_LanzaNotFound()
    {
        _categories.Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => BuildService().GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateAsync_ActualizaCamposYNormalizaCodigo()
    {
        var category = new Category { Id = 1, Codigo = "JUV", NombreCategoria = "Juvenil", EdadMinima = 13, EdadMaxima = 17 };
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.CodigoExistsAsync("lib", 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dto = await BuildService().UpdateAsync(1, new UpdateCategoryRequest("lib", "Libre", null, 10m, 18, 39, 2));

        Assert.Equal("LIB", category.Codigo);
        Assert.Equal("LIB", dto.Codigo);
        Assert.Equal(18, category.EdadMinima);
    }

    [Fact]
    public async Task DeleteAsync_SinCorredoresInscritos_Elimina()
    {
        var category = new Category { Id = 1, NombreCategoria = "Juvenil" };
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _runners.Setup(r => r.ExistsByCategoryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await BuildService().DeleteAsync(1);

        _categories.Verify(c => c.Remove(category), Times.Once);
        _categories.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ConCorredoresInscritos_LanzaConflict()
    {
        var category = new Category { Id = 1, NombreCategoria = "Juvenil" };
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _runners.Setup(r => r.ExistsByCategoryAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => BuildService().DeleteAsync(1));

        _categories.Verify(c => c.Remove(It.IsAny<Category>()), Times.Never);
    }
}
