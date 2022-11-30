# [Choice] .NET version: 6.0-focal, 3.1-focal
ARG VARIANT="3.1-focal"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:0-${VARIANT}

RUN dotnet tool install --global fake-cli --version 5.20.4
ENV PATH=$PATH:/root/.dotnet/tools

# [Optional] Uncomment this section to install additional OS packages.
RUN apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
       libsass-dev \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*
