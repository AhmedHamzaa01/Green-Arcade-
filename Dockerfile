# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY GreenArcade.slnx .
COPY src/GreenArcade.Api/GreenArcade.Api.csproj src/GreenArcade.Api/
RUN dotnet restore src/GreenArcade.Api/GreenArcade.Api.csproj

COPY src/GreenArcade.Api/ src/GreenArcade.Api/
RUN dotnet publish src/GreenArcade.Api/GreenArcade.Api.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "GreenArcade.Api.dll"]
