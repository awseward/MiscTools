namespace ASeward.MiscTools

module Regex =
  open System
  open System.Text.RegularExpressions

  let tryMatch (regex: Regex) input =
    input
    |> regex.Match
    |> Option.someIf (fun m -> m.Success)

  let groupToInt (group: Group) = Int32.Parse group.Value
  let namedCaptureToInt (name: string) (m: Match) = groupToInt m.Groups.[name]

  let groupToOption (group: Group) =
    if group.Success then
      Some group.Value
    else
      None
  let namedCaptureToOption (name: string) (m: Match) = groupToOption m.Groups.[name]