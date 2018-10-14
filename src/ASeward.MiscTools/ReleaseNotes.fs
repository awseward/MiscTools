namespace ASeward.MiscTools

open Newtonsoft.Json
open System
open System.Net.Http
open System.Text.RegularExpressions
open System.Linq
open System.Diagnostics
open System.Net.Http

module private Util =
  type private ___ = interface end
  let private _t = typeof<___>
  let projectNamespace = _t.Namespace
  let projectVersionString =
    FileVersionInfo
      .GetVersionInfo(_t.Assembly.Location)
      .ProductVersion
  let userAgentString = sprintf "%s/%s" projectNamespace projectVersionString
  let client =
    let c = new HttpClient ()
    c.DefaultRequestHeaders.Add ("User-Agent", userAgentString)
    c

module ReleaseNotes =

  module GitHub =
    type PullRequest =
      { title:    string
        html_url: string }

    let private _getUriBuilder () =
      UriBuilder (
        scheme = "https",
        port = 443,
        host = "api.github.com",
        pathValue = "/"
      )
    let private _getUrlForPath path =
      let builder = _getUriBuilder ()
      builder.Path <- path
      builder.Uri

    let private _getPullRequestPath org repo prNum =
      sprintf "/repos/%s/%s/pulls/%i" org repo prNum

    let private _buildGetRequest token (uri: Uri) =
      let request = new HttpRequestMessage (HttpMethod.Get, uri)

      token
      |> sprintf "token %s"
      |> fun headerValue -> request.Headers.Add ("Authorization", headerValue)

      request

    let private _sendRequestAsync request =
      request
      |> Util.client.SendAsync
      |> Async.AwaitTask

    let private _getResponseBodyAsync (response: HttpResponseMessage) =
      Async.AwaitTask <| response.Content.ReadAsStringAsync ()

    let getPullRequestAsync token org repo prNum =
      async {
        let! response =
          prNum
          |> _getPullRequestPath org repo
          |> _getUrlForPath
          |> _buildGetRequest token
          |> _sendRequestAsync
        let! responseBody = _getResponseBodyAsync response

        return JsonConvert.DeserializeObject<PullRequest> responseBody
      }

    let tryGetPullRequestAsync token org repo prNum =
      async {
        try
          let! pullRequest = getPullRequestAsync token org repo prNum

          return (Some pullRequest)
        with
        | ex -> eprintf "ERROR: %A" ex; return None
      }

  module SemanticCommit =
    type SemMsg =
      { prefix: string option
        message: string }
    type SemPr =
      { title: SemMsg
        htmlUri: Uri }

    let parseSemMsg (str: string) : SemMsg =
      let m = Regex.Match(str, "(^[^\ ]*):\ *(.*)$")
      if m.Success
      then
        { prefix = Some <| m.Groups.[1].Captures.[0].Value.Trim()
          message = m.Groups.[2].Captures.[0].Value.Trim() }
      else
        { prefix = None; message = str.Trim() }

  module private TableRendering =
    let toColumns<'a> (getCells: ('a -> string) seq) (separator: string) (items: 'a seq) =
      let cellValues =
        items
        |> Seq.map (fun item ->
            getCells
            |> Seq.map (fun fn ->
                let cellVal = fn item

                (cellVal.Length, cellVal)
            )
        )

      let colWidths =
        [0..getCells.Count() - 1]
        |> Seq.map (fun columnIndex ->
            cellValues
            |> Seq.map (Seq.item columnIndex >> fst)
            |> Seq.max
        )

      cellValues
      |> Seq.map (fun cellVal ->
          cellVal
          |> Seq.mapi (fun columnIndex thing ->
              let colWidth = colWidths |> Seq.item columnIndex

              sprintf "%-*s" colWidth (snd thing)
          )
          |> String.concat separator
      )

  let private _toSemPr (pullRequest: GitHub.PullRequest) : SemanticCommit.SemPr =
    { title = (SemanticCommit.parseSemMsg pullRequest.title)
      htmlUri = (Uri pullRequest.html_url) }

  let doTheThingAsync token org repo prNums =
    async {
      let! prOpts =
        prNums
        |> List.map (GitHub.tryGetPullRequestAsync token org repo)
        |> Async.Parallel

      return
        prOpts
        |> Array.filter Option.isSome
        |> Array.map Option.get // Looks bad but is safe w/ the filter above
        |> Array.map _toSemPr
        |> TableRendering.toColumns
           [
             (fun pull ->
               match pull.title.prefix with
               | Some prefix -> sprintf "%s:" prefix
               | _  -> ""
             )
             (fun pull -> pull.title.message)
             (fun pull -> pull.htmlUri.ToString())
           ]
           " "
        |> (List.ofSeq >> List.sort)
        |> List.map (sprintf "* %s")
        |> String.concat Environment.NewLine
    }

  module FakeTargetStubs =
    open ASeward.MiscTools.ActivePatterns

    let targetName = "releaseNotes:print"
    let printReleaseNotes (getBuildParamOrDefault: string -> string -> string) org repo =
      let getBuildParam name = getBuildParamOrDefault name ""
      let token = Environment.GetEnvironmentVariable "GITHUB_TOKEN"

      printfn ""

      match getBuildParam "base" with
      | NullOrWhiteSpace -> ()
      | baseCommit ->
          let headCommit = getBuildParamOrDefault "head" "master"
          printfn "https://github.com/%s/%s/compare/%s...%s" org repo baseCommit headCommit
          printfn ""

      "prs"
      |> getBuildParam
      |> fun str -> str.Split ';'
      |> Array.map int
      |> Array.toList
      |> doTheThingAsync token org repo
      |> Async.RunSynchronously
      |> printfn "%s"

      printfn ""