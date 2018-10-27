#r "paket: groupref fakebuild //"
#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
  #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif
#load "./script/tempFake5.fsx"

open ASeward.MiscTools
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators

let getFirstArgOrNull (parameter: TargetParameter) =
  parameter.Context.Arguments
  |> List.tryItem 0
  |> Option.defaultValue null

TempFake5.Versioning.createVersionTargets Target.create getFirstArgOrNull ["src/ASeward.MiscTools/AssemblyInfo.fs"]

Target.create FakeTargets.TargetNames.releaseNotesPrint (fun parameter ->
  TempFake5.ReleaseNotes.releaseNotesPrint
    (fun () -> "")                          // (FIXME) Get GitHub API token
    (fun () -> getFirstArgOrNull parameter) // Get PR nums (form: `1;2;3`)
    (fun () -> None)                        // (FIXME) Get base commit
    (fun () -> None)                        // (FIXME) Get head commit
    "awseward"
    "misctools"
)

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
