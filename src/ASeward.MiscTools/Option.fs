namespace ASeward.MiscTools

module Option =
  open System

  let someIf predicate a = if predicate a then (Some a) else None

  let ofString = someIf (not << String.IsNullOrWhiteSpace)
