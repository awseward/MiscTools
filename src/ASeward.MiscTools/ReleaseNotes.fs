namespace ASeward.MiscTools

open Hopac
open HttpFs.Client
open Newtonsoft.Json
open System
open System.Text.RegularExpressions
open System.Linq
open System.Diagnostics

module private Util =
  type private ___ = interface end
  let private _t = typeof<___>
  let projectNamespace = _t.Namespace
  let projectVersionString =
    FileVersionInfo
      .GetVersionInfo(_t.Assembly.Location)
      .ProductVersion
  let userAgentString = sprintf "%s/%s" projectNamespace projectVersionString

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

    let private _setGitHubRequestHeaders token request =
      request
      |> Request.setHeader (Authorization (sprintf "token %s" token))
      |> Request.setHeader (UserAgent Util.userAgentString)

    let private _getResponseBody setHeaders (uri: Uri) =
      uri
      |> fun u -> u.ToString ()
      |> Request.createUrl Get
      |> setHeaders
      |> Request.responseAsString
      |> run

    let getPullRequest token org repo prNum =
      prNum
      |> _getPullRequestPath org repo
      |> _getUrlForPath
      |> _getResponseBody (_setGitHubRequestHeaders token)
      |> JsonConvert.DeserializeObject<PullRequest>

    let tryGetPullRequest token org repo prNum =
      try
        Some <| getPullRequest token org repo prNum
      with
      | ex -> eprintf "ERROR: %A" ex; None

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
  let doTheThing token org repo prNums =
    prNums
    |> List.choose (GitHub.tryGetPullRequest token org repo)
    |> List.map _toSemPr
    |> TableRendering.toColumns
        [
          (fun pull ->
            match pull.title.prefix with
            | Some pfx -> sprintf "%s:" pfx
            | _        -> ""
          )
          (fun pull -> pull.title.message)
          (fun pull -> pull.htmlUri.ToString())
        ]
        " "
    |> (List.ofSeq >> List.sort)
    |> List.map (sprintf "* %s")
    |> String.concat Environment.NewLine
