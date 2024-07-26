# Use the specified base image
FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0

# Install NVM, Node.js, and npm
ENV NVM_DIR /usr/local/nvm
ENV NODE_VERSION 20

RUN mkdir -p $NVM_DIR \
    && apt-get update && apt-get install -y curl \
    && curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.1/install.sh | bash \
    && . $NVM_DIR/nvm.sh \
    && nvm install $NODE_VERSION \
    && nvm alias default $NODE_VERSION \
    && nvm use default

ENV NODE_PATH $NVM_DIR/versions/node/v$NODE_VERSION/lib/node_modules
ENV PATH $NVM_DIR/versions/node/v$NODE_VERSION/bin:$PATH
