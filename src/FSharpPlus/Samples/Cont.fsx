#if INTERACTIVE
#r @"../bin/Release/net45/FSharpPlus.dll"
#else
module Samples.Cont
#endif

open FSharpPlus
open FSharpPlus.Data

//https://github.com/fabriceleal/Continuations/blob/master/Continuations/Program.fs

// http://www.markhneedham.com/blog/2009/06/22/f-continuation-passing-style/
// http://nathansuniversity.com/cont_.html

let assertEqual expected actual = 
    if expected <> actual then
        failwithf "%A != %A" expected actual

let g n = n + 1
let f n = g(n + 1) + 1

module ``EXAMPLE g k`` =
    let g_k n k = k(n + 1)
    let f_k n k = g_k(n + 1) (fun x -> k(x + 1))
    f_k 1 (fun x -> assertEqual (f 1) x)
    f_k 2 (fun x -> assertEqual (f 2) x)


module ``EXAMPLE g k in FSharpPlus`` =
    let g_k n = monad { return (n + 1) }
    let f_k n = monad {
      let! x= g_k(n + 1) 
      return x+1
    }
    let n = 2
    let res = Cont.run (f_k n) id
    assertEqual (f n) res

// Max, regular-style
let max x y =
    if x > y then x else y

module ``EXAMPLE max`` =

    // Max, CPS-style
    let max_k x y k =
        if x > y then k x else k y
    // More CPS Styl-ish
    max_k 1 2 (fun x -> assertEqual (max 1 2) x)

module ``EXAMPLE max in FSharpPlus`` =
    let max_k x y = monad {
        return if x > y then x else y }
    let x = Cont.run (max_k 1 2) id
    assertEqual (max 1 2) x

// regular factorial
let rec factorial n =
    if n = 0 then
        1
    else
        n * factorial (n-1)

module ``EXAMPLE factorial`` =
    let rec factorial_k n k =
        if n = 0 then
            k 1
        else
            factorial_k (n-1) (fun x -> k(x * n))

    let fact_n = 5
    factorial_k fact_n (fun x -> assertEqual (factorial fact_n) x)

module ``EXAMPLE factorial in FSharpPlus`` =
    let rec factorial_k n = monad {
        if n = 0 then
            return 1
        else
            let! x=factorial_k (n-1)
            return x * n
      }
    let fact_n = 5
    let x = Cont.run (factorial_k fact_n) id
    assertEqual (factorial fact_n) x
// sum
let rec sum x =
    if x = 1 then
        1
    else
        sum(x - 1) + x

module ``EXAMPLE sum`` =

    let rec sum_k x k =
        if x = 1 then
            k 1
        else
            sum_k(x - 1) (fun y -> k(x + y))

    let sum_n = 5
    sum_k sum_n (fun t ->  assertEqual (sum sum_n) t)
module ``EXAMPLE sum in FSharpPlus`` =

    let rec sum_k x = monad {
        if x = 1 then
            return 1
        else
            let! y=sum_k(x - 1)
            return x + y
      }

    let sum_n = 5
    let t = Cont.run (sum_k sum_n) id
    assertEqual (sum sum_n) t
    
// fibo
let rec fibo n =
    if n = 0 then
        1
    else if n = 1 then
        1
        else
            fibo (n - 1) + fibo (n - 2)

module ``EXAMPLE fibo`` =
    let rec fibo_k n k =
        if n = 0 then
            k 1
        else if n = 1 then 
            k 1
            else
                let k_new1 = (fun x1 -> 
                    let k_new2 = (fun x2 -> k(x1 + x2))
                    fibo_k (n - 2) k_new2
                )
                fibo_k (n - 1) k_new1

    let fibo_n = 9
    fibo_k fibo_n (fun x -> assertEqual (fibo fibo_n) x)
module ``EXAMPLE fibo in FSharpPlus`` =
    let rec fibo_k n =
      monad {
        if n = 0 then
            return 1
        else if n = 1 then 
            return 1
            else
                let! x1 = fibo_k (n - 1)
                let! x2 = fibo_k (n - 2)
                return x1+x2
      }
    let fibo_n = 9
    let x = Cont.run (fibo_k fibo_n) id
    assertEqual (fibo fibo_n) x

// nth
let rec nth n (ls : 'a list) =
    if ls.IsEmpty then
        None
    else if n = 0 then
        Some(ls.Head)
    else
        nth (n - 1) ls.Tail

module ``EXAMPLE nth`` =

    let rec nth_k n (ls : 'a list) k =
        if ls.IsEmpty then
            k(None)
        else if n = 0 then
            k(Some(ls.Head))
        else
            nth_k (n - 1) ls.Tail k
    let ls, i1, i2 = [1;2;3;4;5;6], 3, 15

    // becomes:
    nth_k i1 ls (fun x->assertEqual (nth i1 ls) x)

    nth_k i2 ls (fun x->assertEqual (nth i2 ls) x)


#nowarn "0064"
module ``EXAMPLE nth in FSharpPlus`` =

    let rec nth_k n (ls : 'a list) = monad {
        if ls.IsEmpty then
            return (None)
        else if n = 0 then
            return (Some(ls.Head))
        else
            let! r=nth_k (n - 1) ls.Tail
            return r
      }
    let ls, i1, i2 = [1;2;3;4;5;6], 3, 15

    // becomes:
    let x = Cont.run (nth_k i1 ls) id
    assertEqual (nth i1 ls) x

    let x2 = Cont.run (nth_k i2 ls) id
    assertEqual (nth i2 ls) x2

type Tree =
    | Node of Tree * Tree
    | Leaf
// node_count
let rec node_count = function
                    | Node(lt, rt) -> 1 + node_count(lt)  + node_count(rt)
                    | Leaf -> 0

module ``EXAMPLE count_nodes`` =
    let rec node_count_k tree k = match tree with
                                    | Node(ltree, rtree) ->
                                        let new_k1 = (fun ltree_count -> 
                                            let new_k2 = (fun rtree_count -> 
                                                k(1 + ltree_count + rtree_count)
                                            )
                                            node_count_k rtree new_k2
                                        )
                                        node_count_k ltree new_k1
                                    | Leaf -> k 0

    let t = Node(Node(Leaf, Leaf), Node(Leaf, Node(Leaf, Node(Leaf, Leaf))))
    node_count_k t (fun count -> assertEqual (node_count t)  count)

module ``EXAMPLE count_nodes in FSharpPlus`` =
    let rec node_count_k tree = 
                                monad {
                                    match tree with
                                    | Node(lt, rt) -> 
                                        let! x_lt=node_count_k(lt)
                                        let! x_rt=node_count_k(rt)
                                        return 1 + x_lt + x_rt
                                    | Leaf -> return 0
                                }
    let t = Node(Node(Leaf, Leaf), Node(Leaf, Node(Leaf, Node(Leaf, Leaf))))
    let count = Cont.run (node_count_k t) id
    assertEqual (node_count t)  count