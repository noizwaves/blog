#!/usr/bin/env bash
set -euo pipefail

rm -rf public
mise exec -- hugo
rsync -a public/ dell-one:/home/cloud/cloud-data/blog.noizwaves.com/content/
