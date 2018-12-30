# noizwaves.github.io

Professional blog powered by F#, Suave.IO, and .NET Core. Inspired by Jekyll.

## Quick start

1.  `dotnet restore`
1.  `dotnet run`
1.  View the [blog](http://localhost:8080)
1.  View a [post](http://localhost:8080/2018/12/10/hello-fsharp-world)

## Dependencies

1.  Download and install [.NET Core SDK 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)
1.  Install FAKE 5 via
  1.  `dotnet tool install fake-cli -g`
  1.  adding `$HOME/.dotnet/tools` to `PATH` (as [described here](https://github.com/dotnet/docs/blob/master/docs/core/tools/global-tools.md#install-a-global-tool))

## Deploying

1.  `fake build target Publish`
1.  `cf push`
