#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  |> Shell.cleanDirs
)

Target.create "Build:Release" (fun _ ->
  !! "src/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)

let paketOutputDir = "dist"

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
        ApiKey = Environment.environVar "BUGSNAG_NET_NUGET_API_KEY"
        WorkingDir = paketOutputDir
    }
)

Target.create "All" ignore

"Clean"
  ==> "Build:Release"
  ==> "Paket:Pack"
  ==> "Paket:Push"
  ==> "All"

Target.runOrDefault "All"
