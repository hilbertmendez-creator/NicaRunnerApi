namespace NicaRunner.Application.Common.Dtos;

public record PaginatedList<T>(List<T> Items, int TotalCount);
