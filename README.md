# zmqdemo

## Links

### zeromq patterns protocol and valid combinations
http://zguide.zeromq.org/page:all

### Spec
https://rfc.zeromq.org/spec:23/ZMTP/

## About Empty Frame

It is the envelope delimiter, when there is no envelope, an empty frame is used as "zero envelope".

Link : http://zguide.zeromq.org/php:chapter3#The-Extended-Reply-Envelope

## Patterns tradeoff

- Async/Sync : block on send
- Drop : discard messages when High water mark occurs. If it does not drop any messages, a block operation occurs. See [cheat sheet](http://zguide.zeromq.org/php:chapter2#Missing-Message-Problem-Solver)
- HWM : high water mark; define a limit to avoid queue memory leak.
- Delivery : One (req/rep/router/dealer/push/pull) or all (pub/sub/xpub/xsub)
- Uni/Multidirectional

## Patterns Specs

- [Req-Rep/Router-Dealer](https://rfc.zeromq.org/spec:28/REQREP/)
- [Pub-Sub](https://rfc.zeromq.org/spec:29/PUBSUB/)
- [Pipeline/Push-Pull](http://rfc.zeromq.org/spec:30/PIPELINE)
- [Exclusive Pair](https://rfc.zeromq.org/spec:31/EXPAIR/)
- [Invalid combinations](http://zguide.zeromq.org/php:chapter3#Invalid-Combinations)
