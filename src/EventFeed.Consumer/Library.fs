namespace EventFeed.Consumer

open System
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json
open System.Threading


type Link = {
    href : string
    templated : bool
    rel : string option
}
type Link with
    static member Render(value: obj, link: Link) =
        if link.templated then
            let mutable replacing = false
            let rendered = 
                [|
                    for c in link.href do
                        if c = '{' then 
                            do replacing <- true
                            yield! value.ToString()
                        elif c = '}' then
                           do replacing <- false
                        elif replacing then
                           ignore()
                        else yield c
                |]  
            String(rendered)
        else link.href

type MetaLinks = {
    self : Link
    head : Link
    tail : Link
    page : Link
}

type Meta = {
    _links : MetaLinks
    eventCount : int64
    eventsPerPage : int
    pageCount : int
}
type Meta with
    static member Deserialize(json: string) =
        JsonSerializer.Deserialize<Meta>(json)

type PageLinks = {
    meta : Link
    head : Link
    previous : Link option
    self : Link
    next : Link option
    tail : Link
    page : Link
}

type FeedEvent = {
    name : string
}

type Page = {
    _links : PageLinks
    pageNumber : int
    isComplete : bool
    events : FeedEvent array
}
type Page with
    static member Deserialize(json: string) =
        JsonSerializer.Deserialize<Page>(json)

type GetAction = string -> HttpResponseMessage

// what about a state machine?
// Page
// LastPage
// SamePage
// MetaPage
// Error
type EventFeedClient =
    abstract member Meta : unit -> Task<Meta>
    abstract member Head : unit -> Task<Page>
    abstract member Next : unit -> Task<Page>
    abstract member Tail : unit -> Task<Page>
    abstract member Page : int -> Task<Page>

type EventFeedHttpClient(baseUri : Uri, httpClientFactory : IHttpClientFactory) =
    let mutable headUrl = None
    let mutable pageUrl = None
    let mutable nextUrl = None
    let mutable tailUrl = None

    let httpClient() =
        let httpClient = httpClientFactory.CreateClient()
        do httpClient.BaseAddress <- baseUri
        httpClient

    let getPageF (httpClient : HttpClient) (url : string) parseResponse = 
        task {
            try 
                let! pageReponse = httpClient.GetAsync(url)
                if pageReponse.IsSuccessStatusCode then
                    let! pageJson = pageReponse.Content.ReadAsStringAsync()
                    let page = parseResponse(pageJson)
                    return Ok(page)
                else return Error $"Fetching {url} failed. {pageReponse.StatusCode} {pageReponse.ReasonPhrase}"
            with 
            | ex -> return Error ex.Message
        }

    let getMeta (httpClient : HttpClient) (url : string option) =
        let url = Option.defaultValue "/" url
        let handleResponse json =
            let meta = Meta.Deserialize json
            headUrl <- Some meta._links.head.href
            pageUrl <- Some meta._links.page.href
            meta

        getPageF httpClient url handleResponse

    let getPage (httpClient : HttpClient) (url : string) = 
        getPageF httpClient url Page.Deserialize

    interface EventFeedClient with

        member this.Meta() = 
            task {
                let! result = getMeta (httpClient()) (Some(baseUri.AbsoluteUri)) 
                let meta = 
                    match result with
                    | Ok y -> y
                    | Error err -> failwith err
                return meta
            }

        member this.Page i =
            let httpClient = httpClient()
            task {
                if pageUrl.IsNone then
                    let _ = getMeta httpClient (Some(baseUri.AbsoluteUri))
                    if pageUrl.IsNone then 
                        failwith "`page` is required in _links."
                    
                let! result  = getPage httpClient pageUrl.Value
                let page = 
                    match result with
                    | Ok y -> y
                    | Error err -> failwith err

                return page
            }

        member this.Head() =
            let httpClient = httpClient()
            task {
                if headUrl.IsNone then
                    let _ = getMeta httpClient (Some(baseUri.AbsoluteUri))
                    if headUrl.IsNone then 
                        failwith "`page` is required in _links."
                    
                let! result  = getPage httpClient headUrl.Value
                let page = 
                    match result with
                    | Ok y -> y
                    | Error err -> failwith err

                return page
            }

        member this.Next() =
            let httpClient = httpClient()
            task {
                if nextUrl.IsNone then
                    let _ = getMeta httpClient (Some(baseUri.AbsoluteUri))
                    if nextUrl.IsNone then 
                        failwith "`page` is required in _links."
                    
                let! result  = getPage httpClient nextUrl.Value
                let page = 
                    match result with
                    | Ok y -> y
                    | Error err -> failwith err

                return page
            }
        member this.Tail() =
            let httpClient = httpClient()
            task {
                if tailUrl.IsNone then
                    let _ = getMeta httpClient (Some(baseUri.AbsoluteUri))
                    if tailUrl.IsNone then 
                        failwith "`page` is required in _links."
                    
                let! result  = getPage httpClient tailUrl.Value
                let page = 
                    match result with
                    | Ok y -> y
                    | Error err -> failwith err

                return page
            }

            

type EventFeedConsumer(client : EventFeedClient) =
    member this.PrintMessage() =
        printf "Creating MyClass2 with Data %s" "data"


// https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/generate-consume-asynchronous-stream
// https://github.com/event-streams-dotnet/event-stream-processing
// https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-6.0
// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/async-streams
// https://techinplanet.com/how-to-batch-an-iasyncenumerable-enforcing-a-maximum-interval-policy-between-consecutive-batches/
// http://fsprojects.github.io/FSharp.Control.AsyncSeq/
// https://adamstorr.azurewebsites.net/blog/test-your-dotnet-httpclient-based-strongly-typed-clients-like-a-boss