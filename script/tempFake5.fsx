#load "./auxPreamble.fsx"

namespace TempFake5

open System
open ASeward.MiscTools
open ASeward.MiscTools.ActivePatterns

type TargetCreator<'param> = (string -> ('param -> unit) -> unit)

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

  let createVersionTargets<'param> (createTarget: TargetCreator<'param>) (getSingleParamValue: 'param -> string) (prefixOpt: string option) (asmInfPaths: string list) =
    let apply fn =
      asmInfPaths
      |> List.iter (fun filePath ->
          filePath
          |> AssemblyInfo.tryReadInfoVersion
          |> Option.map fn
          |> Option.iter (_write filePath)
      )
    let pref'd =
      prefixOpt
      |> Option.defaultValue "version"
      |> sprintf "%s:%s"

    createTarget (pref'd "major") <| fun _ -> apply SemVer.incrMajor
    createTarget (pref'd "minor") <| fun _ -> apply SemVer.incrMinor
    createTarget (pref'd "patch") <| fun _ -> apply SemVer.incrPatch

    createTarget (pref'd "pre") (fun targetParam ->
      targetParam
      |> getSingleParamValue
      |> function
          | NullOrWhiteSpace -> _promptFor "pre"
          | str -> str
      |> fun pre -> apply (SemVer.setPre pre)
    )

    createTarget (pref'd "meta") (fun targetParam ->
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
