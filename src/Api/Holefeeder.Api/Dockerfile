FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

ENV ASPNETCORE_URLS=http://+:5001

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src

COPY ["DrifterApps.Holefeeder.sln", "."]
COPY [".editorconfig", "."]
COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]
COPY ["nuget.config", "."]

COPY ["src/Api/Holefeeder.Api/Holefeeder.Api.csproj", "src/Api/Holefeeder.Api/"]
COPY ["src/Api/Holefeeder.Application/Holefeeder.Application.csproj", "src/Api/Holefeeder.Application/"]
COPY ["src/Api/Holefeeder.Domain/Holefeeder.Domain.csproj", "src/Api/Holefeeder.Domain/"]
COPY ["src/Api/Holefeeder.FunctionalTests/Holefeeder.FunctionalTests.csproj", "src/Api/Holefeeder.FunctionalTests/"]
COPY ["src/Api/Holefeeder.Infrastructure/Holefeeder.Infrastructure.csproj", "src/Api/Holefeeder.Infrastructure/"]
COPY ["src/Api/Holefeeder.Tests.Common/Holefeeder.Tests.Common.csproj", "src/Api/Holefeeder.Tests.Common/"]
COPY ["src/Api/Holefeeder.UnitTests/Holefeeder.UnitTests.csproj", "src/Api/Holefeeder.UnitTests/"]
COPY ["src/Web/Holefeeder.Web/Holefeeder.Web.csproj", "src/Web/Holefeeder.Web/"]

RUN dotnet restore

COPY ["src/.", "src/"]

WORKDIR "/src/src/Api/Holefeeder.Api"
RUN dotnet build "Holefeeder.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Holefeeder.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Holefeeder.Api.dll"]
