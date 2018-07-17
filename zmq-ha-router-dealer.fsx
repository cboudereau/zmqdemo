#load "zmqprelude.fsx"

open fszmq
open fszmq.Context
open fszmq.Socket
open System

let decode x = System.Text.Encoding.UTF8.GetString(x)
let encode x = System.Text.Encoding.UTF8.GetBytes(x:string)

let srecv = recv >> decode

type [<Struct>] Identity = Identity of string
type [<Struct>] Port = Port of int
type [<Struct>] Times = Times of int

type [<Measure>] s
type [<Measure>] ms

type [<Struct>] ThinkTime = ThinkTime of float<s>
let milliseconds = 1000.<ms/s>

let client (ThinkTime thinktime) (Times times) (Identity dest) ports msg = 
    async {
        use context = new Context ()
        use channel = dealer context
        (ZMQ.IDENTITY, ("client")) |> setOption channel 

        ports |> List.iter (fun (Port port) -> sprintf "tcp://localhost:%i" port |> connect channel)
        do!
            [ 1 .. times ]
            |> List.fold (fun s t -> 
                async {
                    do! s
                    if thinktime > 0.<s> then 
                        do printfn "sleeping %i" t
                        do! milliseconds * thinktime |> int |> Async.Sleep
                    do channel <~| encode dest <<| encode msg
                    printfn "client send %i" t }) (async.Return ())
    }

let server (Identity identity) ports = 
    async {
        use context = new Context()
        use channel  = dealer context

        (ZMQ.IDENTITY, (identity:string)) |> setOption channel 
        
        ports |> List.iter (fun (Port port) -> sprintf "tcp://localhost:%i" port |> connect channel)
        
        let rec handle () = 
            let msg = srecv channel
            printfn "dealer %s received : %s" identity msg
            handle ()
        handle () }

let router (Port port) = 
    let route channel = 
        let _ = Socket.recv channel
        let identity = Socket.recv channel
        let client = channel <~| identity
        use message = new Message ()
        Message.recv message channel

        while Message.hasMore message do
            client <~| Message.data message
            |> ignore
            Message.recv message channel
        printfn "routing message from router port %i" port
        client <<| Message.data message
    
    async {
        use context = new Context()
        use channel  = router context
        
        sprintf "tcp://*:%i" port |> bind channel
        
        let rec handle () = 
            route channel
            handle ()
        
        handle () }

let idt = Identity "pacman"

Port 6666 |> router |> Async.Start
Port 6667 |> router |> Async.Start

server idt [Port 6666; Port 6667] |> Async.Start

let send ports = DateTime.UtcNow |> sprintf "%O hello" |> client (ThinkTime 3.<s>) (Times 20) idt ports

send [Port 6666; Port 6667] |> Async.Start