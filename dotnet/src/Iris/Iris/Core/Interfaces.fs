[<AutoOpen>]
module Iris.Core.Interfaces

// * IAgentStore

type IAgentStore<'t when 't : not struct> =
  abstract State: 't
  abstract Update: 't -> unit

// * AgentStore module

module AgentStore =
  open System.Threading

  let create<'t when 't : not struct> (initial: 't) =
    let mutable state = initial

    { new IAgentStore<'t> with
        member self.State with get () = state
        member self.Update update =
          Interlocked.CompareExchange<'t>(&state, update, state)
          |> ignore }
