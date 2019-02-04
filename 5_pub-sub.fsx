#load "zmqprelude.fsx"
#load "String.fs"

open fszmq
open fszmq.Socket

let ctx = new Context ()
let pub = Context.pub ctx

Socket.bind pub "tcp://*:5555"

let sub = Context.sub ctx
Socket.connect sub "tcp://localhost:5555"

let messages = 
    [ ("zmq"B, "hello zmq meetup!"B)
      ("fsharp"B, "hello fsharp meetup!"B)
      ("pizza"B, "pizza has arrived!"B) ]

let publish socket (topic, message) = socket <~| topic <<| message

let sread socket = 
    let topic = Socket.recv socket |> String.decode
    let m = Socket.recv socket |> String.decode
    sprintf "%s : %s" topic m

////////
messages |> List.iter (publish pub)

Socket.subscribe sub [ "zmq"B;"fsharp"B ]
sread sub
        
Socket.subscribe sub [ "pizza"B ]
