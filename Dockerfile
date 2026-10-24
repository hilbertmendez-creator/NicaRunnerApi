FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/NicaRunner.Domain/NicaRunner.Domain.csproj src/NicaRunner.Domain/
COPY src/NicaRunner.Application/NicaRunner.Application.csproj src/NicaRunner.Application/
COPY src/NicaRunner.Infrastructure/NicaRunner.Infrastructure.csproj src/NicaRunner.Infrastructure/
COPY src/NicaRunner.Api/NicaRunner.Api.csproj src/NicaRunner.Api/
RUN dotnet restore src/NicaRunner.Api/NicaRunner.Api.csproj

COPY src/ src/
RUN dotnet publish src/NicaRunner.Api/NicaRunner.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# La imagen trae el usuario sin privilegios `app` (APP_UID). Sin esto la API corre
# como root, y una falla en el proceso tendría permiso de escritura sobre todo el
# contenedor. 8080 no es un puerto privilegiado, así que no hace falta nada más.
USER $APP_UID

ENTRYPOINT ["dotnet", "NicaRunner.Api.dll"]
