namespace ASeward.MiscTools

open System.Text.RegularExpressions

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
          { major = mtch |> Regex.namedCaptureToInt    "major"
            minor = mtch |> Regex.namedCaptureToInt    "minor"
            patch = mtch |> Regex.namedCaptureToInt    "patch"
            pre   = mtch |> Regex.namedCaptureToOption "prerelease"
            meta  = mtch |> Regex.namedCaptureToOption "metadata" }
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

    let private _buildAttrRegex (name: string) : Regex =
      name
      |> sprintf @"^(?!//)(?<prefix>\s*\[(?:\s*<)?\s*assembly\s*:\s*%s\s*\(\s*"")(?<attrVal>[^""]+)(?<postfix>""\s*\)(?:\s*>)?\s*]\s*)"
      |> fun pattern -> (pattern, RegexOptions.Multiline)
      |> Regex
    let private _versionRegex     = _buildAttrRegex "AssemblyVersion"
    let private _fileVersionRegex = _buildAttrRegex "AssemblyFileVersion"
    let private _infoVersionRegex = _buildAttrRegex "AssemblyInformationalVersion"

    let private _toString a = a.ToString()
    let private _toSystemVersionString = SemVer.toSystemVersion >> _toString

    let private _replaceAttrValue (regex: Regex) semVerToString (semVer: SemanticVersion) content =
      semVer
      |> semVerToString
      |> sprintf "${prefix}%s${postfix}"
      |> fun replacement -> regex.Replace (input = content, replacement = replacement)

    let tryParseInfoVersion =
      (Regex.tryMatch _infoVersionRegex)
      >> Option.bind (Regex.namedCaptureToOption "attrVal")
      >> Option.bind SemVer.tryParse

    let tryReadInfoVersion = File.ReadAllText >> tryParseInfoVersion

    let replaceAssemblyVersion =
      _replaceAttrValue
        _versionRegex
        (_toSystemVersionString)

    let replaceAssemblyFileVersion =
      _replaceAttrValue
        _fileVersionRegex
        (_toSystemVersionString)

    let replaceAssemblyInformationalVersion =
      _replaceAttrValue
        _infoVersionRegex
        SemVer.toString

    let replaceAll semVer =
      (replaceAssemblyVersion semVer)
      >> (replaceAssemblyFileVersion semVer)
      >> (replaceAssemblyInformationalVersion semVer)

    let updateFile fn filePath =
      filePath
      |> File.ReadAllText
      |> fn
      |> fun contents -> File.WriteAllText (filePath, contents)
