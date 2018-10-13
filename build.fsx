#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/ASeward.MiscTools/lib/net471/ASeward.MiscTools.dll"

open ASeward.MiscTools
open Fake
open System

Versioning.FakeTargetStubs.createVersionTargets Target getBuildParam ["src/ASeward.MiscTools/AssemblyInfo.fs"]

let ghToken = Environment.GetEnvironmentVariable "GITHUB_TOKEN"

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

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

RunTargetOrDefault "Build:Release"
