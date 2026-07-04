using System.ComponentModel.DataAnnotations;

namespace NicaRunner.Application.Categories.Dtos;

public record AssignCategoryRequest([Required] int CategoryId);
