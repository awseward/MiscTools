#r "paket: groupref fakebuild //"
#load "../.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
  #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif
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
