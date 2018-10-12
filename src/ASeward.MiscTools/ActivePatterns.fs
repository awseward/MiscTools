namespace ASeward.MiscTools

module ActivePatterns =
  open System

  let (|NullOrWhiteSpace|_|) input =
    if String.IsNullOrWhiteSpace input then Some ()
    else None
