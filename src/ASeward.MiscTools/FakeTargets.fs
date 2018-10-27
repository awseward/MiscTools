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

  module Fake5 =
    ()
