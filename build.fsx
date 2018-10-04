#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/FSharp.Version.Utils/lib/net45/FSharpVersionUtils.dll"
#r "./packages/FSharp.FakeTargets/lib/net45/FSharp.FakeTargets.dll"

open Fake

let projects = !! "/**/*.fsproj"

Target "Build:Release" (fun _ ->
  projects
  |> MSBuildRelease null "Clean;Rebuild"
  |> Log "AppBuild-Output: "
)

Target "Package:NuGetFail" (fun _ ->
  @"

  Packaging with NuGet does not work. Please prefer Paket:* FAKE targets, or use paket.

  Paket usage:
    .paket/paket.exe pack .
    .paket/paket.exe push --api-key <API_KEY> <NUPKG_FILE>

  "
  |> failwith
)

datNET.Targets.initialize (fun p ->
  { p with
      AccessKey             = environVar "BUGSNAG_NET_NUGET_API_KEY"
      AssemblyInfoFilePaths = ["src/ASeward.MiscTools/AssemblyInfo.fs"]
      Project               = "ASeward.MiscTools"
      ProjectFilePath       = Some "src/ASeward.MiscTools/ASeward.MiscTools.fsproj"
      OutputPath            = "."
      WorkingDir            = "."
  }
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

// Deprecated
"Package:NuGetFail"
  ==> "Package:Project"
  ==> "Publish"

RunTargetOrDefault "Build:Release"
