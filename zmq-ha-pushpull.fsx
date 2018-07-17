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

let proxy (Port inPort) (Port outPort) = 
    async {
        use context = new Context ()
        use pullC = Context.pull context
        use pushC = Context.push context
        (ZMQ.IMMEDIATE, 1) |> Socket.setOption pushC
        
        inPort |> sprintf "tcp://*:%i" |> Socket.bind pullC 
        outPort |> sprintf "tcp://*:%i" |> Socket.bind pushC

        fszmq.Proxying.proxy pushC pullC None          
    }

let receive (Identity identity) ports = 
    use context = new Context ()
    use channel = pull context
    ports |> List.iter (fun (Port port) -> sprintf "tcp://localhost:%i" port |> connect channel)
    
    let rec handle () = 
        let msg = srecv channel
        printfn "%O - I%s: reader receive : %s" (DateTime.Now) identity msg
        handle ()
    handle ()

let send (ThinkTime thinktime) (Times times) ports = 
    async { 
        use context = new Context ()
        use channel = push context

        ports |> List.iter (fun (Port port) -> sprintf "tcp://localhost:%i" port |> connect channel)

        let send t = 
            async {
                sprintf "hello %i" t |> encode |> send channel
                printfn "send(%i) finished" t
            }
        do! 
            [1 .. times ] 
            |> List.fold (fun s t -> 
                async { 
                    do! s
                    if thinktime > 0.<s> then do! milliseconds * thinktime |> int |> Async.Sleep
                    do! send t }) (async.Return ()) } 

//Proxy nodes on server A and B as P component
proxy (Port 5571) (Port 5572) |> Async.Start
proxy (Port 5573) (Port 5574) |> Async.Start

//Receiver server C and D as R component (connected to P output)
async { receive (Identity "1") [Port 5572; Port 5574] } |> Async.Start
async { receive (Identity "2") [Port 5572; Port 5574] } |> Async.Start

//Sender server E as S component (connected to P input)
send (ThinkTime 5.<s>) (Times 20) [Port 5571;Port 5573] |> Async.Start