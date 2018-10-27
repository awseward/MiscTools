#r "paket: groupref fakebuild //"
#load "../.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
  #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

namespace TempFake5

open System
open ASeward.MiscTools
open ASeward.MiscTools.ActivePatterns

module Versioning =
  open ASeward.MiscTools.Versioning

  let private _write filePath semVer =
    filePath |> AssemblyInfo.updateFile (AssemblyInfo.replaceAll semVer)

  let private _promptFor paramName =
    paramName
    |> sprintf "No value provided for parameter '%s'. Please specify (default: ''): "
    |> Console.Write
    |> Console.ReadLine
    |> fun str -> str.Trim ()

  let private _withParamOrPrompt (getParamValue: string -> string) (fn: string -> SemanticVersion -> SemanticVersion) paramName =
    paramName
    |> getParamValue
    |> function
        | NullOrWhiteSpace -> _promptFor paramName
        | str -> str
    |> fn

  let createVersionTargets<'a> (create: string -> ('a -> unit) -> unit) (getSingleParamValue: 'a -> string) (asmInfPaths: string list) =
    let apply fn =
      asmInfPaths
      |> List.iter (fun filePath ->
          filePath
          |> AssemblyInfo.tryReadInfoVersion
          |> Option.map fn
          |> Option.iter (_write filePath)
      )

    create "version:major" <| fun _ -> apply SemVer.incrMajor
    create "version:minor" <| fun _ -> apply SemVer.incrMinor
    create "version:patch" <| fun _ -> apply SemVer.incrPatch

    create "version:pre" (fun targetParam ->
      targetParam
      |> getSingleParamValue
      |> function
          | NullOrWhiteSpace -> _promptFor "pre"
          | str -> str
      |> fun pre -> apply (SemVer.setPre pre)
    )

    create "version:meta" (fun targetParam ->
      targetParam
      |> getSingleParamValue
      |> function
          | NullOrWhiteSpace -> _promptFor "meta"
          | str -> str
      |> fun meta -> apply (SemVer.setMeta meta)
    )

module ReleaseNotes =
  let releaseNotesPrint (getGithubToken: unit -> string) (getPrNumsParamValue: unit -> string) (getBaseCommit: unit -> string option) (getHeadCommit: unit -> string option) owner repo =
    let token =
      match getGithubToken () with
      | NullOrWhiteSpace -> failwith "Missing required github token."
      | str -> str

    printfn ""

    match getPrNumsParamValue () with
    | NullOrWhiteSpace -> failwith "Missing required param 'prs'"
    | str -> str.Split ';'
    |> Array.map int
    |> Array.toList
    |> ReleaseNotes.doTheThingAsync token owner repo
    |> Async.RunSynchronously
    |> printfn "%s"

    printfn ""
