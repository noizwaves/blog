module NoizwavesBlog.Persistence

open NoizwavesBlog.Domain
open SharpYaml.Serialization
open System
open System.IO

type FrontMatter = Map<String, String>

let private deserializeYaml (raw: string): YamlStream =
    use stream = new StringReader(raw)
    let yamlStream = YamlStream()
    yamlStream.Load stream
    yamlStream

let private parseFrontMatter (yml: string): FrontMatter =
    let yamlStream = deserializeYaml yml
    let doc = (yamlStream.Documents.Item 0).RootNode :?> YamlMappingNode
    if doc = null then failwith "Expected front matter YAML to be a map, it was not"
    doc
    |> Seq.map (fun kvp -> kvp.Key :?> YamlScalarNode, kvp.Value :?> YamlScalarNode)
    |> Seq.filter (fun (k, v) -> k <> null && v <> null)
    |> Seq.map (fun (k, v) -> k.Value, v.Value)
    |> Map.ofSeq

let private parseDelimitedFile (raw: string): FrontMatter * string =
    let split = raw.Split("---") |> Array.toList
    match split with
    | _ :: first :: second :: _ ->
        let document = second |> String.trim

        let frontMatter =
            first
            |> String.trim
            |> parseFrontMatter
        (frontMatter, document)
    | _ -> (Map.empty, raw)

let private fromRawString (filename: string) (raw: string): BlogPost =
    let (frontMatter, body) = parseDelimitedFile raw
    let title = Map.find "title" frontMatter
    let name = String.substring 11 filename

    let filenameCreatedAt: DateTimeOffset =
        match String.split '-' filename with
        | year :: month :: day :: _ -> DateTimeOffset(int year, int month, int day, 0, 0, 0, TimeSpan.Zero)
        | _ -> failwith "Unable to parse date"

    let createdAt: DateTimeOffset =
        Map.tryFind "date" frontMatter
        |> Option.bind parseFromLongString
        |> Option.defaultValue filenameCreatedAt

    let slug =
        { year = createdAt.Year
          month = createdAt.Month
          day = createdAt.Day
          name = name }

    { slug = slug
      title = title
      createdAt = createdAt
      body = body }

let loadPostsFromFolder (folder: string): BlogPost list =
    folder
    |> System.IO.Directory.GetFiles
    |> Array.toList
    |> List.map (fun path ->
        let filename = System.IO.Path.GetFileNameWithoutExtension path
        path
        |> System.IO.File.ReadAllText
        |> fromRawString filename)

let private draftFromRawString (path: string) (filename: string) (raw: string): BlogPost =
    let (frontMatter, body) = parseDelimitedFile raw
    let title = Map.find "title" frontMatter
    let name = filename

    let createdAt: DateTimeOffset =
        Map.tryFind "date" frontMatter
        |> Option.bind parseFromLongString
        |> Option.defaultWith (fun _ -> failwith "Unable to parse date from frontmatter")

    let slug =
        { year = createdAt.Year
          month = createdAt.Month
          day = createdAt.Day
          name = name }

    { slug = slug
      title = title
      createdAt = createdAt
      body = body }

let loadDraftsFromFolder (folder: string): BlogPost list =
    folder
    |> System.IO.Directory.GetFiles
    |> Array.toList
    |> List.filter (fun path -> not (path.EndsWith ".gitkeep"))
    |> List.map (fun path ->
        let filename = System.IO.Path.GetFileNameWithoutExtension path
        path
        |> System.IO.File.ReadAllText
        |> draftFromRawString path filename)

let private pageFromRaw (filename: string) (raw: string): Page =
    let (frontMatter, body) = parseDelimitedFile raw
    let title = Map.find "title" frontMatter
    { path = filename
      title = title
      body = body }

let loadPagesFromFolder (folder: string): Page list =
    folder
    |> System.IO.Directory.GetFiles
    |> Array.toList
    |> List.map (fun path ->
        let filename = System.IO.Path.GetFileNameWithoutExtension path
        path
        |> System.IO.File.ReadAllText
        |> pageFromRaw filename)

let private safeFind predicate list =
    try
        list
        |> List.find predicate
        |> Some
    with :? System.Collections.Generic.KeyNotFoundException -> None

let findPostInList (posts: BlogPost list) (slug: Slug): BlogPost option =
    posts |> safeFind (fun p -> p.slug.Equals(slug))
let findPageInList (pages: Page list) (path: string): Page option =
    pages |> safeFind (fun p -> String.equals path p.path)
