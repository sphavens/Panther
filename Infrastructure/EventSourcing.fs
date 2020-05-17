namespace Infrastructure

open System
open FSharp.Control

module EventSourcing =
    type StreamId = string
    type StreamSequenceNumber = int64
    type CommandId = string

    type InputCommand<'cmd> =
        {
            id : CommandId
            commandType : string
            commandData : 'cmd
        }

    type ProvisionalEvent<'ev> =
        {
            streamId : StreamId
            eventType : string
            eventData : 'ev
            timestamp : DateTimeOffset
            causalCommand : CommandId
            ttl : int64
        }
    module CommittedEvent =
        type T<'ev> =
            {
                id : string
                streamId : StreamId
                streamSequenceNumber : StreamSequenceNumber
                eventType : string
                eventData : 'ev
                timestamp : DateTimeOffset
                causalCommand : CommandId
                ttl : int64
            }
        let fromProvisionalEvent (pev:ProvisionalEvent<'ev>) id streamSeqNum =
            {
                id = id
                streamId = pev.streamId
                streamSequenceNumber = streamSeqNum
                eventType = pev.eventType
                eventData = pev.eventData
                timestamp = pev.timestamp
                causalCommand = pev.causalCommand
                ttl = pev.ttl
            }

    type CommandEventPair<'cmd, 'ev> = 'cmd * ProvisionalEvent<'ev>

    type CommitResult<'ev> =
    | Success of CommittedEvent.T<'ev>
    | OptimisticConcurrencyFailure
    | OtherFailure of Exception

    type BatchCommitResult<'cmd, 'ev> =
    | Success of CommittedEvent.T<'ev> seq
    | PartialSuccess of SuccessfulCommits<'ev> * FailedCommits<'cmd, 'ev>
    | OtherFailure of Exception
    and SuccessfulCommits<'ev> = CommittedEvent.T<'ev> seq
    and FailedCommits<'cmd, 'ev> = CommandEventPair<'cmd, 'ev> list

    type AppendEvent<'ev> = ProvisionalEvent<'ev> -> Async<CommitResult<'ev>>
    type AppendEventAtSequenceNumber<'ev> = ProvisionalEvent<'ev> -> StreamSequenceNumber -> Async<CommitResult<'ev>>
    type AppendEvents<'cmd, 'ev> = CommandEventPair<'cmd, 'ev> -> Async<BatchCommitResult<'cmd, 'ev>>
    type AppendEventsAtSequenceNumber<'cmd, 'ev> = CommandEventPair<'cmd, 'ev> -> StreamSequenceNumber -> Async<BatchCommitResult<'cmd, 'ev>>
    type GetEventsAfter<'ev> = StreamId -> StreamSequenceNumber -> AsyncSeq<CommittedEvent.T<'ev>>
    type GetEvents<'ev> = StreamId -> AsyncSeq<CommittedEvent.T<'ev>>
    type GetSnapshot<'T> = 'T -> StreamId -> Async<'T>
    type WriteSnapshot<'T> = StreamId -> 'T -> Async<unit>






