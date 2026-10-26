# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /source

COPY backend/src/Entrenate.Domain/Entrenate.Domain.csproj backend/src/Entrenate.Domain/
COPY backend/src/Entrenate.Application/Entrenate.Application.csproj backend/src/Entrenate.Application/
COPY backend/src/Entrenate.Infrastructure/Entrenate.Infrastructure.csproj backend/src/Entrenate.Infrastructure/
COPY backend/src/Entrenate.Api/Entrenate.Api.csproj backend/src/Entrenate.Api/

RUN dotnet restore backend/src/Entrenate.Api/Entrenate.Api.csproj

FROM restore AS publish
COPY backend/ backend/
RUN dotnet publish backend/src/Entrenate.Api/Entrenate.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0 \
    PORT=8080

EXPOSE 8080

COPY --from=publish --chown=app:app /app/publish .

USER app

ENTRYPOINT ["sh", "-c", "exec dotnet Entrenate.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
