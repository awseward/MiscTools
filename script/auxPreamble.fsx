#if !FAKE
  #r "paket: groupref fakebuild //"
  #load "../.fake/build.fsx/intellisense.fsx"
  #r "netstandard"
  #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif
