#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/ASeward.MiscTools/lib/net471/ASeward.MiscTools.dll"

open ASeward.MiscTools.Versioning
open ASeward.MiscTools.ActivePatterns
open Fake
open System
open System.IO

let projects = !! "/**/*.fsproj"
let assemblyInfos = ["src/ASeward.MiscTools/AssemblyInfo.fs"]

module AsmInf = ASeward.MiscTools.Versioning.AssemblyInfo
module AsmInfSemVer =
  let private _writeAllText filePath contents = File.WriteAllText (filePath, contents)
  let private _write filePath semVer =
    filePath
    |> File.ReadAllText
    |> AsmInf.replaceAssemblyVersion semVer
    |> AsmInf.replaceAssemblyFileVersion semVer
    |> AsmInf.replaceAssemblyInformationalVersion semVer
    |> _writeAllText filePath
  let iterMap fn =
    assemblyInfos
    |> List.iter (fun filePath ->
        filePath
        |> AsmInf.tryReadInfoVersion
        |> Option.map fn
        |> Option.iter (_write filePath)
    )

let promptFor name =
  (sprintf "Did not provide value for parameter '%s'. Please specify (default: ''): " name)
  |> Console.Write
  |> Console.ReadLine
  |> fun str -> str.Trim ()

Target "version:major" (fun _ -> AsmInfSemVer.iterMap SemVer.incrMajor)
Target "version:minor" (fun _ -> AsmInfSemVer.iterMap SemVer.incrMinor)
Target "version:patch" (fun _ -> AsmInfSemVer.iterMap SemVer.incrPatch)
Target "version:pre" (fun _ ->
  let map =
    "pre"
    |> getBuildParam
    |> function
        | NullOrWhiteSpace -> promptFor "pre"
        | str -> str
    |> fun pre -> SemVer.setPre pre

  AsmInfSemVer.iterMap map
)
Target "version:meta" (fun _ ->
  let map =
    "meta"
    |> getBuildParam
    |> function
        | NullOrWhiteSpace -> promptFor "meta"
        | str -> str
    |> fun meta -> SemVer.setMeta meta

  AsmInfSemVer.iterMap map
)

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
