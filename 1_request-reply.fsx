#load "zmqprelude.fsx"

open fszmq

let decode = System.Text.UTF8Encoding.UTF8.GetString
let encode s = System.Text.UTF8Encoding.UTF8.GetBytes(s:string)

let request m =
    let ctx = new Context ()

    let socket = Context.req ctx
    Socket.connect socket "tcp://localhost:5555"
    encode m |> Socket.send socket

let reply () = 
    let ctx = new Context ()
    let socket = Context.rep ctx
    Socket.bind socket "tcp://*:5555"
    Socket.recv socket |> decode

reply ()
request "hello"