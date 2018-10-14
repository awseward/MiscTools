#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/ASeward.MiscTools/lib/net471/ASeward.MiscTools.dll"

open ASeward.MiscTools
open ASeward.MiscTools.ActivePatterns
open Fake
open System

// Here to enable newer TLS versions (GitHub has disallowed older versions)
open System.Net
ServicePointManager.SecurityProtocol <- ServicePointManager.SecurityProtocol ||| SecurityProtocolType.Tls ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls12

Versioning.FakeTargetStubs.createVersionTargets Target getBuildParam ["src/ASeward.MiscTools/AssemblyInfo.fs"]

let projects = !! "/**/*.fsproj"

Target "Build:Release" (fun _ ->
  projects
  |> MSBuildRelease null "Clean;Rebuild"
  |> Log "AppBuild-Output: "
)

let paketOutputDir = ".dist"

Target "Paket:Pack" (fun _ ->
  FileHelper.CleanDir paketOutputDir

  Paket.Pack <| fun p ->
    { p with
        OutputPath = paketOutputDir
    }
)

Target "Paket:Push" (fun _ ->
  Paket.Push <| fun p ->
    { p with
        ApiKey = environVar "BUGSNAG_NET_NUGET_API_KEY"
        WorkingDir = paketOutputDir
    }
)

Target "release:notes" (fun _ ->
  let org = "awseward"
  let repo = "misctools"
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
  |> ReleaseNotes.doTheThingAsync token org repo
  |> Async.RunSynchronously
  |> printfn "%s"

  printfn ""
)

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

RunTargetOrDefault "Build:Release"
