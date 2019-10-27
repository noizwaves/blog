module NoizwavesBlog.Html

open NoizwavesBlog.Domain
open NoizwavesBlog.Markdown
open Suave
open Suave.DotLiquid
open Suave.RequestErrors
open System

type PageLinkDto =
    { title : string
      url : string }

type PostHtmlDto =
    { title : string
      navigablePages : PageLinkDto list
      createdAt : string
      bodyHtml : string }

type PostItemHtmlDto =
    { title : string
      createdAt : string
      link : string }

type PostsHtmlDto =
    { title : string
      navigablePages : PageLinkDto list
      posts : PostItemHtmlDto list }

type PageHtmlDto =
    { title : string
      navigablePages : PageLinkDto list
      bodyHtml : string }

let private formatCreateDate (value : DateTimeOffset) : string = value.ToString("MMM d, yyyy")
let private derivePostUrl (post : BlogPost) : string =
    sprintf "/%04i/%02i/%02i/%s.html" post.slug.year post.slug.month post.slug.day post.slug.name

let private toPageLinks (pages : Page list) : PageLinkDto list =
    pages
        |> List.map (fun p -> { title = p.title; url = sprintf "/pages/%s.html" p.path })
        |> List.sortBy (fun link -> link.title)

// TODO: making this public is suboptimal
let toDto (allPages : Page list) (post : BlogPost) : PostHtmlDto =
    { title = post.title
      navigablePages = allPages |> toPageLinks
      createdAt = post.createdAt |> formatCreateDate
      bodyHtml = post.body |> convertToHtml }

let private toItemDto (post : BlogPost) : PostItemHtmlDto =
    { title = post.title
      createdAt = post.createdAt |> formatCreateDate
      link = post |> derivePostUrl }

// TODO: making this public is suboptimal
let toPageDto (allPages : Page list) (page : Page) : PageHtmlDto =
    { title = page.title
      navigablePages = allPages |> toPageLinks
      bodyHtml = page.body |> convertToHtml }

// TODO: making this public is suboptimal
let toPostsDto (allPages: Page list) (allPosts: BlogPost list) : PostsHtmlDto =
    let posts =
        allPosts
        |> List.sortByDescending (fun p -> p.createdAt)
        |> List.map toItemDto
        
    { title = "Adam Neumann's blog"
      navigablePages = allPages |> toPageLinks
      posts = posts }

// Flows
let handleBlogPost (fetch : FetchPost) (allPages : Page list) (year, month, day, titleSlug) : WebPart =
    request (fun r ->
        let post =
            slugFromUrlParts year month day titleSlug
            |> Option.bind fetch
            |> Option.map (toDto allPages)
        match post with
        | Some dto -> page "post.html.liquid" dto
        | None -> NOT_FOUND "404")

let handleBlogPosts (fetch : FetchPosts) (allPages : Page list) request : WebPart =
    let model =
        fetch()
        |> toPostsDto allPages

    page "posts.html.liquid" model

let handlePage (fetch : FetchPage) (allPages : Page list) (path : string) : WebPart =
    request (fun r ->
        let pageDto =
            path
            |> fetch
            |> Option.map (toPageDto allPages)

        match pageDto with
        | Some dto -> page "page.html.liquid" dto
        | None -> NOT_FOUND "404")
