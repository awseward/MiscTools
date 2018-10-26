#nowarn "44" // Silence obsolete warnings

#r "../packages/FAKE/tools/FakeLib.dll"

namespace ASeward.MiscTools

module Shims =
  open Fake
  let Target = Target
  let RunTargetOrDefault = RunTargetOrDefault
  let (<==) = (<==)
  let getBuildParamOrDefault = getBuildParamOrDefault
