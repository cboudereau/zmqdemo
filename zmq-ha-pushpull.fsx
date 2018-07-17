#load "zmqprelude.fsx"

open fszmq
open fszmq.Context
open fszmq.Socket
open System

let decode x = System.Text.Encoding.UTF8.GetString(x)
let encode x = System.Text.Encoding.UTF8.GetBytes(x:string)

let srecv = recv >> decode

let pushPull inPort outPort = 
    async {
        use context = new Context ()
        use pullC = Context.pull context
        use pushC = Context.push context
        (ZMQ.IMMEDIATE, 1) |> Socket.setOption pushC
        
        inPort |> sprintf "tcp://*:%i" |> Socket.bind pullC 
        outPort |> sprintf "tcp://*:%i" |> Socket.bind pushC

        fszmq.Proxying.proxy pushC pullC None          
    }

let receive identity ports = 
    use context = new Context ()
    use channel = pull context
    ports |> List.iter (sprintf "tcp://localhost:%i" >> connect channel)
    
    let rec handle () = 
        let msg = srecv channel
        printfn "%O - I%i: reader receive : %s" (DateTime.Now) identity msg
        handle ()
    handle ()

let send thinktime times port = 
    async { 
        use context = new Context ()
        use channel = push context

        sprintf "tcp://localhost:%i" port |> connect channel

        let send t = 
            async {
                sprintf "hello from port %i" port |> encode |> send channel
                printfn "send(%i) %i finished" t port
            }
        do! 
            [1 .. times ] 
            |> List.fold (fun s t -> 
                async { 
                    do! s
                    if thinktime > 0 then do! Async.Sleep thinktime
                    do! send t }) (async.Return ()) } 


//Sandbox zone

//Proxy cluster
pushPull 5571 5572 |> Async.Start
pushPull 5573 5574 |> Async.Start

async { receive 1 [5572;5574] } |> Async.Start
async { receive 2 [5572;5574] } |> Async.Start

send 5000 20 5571 |> Async.Start
send 3000 20 5573 |> Async.Start