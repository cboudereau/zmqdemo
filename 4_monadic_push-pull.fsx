#load "zmqprelude.fsx"
#load "String.fs"

open fszmq

type Disposable<'a> = Disposable of (unit -> unit) * 'a

module Disposable = 
    let run (Disposable (_,f)) x = f x
    let dispose (Disposable (dispose, _)) = dispose ()
    let disposable d = (d :> System.IDisposable).Dispose

    let map f = function Disposable (d, x) -> Disposable (d, f x)
    let bind f = function Disposable (d, x) -> let (Disposable (e, y)) = f x in Disposable (d >> e, y)
    let apply f x = f |> bind (fun f' -> x |> map (fun x' -> f' x'))
    let ret x = Disposable (ignore, x)

let pusher address = 
    let context = new Context ()
    let socket = Context.push context

    Socket.bind socket address

    let dispose () = 
        Disposable.disposable socket ()
        Disposable.disposable context ()
    let push x = Socket.send socket x
    Disposable  (dispose, push)

let puller address = 
    let context = new Context ()
    let socket = Context.pull context

    Socket.connect socket address

    let dispose () = 
        Disposable.disposable socket ()
        Disposable.disposable context ()
    
    let pull () = Socket.recv socket
    Disposable (dispose, pull)

let spusher = pusher "tcp://*:5555" |> Disposable.map ((>>) String.encode)

let spuller = puller "tcp://localhost:5555" |> Disposable.map ((<<) String.decode)

let consolepuller = spuller |> Disposable.map ((<<) (printfn "console : %s"))

Disposable.run spusher "hello"
Disposable.run consolepuller ()

let forever (cts:System.Threading.CancellationTokenSource) f =
    let rec job ctx = if not cts.IsCancellationRequested then Disposable.run f ctx |> job
    let (Disposable (df,_)) = f
    Disposable (df >> cts.Dispose, job)

let cts = new System.Threading.CancellationTokenSource()
let pullForever = forever cts consolepuller

Disposable.run spusher "hello"
Disposable.run pullForever >> async.Return |> async.Delay |> Async.Start

let pipePush = spusher |> Disposable.map (fun f -> fun x -> f x; x)
let pushSameContentForever = forever cts pipePush

Disposable.run pushSameContentForever "hello"

spusher |> Disposable.dispose
spuller |> Disposable.dispose
pullForever |> Disposable.dispose
pushSameContentForever |> Disposable.dispose