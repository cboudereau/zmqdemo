module PATH = 
  open System
  let private divider =
    match Environment.OSVersion.Platform with
    | PlatformID.Unix
    | PlatformID.MacOSX -> ":"
    | _                 -> ";"
  let private zmqFolder = __SOURCE_DIRECTORY__
  let private oldPATH = ref ""
  // temporarily add location of native libs to global environment
  let hijack () =
    oldPATH := Environment.GetEnvironmentVariable "PATH"
    let newPATH = sprintf "%s%s%s" !oldPATH divider zmqFolder
    Environment.SetEnvironmentVariable ("PATH",newPATH)
  // undo changes to global environment
  let release () =
    if not <| String.IsNullOrWhiteSpace !oldPATH then
      Environment.SetEnvironmentVariable ("PATH",!oldPATH)
      oldPATH := ""

PATH.hijack ()

#r @"fszmq.dll"
open fszmq
printfn "libzmq version: %A" ZMQ.version