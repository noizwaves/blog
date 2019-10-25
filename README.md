# noizwaves.github.io

Professional blog powered by F#, Suave.IO, and .NET Core. Inspired by Jekyll.

[![CircleCI](https://circleci.com/gh/noizwaves/blog/tree/master.svg?style=svg)](https://circleci.com/gh/noizwaves/blog/tree/master)

## Quick start

1.  `dotnet restore`
1.  `fake build`
1.  `dotnet run -p NoizwavesBlog`
1.  View the [blog](http://localhost:8080)
1.  View a [post](http://localhost:8080/2018/12/10/hello-fsharp-world)

## Dependencies

1.  Download and install [.NET Core SDK 2.2.401]https://dotnet.microsoft.com/download/dotnet-core/2.2#sdk-2.2.401)
1.  Install FAKE 5 via
    1.  `dotnet tool install fake-cli -g`
    1.  adding `$HOME/.dotnet/tools` to `PATH` (as [described here](https://github.com/dotnet/docs/blob/master/docs/core/tools/global-tools.md#install-a-global-tool))
1.  Install `libsass` via
    -   on Linux via `sudo apt install libsass-dev`
    -   on macOS (probably) via `brew install libsass`

## Tests

1.  `dotnet test`

## Deploying

1.  `fake build target Publish`
1.  `cf push`

## Features

### Drafts

Draft posts can be displayed by setting the `DRAFTS` environment variable to a non-empty value.

To see drafts locally, run:
1.  `DRAFTS=true dotnet run -p NoizwavesBlog`

### Static Site generation

In addition to running as a web server, a complete version of the blog can be generated.

1. `dotnet run -p NoizwavesBlog -- static`
1. `cd output`
1. `python -m SimpleHTTPServer`
1. `open http://localhost:8000`