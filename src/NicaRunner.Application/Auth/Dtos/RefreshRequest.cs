namespace NicaRunner.Application.Auth.Dtos;

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);
