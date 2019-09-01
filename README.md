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

## Deploying

1.  `fake build target Publish`
1.  `cf push`
