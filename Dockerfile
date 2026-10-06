# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes.
COPY Directory.Build.props ./
COPY backend/src/RowCycle.Api/RowCycle.Api.csproj backend/src/RowCycle.Api/
COPY backend/src/RowCycle.Application/RowCycle.Application.csproj backend/src/RowCycle.Application/
COPY backend/src/RowCycle.Domain/RowCycle.Domain.csproj backend/src/RowCycle.Domain/
COPY backend/src/RowCycle.Infrastructure/RowCycle.Infrastructure.csproj backend/src/RowCycle.Infrastructure/
RUN dotnet restore backend/src/RowCycle.Api/RowCycle.Api.csproj

COPY backend/src/ backend/src/
RUN dotnet publish backend/src/RowCycle.Api/RowCycle.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "RowCycle.Api.dll"]
