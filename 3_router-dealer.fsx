#load "zmqprelude.fsx"
#load "String.fs"

open fszmq

let broker = 
    async {
        use ctx = new Context()
        use frontend = Context.router ctx
        use backend = Context.dealer ctx
        Socket.bind frontend "tcp://*:5559"
        Socket.bind backend "tcp://*:5560"
        Proxying.proxy frontend backend None
    }

let client m = 
    async {
        use ctx = new Context ()
        use req = Context.req ctx

        Socket.connect req "tcp://localhost:5559"
        
        printfn "client send %s" m
        String.encode m |> Socket.send req

        let r = Socket.recv req |> String.decode
        printfn "client received : %s" r
    }

let worker i = 
    async {
        use ctx = new Context ()
        use rep = Context.rep ctx

        Socket.connect rep "tcp://localhost:5560"

        let rec reply () = 
            async {
                let rq = Socket.recv rep |> String.decode 
                let resp = sprintf "[%i] Hello %s" i rq
                do! Async.Sleep 500
                resp |> String.encode |> Socket.send rep
                printfn "worker %i replied : %s" i resp
                do! reply () }
        do! reply ()
    }

broker |> Async.Start 
[0 .. 1] |> List.map worker |> List.iter Async.Start

#time "on"
[1..10] |> List.map (sprintf "client %i" >> client) |> Async.Parallel |> Async.Ignore |> Async.RunSynchronously

[2 .. 9] |> List.map worker |> List.iter Async.Start
[1..10] |> List.map (sprintf "client %i" >> client) |> Async.Parallel |> Async.Ignore |> Async.RunSynchronously
