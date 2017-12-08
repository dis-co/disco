namespace Disco.Core

// * Actors

module Actors =

  // ** tag

  let private tag (str: string) = String.format "MailboxProcessor.{0}" str

  // ** warnQueueLength

  let warnQueueLength t (inbox: MailboxProcessor<_>) =
    // wa't when 't :> rn if the queue length surpasses threshold
    let count = inbox.CurrentQueueLength
    if count > Constants.QUEUE_LENGTH_THRESHOLD then
      count
      |> String.format "Queue length threshold was reached: {0}"
      |> Logger.warn (tag t)
