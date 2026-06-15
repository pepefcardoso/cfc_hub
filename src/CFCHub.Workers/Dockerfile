FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/CFCHub.Workers/CFCHub.Workers.csproj", "src/CFCHub.Workers/"]
COPY ["src/CFCHub.Application/CFCHub.Application.csproj", "src/CFCHub.Application/"]
COPY ["src/CFCHub.Domain/CFCHub.Domain.csproj", "src/CFCHub.Domain/"]
COPY ["src/CFCHub.Infrastructure/CFCHub.Infrastructure.csproj", "src/CFCHub.Infrastructure/"]
RUN dotnet restore "./src/CFCHub.Workers/CFCHub.Workers.csproj"
COPY . .
WORKDIR "/src/src/CFCHub.Workers"
RUN dotnet build "./CFCHub.Workers.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CFCHub.Workers.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CFCHub.Workers.dll"]
