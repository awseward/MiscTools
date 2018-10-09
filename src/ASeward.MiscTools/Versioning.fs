namespace ASeward.MiscTools

open System
open System.Text.RegularExpressions

module Int32 =
  let ofRegexGroup (group: Group) = Int32.Parse group.Value
  let ofNamedCapture (name: string) (m: Match) = ofRegexGroup m.Groups.[name]

module Regex =
  let tryMatch (regex: Regex) input =
    let m = regex.Match input
    if not m.Success then
      None
    else
      Some m

module Versioning =

  type SemanticVersion =
    { major : int
      minor : int
      patch : int
      pre   : string option
      meta  : string option }

  module SemVer =
    let incrMajor ({ major = major } as semVer) =
      { semVer with
          major = major + 1
          minor = 0
          patch = 0 }

    let incrMinor ({ minor = minor } as semVer) =
      { semVer with
          minor = minor + 1
          patch = 0 }

    let incrPatch ({ patch = patch } as semVer) =
      { semVer with
          patch = patch + 1 }

    let setPre pre semVer =
      { semVer with
          pre = Option.ofString pre }
    let mapPre f ({ pre = pre } as semVer) =
      { semVer with
          pre = Option.map f pre }

    let setMeta meta semVer =
      { semVer with
          meta = Option.ofString meta }
    let mapMeta f ({ meta = meta } as semVer) =
      { semVer with
          meta = Option.map f meta }

    let private _regex = Regex @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z-]+))?(?:\+(?<metadata>[0-9A-Za-z-]+))?"
    // capture boundary hints:    |___________|  |___________|  |___________|    |__________________________|       |________________________|

    let tryParse =
      (Regex.tryMatch _regex)
      >> Option.map (fun mtch ->
          { major = mtch |> Int32.ofNamedCapture  "major"
            minor = mtch |> Int32.ofNamedCapture  "minor"
            patch = mtch |> Int32.ofNamedCapture  "patch"
            pre   = mtch |> Option.ofNamedCapture "prerelease"
            meta  = mtch |> Option.ofNamedCapture "metadata" }
      )

    let toString ({ major = major; minor = minor; patch = patch } as semVer) =
      sprintf "%i.%i.%i" major minor patch
      |> fun str ->
          match semVer.pre with
          | Some pre -> sprintf "%s-%s" str pre
          | None -> str
      |> fun str ->
          match semVer.meta with
          | Some meta -> sprintf "%s+%s" str meta
          | None -> str

    let toSystemVersion { major = major; minor = minor; patch = patch } =
      System.Version (
        major    = major,
        minor    = minor,
        build    = patch,
        revision = 0
      )

  module AssemblyInfo =
    open System.IO

    let private _versionRegex     = Regex (@"AssemblyVersion\s*\(\s*""(?<attrVal>[^""]+)""\s*\)(?:\s*>)?\s*]\s*$", RegexOptions.Multiline)
    let private _fileVersionRegex = Regex (@"AssemblyFileVersion\s*\(\s*""(?<attrVal>[^""]+)""\s*\)(?:\s*>)?\s*]\s*$", RegexOptions.Multiline)
    let private _infoVersionRegex = Regex (@"AssemblyInformationalVersion\s*\(\s*""(?<attrVal>[^""]+)""\s*\)(?:\s*>)?\s*]\s*$", RegexOptions.Multiline)

    let tryParseInfoVersion =
      (Regex.tryMatch _infoVersionRegex)
      >> Option.bind (Option.ofNamedCapture "attrVal")
      >> Option.bind SemVer.tryParse

    let tryReadInfoVersion = File.ReadAllText >> tryParseInfoVersion