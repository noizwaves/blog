module NoizwavesBlog.Html

open NoizwavesBlog.Domain
open NoizwavesBlog.Markdown
open Suave
open Suave.DotLiquid
open Suave.RequestErrors
open System

type PostHtmlDto =
    { title : string
      createdAt : string
      bodyHtml : string }

type PostItemHtmlDto =
    { title : string
      createdAt : string
      link : string }

type PostsHtmlDto =
    { posts : PostItemHtmlDto list }

type PageHtmlDto =
    { title : string
      bodyHtml : string }

let private formatCreateDate (value : DateTime) : string = value.ToString("MMM d, yyyy")
let private derivePostUrl (post : BlogPost) : string =
    sprintf "/%04i/%02i/%02i/%s" post.slug.year post.slug.month post.slug.day post.slug.name

let private toDto (post : BlogPost) : PostHtmlDto =
    { title = post.title
      createdAt = post.createdAt |> formatCreateDate
      bodyHtml = post.body |> convertToHtml }

let private toItemDto (post : BlogPost) : PostItemHtmlDto =
    { title = post.title
      createdAt = post.createdAt |> formatCreateDate
      link = post |> derivePostUrl }

let private toPageDto (page : Page) : PageHtmlDto =
    { title = page.title
      bodyHtml = page.body |> convertToHtml }

// Flows
let handleBlogPost (fetch : FetchPost) (year, month, day, titleSlug) : WebPart =
    request (fun r ->
        let post =
            slugFromUrlParts year month day titleSlug
            |> Option.bind fetch
            |> Option.map toDto
        match post with
        | Some dto -> page "post.html.liquid" dto
        | None -> NOT_FOUND "404")

let handleBlogPosts (fetch : FetchPosts) request : WebPart =
    let posts =
        fetch()
        |> List.sortByDescending (fun p -> p.createdAt)
        |> List.map toItemDto

    let model = { posts = posts }
    page "posts.html.liquid" model

let handlePage (fetch : FetchPage) (path : string) : WebPart =
    request (fun r ->
        let pageDto =
            path
            |> fetch
            |> Option.map toPageDto

        match pageDto with
        | Some dto -> page "page.html.liquid" dto
        | None -> NOT_FOUND "404")
