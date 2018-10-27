namespace ASeward.MiscTools

open System

module FakeTargets =
  let internal printCompareUrl org repo baseOpt headOpt =
    baseOpt
    |> Option.iter (fun baseCommit ->
        let headCommit = headOpt |> Option.defaultValue "master"
        printfn "https://github.com/%s/%s/compare/%s...%s" org repo baseCommit headCommit
    )

  module TargetNames =
    let releaseNotesPrint = "releaseNotes:print"

  module Fake4 =
    open ASeward.MiscTools.ActivePatterns

    let releaseNotesPrint getBuildParamOrDefault owner repo =
      let getBuildParam name = getBuildParamOrDefault name ""
      let tryGetBuildParam = Option.ofString << getBuildParam
      let token = Environment.GetEnvironmentVariable "GITHUB_TOKEN"

      printfn ""

      "head"
      |> tryGetBuildParam
      |> printCompareUrl owner repo (tryGetBuildParam "base")

      "prs"
      |> getBuildParam
      |> function
          | NullOrWhiteSpace -> failwith "Missing required param 'prs'"
          | str -> str.Split ';'
      |> Array.map int
      |> Array.toList
      |> ReleaseNotes.doTheThingAsync token owner repo
      |> Async.RunSynchronously
      |> printfn "%s"

      printfn ""

    module private Versioning =
      open ActivePatterns
      open Versioning

      let private _write filePath semVer =
        filePath |> AssemblyInfo.updateFile (AssemblyInfo.replaceAll semVer)

      let private _iterMap asmInfoPaths fn  =
        asmInfoPaths
        |> List.iter (fun filePath ->
            filePath
            |> AssemblyInfo.tryReadInfoVersion
            |> Option.map fn
            |> Option.iter (_write filePath)
        )

      let private _promptFor paramName =
        paramName
        |> sprintf "No value provided for parameter '%s'. Please specify (default: ''): "
        |> Console.Write
        |> Console.ReadLine
        |> fun str -> str.Trim ()

      let private _withParamOrPrompt (getBuildParam: string -> string) (fn: string -> SemanticVersion -> SemanticVersion) paramName =
        paramName
        |> getBuildParam
        |> function
            | NullOrWhiteSpace -> _promptFor paramName
            | str -> str
        |> fun pre -> fn pre

      let createVersionTargets (create: string -> (unit -> unit) -> unit) (getBuildParam: string -> string) (asmInfPaths: string list) =
        let apply = _iterMap asmInfPaths

        create "version:major" <| fun _ -> apply SemVer.incrMajor
        create "version:minor" <| fun _ -> apply SemVer.incrMinor
        create "version:patch" <| fun _ -> apply SemVer.incrPatch

        create "version:pre"   <| fun _ -> apply (_withParamOrPrompt getBuildParam SemVer.setPre "pre")
        create "version:meta"  <| fun _ -> apply (_withParamOrPrompt getBuildParam SemVer.setMeta "meta")

        create "version:current" (fun _ ->
          let tryRead filePath =
            match AssemblyInfo.tryReadInfoVersion filePath with
            | Some v -> Some (filePath, SemVer.toString v)
            | _ -> None

          asmInfPaths
          |> List.choose (fun filePath ->
              match AssemblyInfo.tryReadInfoVersion filePath with
              | Some v -> Some (filePath, SemVer.toString v)
              | _ -> None)
          |> List.iter (printfn "%A")
        )

    let createVersionTargets = Versioning.createVersionTargets

  module Fake5 =
    ()
