ARG TAG="0.204.1-6.0-jammy"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:${TAG}

RUN dotnet tool install --global fake-cli --version 5.23.0
ENV PATH=$PATH:/root/.dotnet/tools

# [Optional] Uncomment this section to install additional OS packages.
RUN apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
       libsass-dev \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*
