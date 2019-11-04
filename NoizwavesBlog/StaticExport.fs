module NoizwavesBlog.StaticExport

open System
open NoizwavesBlog.Domain
open System.IO
open Suave.DotLiquid

let private rimraf path =
    if Directory.Exists path then
        Directory.Delete(path, true)

    Directory.CreateDirectory path |> ignore

// http://www.fssnip.net/gO/title/Copy-a-directory-of-files
let rec private directoryCopy srcPath dstPath copySubDirs =
    if not <| System.IO.Directory.Exists(srcPath) then
        let msg = System.String.Format("Source directory does not exist or could not be found: {0}", srcPath)
        raise (System.IO.DirectoryNotFoundException(msg))

    if not <| System.IO.Directory.Exists(dstPath) then
        System.IO.Directory.CreateDirectory(dstPath) |> ignore

    let srcDir = new System.IO.DirectoryInfo(srcPath)

    for file in srcDir.GetFiles() do
        let tempPath = System.IO.Path.Combine(dstPath, file.Name)
        file.CopyTo(tempPath, true) |> ignore

    if copySubDirs then
        for sub in srcDir.GetDirectories() do
            let dstSubDir = System.IO.Path.Combine(dstPath, sub.Name)
            directoryCopy sub.FullName dstSubDir copySubDirs

let private copyContents source destination =
    directoryCopy source destination true

let private copyStaticFiles destination =
    let publicPath = Path.GetFullPath "./public"
    copyContents publicPath destination

let private renderPosts destination posts pages =
    let render post =
        let dto = post |> Html.toDto pages
        let html =
            dto
            |> Suave.DotLiquid.renderPageFile "./templates/post.html.liquid"
            |> Async.RunSynchronously
        (post, html)

    let write (post: BlogPost, html: String) =
        let folder = sprintf "/%04i/%02i/%02i" post.slug.year post.slug.month post.slug.day
        let dir = Path.Join(String.op_Implicit destination, String.op_Implicit folder)
        Directory.CreateDirectory dir |> ignore

        let path = Path.Join(String.op_Implicit dir, String.op_Implicit post.slug.name)
        File.WriteAllText(path, html)

        let filename = post.slug.name + ".html"
        let pathWithExtension = Path.Join(String.op_Implicit dir, String.op_Implicit filename)
        File.WriteAllText(pathWithExtension, html)

    posts
    |> List.map render
    |> List.map write
    |> ignore

let private renderPostList destination posts pages =
    let path = Path.Join(String.op_Implicit destination, String.op_Implicit "index.html")
    let dto = Html.toPostsDto pages posts
    let html =
        dto
        |> Suave.DotLiquid.renderPageFile "./templates/posts.html.liquid"
        |> Async.RunSynchronously
    File.WriteAllText(path, html)

let private renderPages (destination: string) (pages: Page list) =
    let folder = Path.Join(String.op_Implicit destination, String.op_Implicit "pages")
    if not <| Directory.Exists folder then
        Directory.CreateDirectory folder |> ignore

    let render (page: Page) =
        let html =
            page
            |> Html.toPageDto pages
            |> Suave.DotLiquid.renderPageFile "./templates/page.html.liquid"
            |> Async.RunSynchronously
        (page, html)

    let write (page, html) =
        if not <| Directory.Exists folder then
            Directory.CreateDirectory folder |> ignore

        let filename = page.path + ".html"
        let path = Path.Join(String.op_Implicit folder, String.op_Implicit filename)
        File.WriteAllText(path, html)

    pages
    |> List.map render
    |> List.map write
    |> ignore

let private renderAtomFeed destination (posts: BlogPost list) =
    let path = Path.Join(String.op_Implicit destination, String.op_Implicit "atom.xml")
    let content = Atom.sprintAtomFeed posts
    File.WriteAllText(path, content)

printfn "- Before StaticExport.fs `let run`"

let run =
    // Load blog
    setTemplatesDir "./templates"
    setCSharpNamingConvention()
    let published = Persistence.loadPostsFromFolder "_posts"
    let pages = Persistence.loadPagesFromFolder "_pages"

    // Clean output dir
    let outputDir = Path.GetFullPath "./output"
    rimraf outputDir

    // Write blog to files
    copyStaticFiles outputDir
    renderPosts outputDir published pages
    renderPostList outputDir published pages
    renderPages outputDir pages
    renderAtomFeed outputDir published

    0

printfn "- After StaticExport.fs `let run`"
