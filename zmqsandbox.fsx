#load "zmqprelude.fsx"

open fszmq
open fszmq.Context
open fszmq.Socket

type Identity = Identity of string
type Address = Address of string
type ControlAddress = ControlAddress of string

let decode x = System.Text.Encoding.UTF8.GetString(x)
let encode x = System.Text.Encoding.UTF8.GetBytes(x:string)

let srecv = recv >> decode

let deleter () = ()

let reader (Identity identity, (ControlAddress caddr, Address addr)) = 
    use context = new Context ()
    use channel = dealer context
    use control = rep context
    (ZMQ.IDENTITY, identity) |> setOption channel

    addr |> bind channel
    caddr |> bind control

    srecv control |> printfn "reader : ping : %s" 
    control <<| "pong"B

    let rec handle () = 
        printfn "%ser : handle!" identity
        let clientId = recv channel
        let data = recv channel
        printfn "%ser : route" identity
        channel <~| "route"B <~| clientId <<| "read done from reader"B
        handle ()
    handle ()
   
let router services = 
    use context = new Context ()
    use channel = router context
    use control = req context
    
    (ZMQ.IDENTITY, "router") |> setOption channel

    "tcp://*:5570" |> bind channel
    
    let cmds = 
        services
        |> Seq.map(fun (Identity identity, (ControlAddress caddr, Address addr)) -> 
            caddr |> connect control
            addr |> connect channel
            control <<| "ping"B
            recv control |> decode |> printfn "router : pong %s"
            identity)
        |> Seq.toList


    let rec handle cmds = 
        printfn "router : handle!"
        let identityB = recv channel
        
        let command = srecv channel

        match command with
        | "route" ->
            printfn "router : route!"
            let identityB = recv channel
            printfn "router : route : identity : %s" (decode identityB)
            let data = recv channel
            printfn "router : route : data : %s" (decode data)
            channel <~| identityB <<| data
            printfn "router : route : forwarded"
        | deal when cmds |> List.exists ((=) command) ->
            printfn "deal : %s" deal
            Socket.recvAll channel |> Socket.sendAll (channel <~| encode deal <~| identityB)
        | other -> printfn "command not handled %s" other
        
        handle cmds
    
    handle cmds

let zipper () = 
    use context = new Context ()
    use channel = dealer context
    (ZMQ.IDENTITY, "zipper") |> setOption channel

    "tcp://localhost:5570" |> Socket.connect channel

    channel <~| "read"B <<| "hello"B
    printfn "zipper read response %s" (channel |> srecv)

    "delete"B |>> channel
    printfn "zipper delete response %s" (channel |> srecv)

let tcp port = sprintf "tcp://%s:%i" System.Environment.MachineName port

async { zipper () } |> Async.Start
async { router [(Identity "read", (ControlAddress (tcp 5572),Address (tcp 5571)))] } |> Async.Start
async { reader (Identity "read", (ControlAddress "tcp://*:5572",Address "tcp://*:5571")) } |> Async.Start
