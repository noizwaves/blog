#!/usr/bin/env bash
set -e

dotnet restore
fake build
