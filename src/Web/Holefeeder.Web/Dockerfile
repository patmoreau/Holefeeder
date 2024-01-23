FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
LABEL author="Drifter Apps Inc."
WORKDIR /app
EXPOSE 5001

ENV ASPNETCORE_URLS=http://+:5001

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src

###################################
# Node and yarn install stage
###################################
# Download and import the Nodesource GPG key
RUN apt-get update
RUN apt-get install -y ca-certificates curl gnupg
RUN mkdir -p /etc/apt/keyrings
RUN curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg

# Create deb repository
ENV NODE_MAJOR=20
RUN echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list

# Run Update and Install
RUN apt-get update
RUN apt-get install nodejs -y

# Install yarn and build-essential
RUN curl -fsSL https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor -o /etc/apt/keyrings/yarnpkg.gpg
RUN echo "deb [signed-by=/etc/apt/keyrings/yarnpkg.gpg] https://dl.yarnpkg.com/debian/ stable main" | tee /etc/apt/sources.list.d/yarn.list
RUN apt-get update
RUN apt-get install -y yarn build-essential

###################################
# Build stage
###################################
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

WORKDIR "/src/src/Web/Holefeeder.Web"
RUN dotnet build "Holefeeder.Web.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Holefeeder.Web.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Holefeeder.Web.dll"]
