namespace ASeward.MiscTools

open System
open System.Text.RegularExpressions

module Option =
  let ofString str =
    if String.IsNullOrWhiteSpace str then None
    else Some str

  let ofRegexGroup (group: Group) =
    if group.Success then
      Some (group.Value)
    else
      None
  let ofNamedCapture (name: string) (m: Match) = ofRegexGroup m.Groups.[name]
