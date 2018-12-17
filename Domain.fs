module NoizwavesBlog.Domain

open System

let parseFromLongString (value : string) : DateTime option = Some <| System.DateTime.Parse value

type Slug =
    { year : int
      month : int
      day : int
      name : string }

let slugFromUrlParts (year : string) (month : string) (day : string) (name : string) : Slug option =
    let yearC = String.length year = 4
    let monthC = String.length month = 2
    let dayC = String.length day = 2
    match (yearC && monthC && dayC) with
    | true -> 
        Some { year = int year
               month = int month
               day = int day
               name = name }
    | false -> None

type BlogPost =
    { slug : Slug
      title : string
      createdAt : DateTime
      body : string }

type FetchPosts = unit -> BlogPost list

type FetchPost = Slug -> BlogPost option