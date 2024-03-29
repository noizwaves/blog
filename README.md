# noizwaves.github.io

Professional blog powered by F#, Suave.IO, and .NET Core. Inspired by Jekyll.

[![CircleCI](https://circleci.com/gh/noizwaves/blog/tree/master.svg?style=svg)](https://circleci.com/gh/noizwaves/blog/tree/master)

## Quick start

1.  Open in VS Code using Dev Containers
1.  `dotnet restore`
1.  `fake build`
1.  `dotnet run -p NoizwavesBlog`
1.  View the [blog](http://localhost:8080)
1.  View a [post](http://localhost:8080/2018/12/10/hello-fsharp-world)

## Tests

1.  `dotnet test`

## Features

### Drafts

Draft posts can be displayed by setting the `DRAFTS` environment variable to a non-empty value.

To see drafts locally, run:
1.  `DRAFTS=true dotnet run -p NoizwavesBlog`

### Static Site generation

In addition to running as a web server, a complete version of the blog can be generated.

1. `dotnet run -p NoizwavesBlog -- static`
1. `python3 -m http.server --directory output`
1. `open http://localhost:8000`

## TODO

- build the concept of all posts and all pages into the domain
- move HTML generation into HTML and `handle` into WebServer
- pull concept of drafts and visibility into the domain
- use a real YAML deserializer instead of the hand-rolled simple one