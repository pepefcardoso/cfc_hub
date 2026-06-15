FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/CFCHub.Api/CFCHub.Api.csproj", "src/CFCHub.Api/"]
COPY ["src/CFCHub.Application/CFCHub.Application.csproj", "src/CFCHub.Application/"]
COPY ["src/CFCHub.Domain/CFCHub.Domain.csproj", "src/CFCHub.Domain/"]
COPY ["src/CFCHub.Infrastructure/CFCHub.Infrastructure.csproj", "src/CFCHub.Infrastructure/"]
RUN dotnet restore "./src/CFCHub.Api/CFCHub.Api.csproj"
COPY . .
WORKDIR "/src/src/CFCHub.Api"
RUN dotnet build "./CFCHub.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CFCHub.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "CFCHub.Api.dll"]
