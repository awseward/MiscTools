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
    let incrMajor semVer =
      { semVer with
          major = semVer.major + 1
          minor = 0
          patch = 0 }
    let incrMinor semVer =
      { semVer with
          minor = semVer.minor + 1
          patch = 0 }
    let incrPatch semVer =
      { semVer with patch = semVer.patch + 1 }
    let setPre pre semVer =
      { semVer with pre = Option.ofString pre }
    let setMeta meta semVer =
      { semVer with meta = Option.ofString meta }

    let private _regex = Regex @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z-]+))?(?:\+(?<metadata>[0-9A-Za-z-]+))?"
    // capture boundary hints:    |___________|  |___________|  |___________|    |__________________________|       |________________________|

    let tryParse =
      (Regex.tryMatch _regex)
      >> Option.map (fun mtch ->
          { major = mtch |> Int32.ofNamedCapture  "major"
            minor = mtch |> Int32.ofNamedCapture  "minor"
            patch = mtch |> Int32.ofNamedCapture  "patch"
            pre   = mtch |> Option.ofNamedCapture "prerelease"
            meta  = mtch |> Option.ofNamedCapture "meta" }
      )

    let toString semVer =
      let
        { major = major
          minor = minor
          patch = patch } = semVer
      sprintf "%i.%i.%i" major minor patch
      |> fun str ->
          match semVer.pre with
          | Some pre -> sprintf "%s-%s" str pre
          | None -> str
      |> fun str ->
          match semVer.meta with
          | Some meta -> sprintf "%s+%s" str meta
          | None -> str
