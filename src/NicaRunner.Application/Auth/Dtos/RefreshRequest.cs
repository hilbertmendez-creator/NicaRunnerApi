namespace NicaRunner.Application.Auth.Dtos;

// RefreshToken es opcional en el body: el cliente web no lo manda (viaja en
// la cookie httpOnly nr_rt, resuelta por AuthController); la app Android
// sigue mandándolo en el body como siempre.
public record RefreshRequest(string? RefreshToken = null);

public record LogoutRequest(string? RefreshToken = null);
