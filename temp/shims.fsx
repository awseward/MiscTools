#nowarn "44" // Silence obsolete warnings

#r "../packages/fakebuild/FAKE/tools/FakeLib.dll"

namespace ASeward.MiscTools

module Shims =
  open Fake
  let Target = Target
  let RunTargetOrDefault = RunTargetOrDefault
  let getBuildParamOrDefault = getBuildParamOrDefault
  let getBuildParam = getBuildParam
  let (<==) = (<==)
