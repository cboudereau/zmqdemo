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

let loadbalancer (Port inPort) (Port outPort) = 
    async {
        use context = new Context ()
        use x = Context.router context
        use y = Context.dealer context
        
        inPort |> sprintf "tcp://*:%i" |> Socket.bind x
        outPort |> sprintf "tcp://*:%i" |> Socket.bind y

        fszmq.Proxying.proxy x y None
    }

let client (ThinkTime thinktime) (Times times) (Identity dest) (Port port) msg = 
    async {
        use context = new Context ()
        use channel = dealer context
        (ZMQ.IDENTITY, ("client")) |> setOption channel 

        sprintf "tcp://localhost:%i" port |> connect channel
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

let server (Identity identity) (Port port) = 
    async {
        use context = new Context()
        use channel  = dealer context

        (ZMQ.IDENTITY, (identity:string)) |> setOption channel 
        
        sprintf "tcp://localhost:%i" port |> connect channel
        
        let rec handle () = 
            let msg = srecv channel
            printfn "dealer %s:%i received : %s" identity port msg
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

        client <<| Message.data message
    
    async {
        use context = new Context()
        use channel  = router context
        
        sprintf "tcp://*:%i" port |> bind channel
        
        let rec handle () = 
            route channel
            handle ()
        
        handle () }

let frontendPort = Port 6666
let idt = Identity "pacman2"

router frontendPort |> Async.Start
server idt frontendPort |> Async.Start
DateTime.UtcNow  |> sprintf "%O hello" |> client (ThinkTime 3.<s>) (Times 20) idt frontendPort |> Async.Start