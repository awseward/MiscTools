#r "./packages/fakebuild/FAKE/tools/FakeLib.dll"
#r "./packages/fakebuild/ASeward.MiscTools/lib/netstandard2.0/ASeward.MiscTools.dll"
#load "./temp/shims.fsx"

open ASeward.MiscTools
open ASeward.MiscTools.Shims
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators

Versioning.FakeTargetStubs.createVersionTargets Target Environment.environVar ["src/ASeward.MiscTools/AssemblyInfo.fs"]

Target FakeTargets.TargetNames.releaseNotesPrint <| fun _ ->
  FakeTargets.Fake4.releaseNotesPrint
    getBuildParamOrDefault
    "awseward"
    "misctools"

let projects = !! "**/*.fsproj"

Target "Build:Release" (fun _ ->
  projects
  |> MSBuild.runRelease id null "Clean;Rebuild"
  |> Trace.logItems "AppBuild-Output: "
)

let paketOutputDir = ".dist"

Target "Paket:Pack" (fun _ ->
  Shell.cleanDir paketOutputDir

  Paket.pack <| fun p ->
    { p with
        OutputPath = paketOutputDir
    }
)

Target "Paket:Push" (fun _ ->
  Paket.push <| fun p ->
    { p with
        ApiKey = Environment.environVar "BUGSNAG_NET_NUGET_API_KEY"
        WorkingDir = paketOutputDir
    }
)

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

RunTargetOrDefault "Build:Release"
