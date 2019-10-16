module NoizwavesBlog.Tests.AtomTests

open System
open System.Text
open Xunit
open FsUnit.Xunit
open Suave
open NoizwavesBlog.Atom
open NoizwavesBlog.Domain

let private expectSome hc =
    match hc with
    | Some a -> a
    | None -> failwith "Expected 'Some'"

let private expectStatus response = response.status.code

let private expectContentType (response : HttpResult) : string =
    response.headers
    |> List.filter (fun (k, _) -> k = "Content-Type")
    |> List.map snd
    |> List.tryHead
    |> fun ct ->
        match ct with
        | None -> failwith "Expected 'Content-Type' header"
        | Some value -> value

let private expectBytes response =
    match response.content with
    | Bytes bytes -> bytes
    | _ -> failwith "Expected 'Bytes'"

[<Fact>]
let ``No posts``() =
    let noPosts = fun () -> []
    let subject = handleAtomFeed noPosts

    let context =
        HttpContext.empty
        |> subject
        |> Async.RunSynchronously
        |> expectSome

    context.response
    |> expectStatus
    |> should equal 200
    
    context.response
    |> expectContentType
    |> should equal "application/atom+xml"

    context.response
    |> expectBytes
    |> Encoding.Default.GetString
    |> should equal """<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom">
  <title>Adam Neumann's blog</title>
  <link href="https://blog.noizwaves.io/" />
  <link href="https://blog.noizwaves.io/feed.atom" rel="self" />
  <updated>2003-12-13T18:30:02Z</updated>
  <author>
    <name>Adam Neumann</name>
  </author>
  <id>https://blog.noizwaves.io/</id>
</feed>
"""

[<Fact>]
let ``Single post``() =
    let singlePost : FetchPosts = fun () ->
        [ { slug = { year = 2019; month = 9; day = 23; name = "foo-bar" };
            title = "Foo Bar";
            createdAt = new DateTimeOffset(2020, 10, 24, 11, 22, 33, TimeSpan.Zero);
            body = """Baz Qux"""
            } ]
    
    let subject = handleAtomFeed singlePost
    
    let context =
        HttpContext.empty
        |> subject
        |> Async.RunSynchronously
        |> expectSome
    
    context.response
        |> expectBytes
        |> Encoding.Default.GetString
        |> should equal """<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom">
  <title>Adam Neumann's blog</title>
  <link href="https://blog.noizwaves.io/" />
  <link href="https://blog.noizwaves.io/feed.atom" rel="self" />
  <updated>2003-12-13T18:30:02Z</updated>
  <author>
    <name>Adam Neumann</name>
  </author>
  <id>https://blog.noizwaves.io/</id>
  <entry>
    <title>Foo Bar</title>
    <link href="https://blog.noizwaves.io/2019/09/23/foo-bar" />
    <content type="html">&lt;p&gt;Baz Qux&lt;/p&gt;
</content>
    <id>https://blog.noizwaves.io/2019/09/23/foo-bar</id>
    <updated>2020-10-24T11:22:33Z</updated>
    <summary>Baz Qux
</summary>
  </entry>
</feed>
"""

[<Fact>]
let ``Summary is only plain text``() =
    let singlePost : FetchPosts = fun () ->
        [ { slug = { year = 2019; month = 9; day = 23; name = "foo-bar" };
            title = "Foo Bar";
            createdAt = new DateTimeOffset(2020, 10, 24, 11, 22, 33, TimeSpan.Zero);
            body = """*hello*"""
            } ]

    let subject = handleAtomFeed singlePost

    let context =
        HttpContext.empty
        |> subject
        |> Async.RunSynchronously
        |> expectSome

    context.response
        |> expectBytes
        |> Encoding.Default.GetString
        |> should equal """<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom">
  <title>Adam Neumann's blog</title>
  <link href="https://blog.noizwaves.io/" />
  <link href="https://blog.noizwaves.io/feed.atom" rel="self" />
  <updated>2003-12-13T18:30:02Z</updated>
  <author>
    <name>Adam Neumann</name>
  </author>
  <id>https://blog.noizwaves.io/</id>
  <entry>
    <title>Foo Bar</title>
    <link href="https://blog.noizwaves.io/2019/09/23/foo-bar" />
    <content type="html">&lt;p&gt;&lt;em&gt;hello&lt;/em&gt;&lt;/p&gt;
</content>
    <id>https://blog.noizwaves.io/2019/09/23/foo-bar</id>
    <updated>2020-10-24T11:22:33Z</updated>
    <summary>hello
</summary>
  </entry>
</feed>
"""