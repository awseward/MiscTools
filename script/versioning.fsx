#load "./auxPreamble.fsx"
#load "./tempFake5.fsx"

namespace ASeward.MiscTools.FakeBuild

open Fake.Core
open TempFake5
open TempFake5.Versioning

module Versioning =

  let getFirstArgOrNull (parameter: TargetParameter) =
    parameter.Context.Arguments
    |> List.tryItem 0
    |> Option.defaultValue null

  let createTargets = createVersionTargets Target.create getFirstArgOrNull (Some "v")
