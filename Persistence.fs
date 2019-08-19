module NoizwavesBlog.Persistence

open NoizwavesBlog.Domain
open System

type ShallowYaml = Map<String, String>

let private shallowYamlDecode (yml : string) : ShallowYaml =
    yml
    |> String.split '\n'
    |> List.map (fun s ->
           let index = s.IndexOf(":")
           let key = s.Substring(0, index).Trim()
           let value = s.Substring(index + 1).Trim().Trim('\"')
           (key, value))
    |> Map.ofList

let private parseDelimitedFile (raw : string) : ShallowYaml * string =
    let split = raw.Split("---")
    if (split.Length >= 2) then
        let document = Array.get split (split.Length - 1) |> String.trim

        let frontmatter =
            Array.get split (split.Length - 2)
            |> String.trim
            |> shallowYamlDecode
        (frontmatter, document)
    else (Map.empty, raw)

let private fromRawString (filename : string) (raw : string) : BlogPost =
    let (frontMatter, body) = parseDelimitedFile raw
    let title = Map.find "title" frontMatter
    let name = String.substring 11 filename

    let filenameCreatedAt : DateTimeOffset =
        match String.split '-' filename with
        | year :: month :: day :: _ -> DateTimeOffset(int year, int month, int day, 0, 0, 0, TimeSpan.Zero)
        | _ -> failwith "Unable to parse date"

    let createdAt : DateTimeOffset =
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

let loadPostsFromFolder (folder : string) : BlogPost list =
    folder
    |> System.IO.Directory.GetFiles
    |> Array.toList
    |> List.map (fun path ->
           let filename = System.IO.Path.GetFileNameWithoutExtension path
           path
           |> System.IO.File.ReadAllText
           |> fromRawString filename)

let private pageFromRaw (filename : string) (raw : string) : Page =
    let (frontMatter, body) = parseDelimitedFile raw
    let title = Map.find "title" frontMatter

    { path = filename
      title = title
      body = body }

let loadPagesFromFolder (folder : string) : Page list =
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

let findPostInList (posts : BlogPost list) (slug : Slug) : BlogPost option =
    posts |> safeFind (fun p -> p.slug.Equals(slug))

let findPageInList (pages : Page list) (path : string) : Page option =
    pages |> safeFind (fun p -> String.equals path p.path)