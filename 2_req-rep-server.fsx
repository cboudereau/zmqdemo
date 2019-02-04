#load "zmqprelude.fsx"

open fszmq

let rqCtx = new Context ()
let rpCtx = new Context ()

let rqSocket = Context.req rqCtx 
Socket.connect rqSocket "tcp://localhost:5555"

let rpSocket = Context.rep rpCtx
Socket.bind rpSocket "tcp://*:5555"

let decode = System.Text.UTF8Encoding.UTF8.GetString
let encode s = System.Text.UTF8Encoding.UTF8.GetBytes(s:string)

// Try in this order and then in a different one
Socket.send rqSocket "hello"B

Socket.recv rpSocket |> decode |> printfn "%s"

encode "received" |> Socket.send rpSocket 

Socket.recv rqSocket |> decode

