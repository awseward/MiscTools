namespace ASeward.MiscTools

open System
open System.Text.RegularExpressions

module Option =
  let someIf predicate a = if predicate a then (Some a) else None

  let ofString = someIf (not << String.IsNullOrWhiteSpace)