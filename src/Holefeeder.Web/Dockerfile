###################################
# Node and pnpm install stage
###################################
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS pre-build
WORKDIR /source

# Download and import the Nodesource GPG key
RUN apt-get update
RUN apt-get install -y ca-certificates curl gnupg
RUN mkdir -p /etc/apt/keyrings
RUN curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg

# Create deb repository
ENV NODE_MAJOR=20
RUN echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list

# Run Update and Install in a single RUN command to ensure freshness of package lists
# Adding a retry mechanism with apt-get install
RUN apt-get update && \
    apt-get install -y --no-install-recommends nodejs build-essential && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install pnpm
RUN npm install -g pnpm

###################################
# Restore stage
###################################
FROM pre-build AS restore

COPY *.sln .
COPY .editorconfig .
COPY global.json .
COPY Directory.Build.props .
COPY Directory.Build.targets .
COPY Directory.Packages.props .
COPY nuget.config .
COPY src/Directory.Build.props src/

COPY src/Holefeeder.Web/*.csproj src/Holefeeder.Web/

RUN dotnet restore src/Holefeeder.Web/Holefeeder.Web.csproj

###################################
# Build stage
###################################
FROM restore AS build
ARG BUILD_VERSION=99.99.99
ARG VERSION=${BUILD_VERSION:-99.99.99}

WORKDIR /source
COPY src/Holefeeder.Web/. src/Holefeeder.Web/.
WORKDIR /source/src/Holefeeder.Web

RUN dotnet publish /property:Version=${VERSION} -c Release -o /app --no-restore

###################################
# Unit tests stage
###################################
FROM build AS angulartests
WORKDIR /source/src/Holefeeder.Web/ClientApp

RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN sh -c 'echo "deb http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list'
RUN apt-get update
RUN apt --fix-broken install
RUN apt-get install google-chrome-stable -y

###################################
# Final stage
###################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
LABEL author="Drifter Apps Inc."
EXPOSE 80
EXPOSE 443

WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Holefeeder.Web.dll"]
