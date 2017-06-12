namespace Iris.Service

// * Imports

open System
open System.Threading
open Iris.Core

// * Resolver

module Resolver =

  // ** tag

  let private tag (str: string) = String.format "Resolver.{0}" str

  // ** ResolverAgent

  type private ResolverAgent = MailboxProcessor<IrisEvent>

  // ** ResolverState

  type private ResolverState = { YoLo: string }

  // ** loop

  let private loop (state: ResolverState) (inbox: ResolverAgent) =

    failwith "huh ha"

  // ** create

  let create () = failwith "create"
