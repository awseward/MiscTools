#r "paket: groupref fakebuild //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
  #r "netstandard"
  #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open ASeward.MiscTools
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

// FakeTargets.Fake4.createVersionTargets Target getBuildParam ["src/ASeward.MiscTools/AssemblyInfo.fs"]

// Target FakeTargets.TargetNames.releaseNotesPrint <| fun _ ->
//   FakeTargets.Fake4.releaseNotesPrint
//     getBuildParamOrDefault
//     "awseward"
//     "misctools"

let projects = !! "**/*.fsproj"

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  |> Shell.cleanDirs
)

Target.create "Build:Release" (fun _ ->
  projects
  |> MSBuild.runRelease id null "Clean;Rebuild"
  |> Trace.logItems "AppBuild-Output: "
)

let paketOutputDir = ".dist"

Target.create "Paket:Pack" (fun _ ->
  Shell.cleanDir paketOutputDir

  Paket.pack <| fun p ->
    { p with
        OutputPath = paketOutputDir
    }
)

Target.create "Paket:Push" (fun _ ->
  Paket.push <| fun p ->
    { p with
        ApiKey = Environment.environVar "NUGET_API_KEY"
        WorkingDir = paketOutputDir
    }
)

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

Target.runOrDefaultWithArguments "Build:Release"
