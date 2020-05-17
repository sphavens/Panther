namespace Infrastructure

module CosmosEventStore =
    open EventSourcing
    open FSharp.Control

    let endpointUrl = "https://<your-account>.documents.azure.com:443/"
    let authorizationKey = "<your-account-key>"
    let databaseId = "FamilyDatabase"
    let containerId = "FamilyContainer"

    let getClient () = new Azure.Cosmos.CosmosClient(endpointUrl, authorizationKey)

    let tryAppendEvent (ev: ProvisionalEvent<'T>) = async {
        let evId = "0"
        let rev = CommittedEvent.fromProvisionalEvent ev evId 0L
        return CommitResult.Success(rev)
    }

    let tryAppendEventAtSequenceNumber (ev: ProvisionalEvent<'T>) (sequenceNum: StreamSequenceNumber) = async {
        let evId = string sequenceNum
        let rev = CommittedEvent.fromProvisionalEvent ev evId sequenceNum
        return CommitResult.Success(rev)
    }

    let tryAppendEvents (cmdsAndEvs: CommandEventPair<'cmd, 'ev> seq) = async {
        let revs =
            cmdsAndEvs
            |> Seq.mapi (fun i (_,ev) -> CommittedEvent.fromProvisionalEvent ev (string i) (int64 i))
        return BatchCommitResult.Success(revs)
    }

    let tryAppendEventsAtSequenceNumber (cmdsAndEvs: CommandEventPair<'cmd, 'ev> seq) (sequenceNum: StreamSequenceNumber) = async {
        let revs =
            cmdsAndEvs
            |> Seq.mapi (fun i (_,ev) ->
                let sn = (int64 i) + sequenceNum
                CommittedEvent.fromProvisionalEvent ev (string sn) sn)
        return BatchCommitResult.Success(revs)
    }

    let getEventsAfter (streamId: StreamId) (sequenceNum: StreamSequenceNumber) : AsyncSeq<CommittedEvent.T<'T>> = asyncSeq {
        return Seq.empty
    }

    let getEvents (streamId: StreamId) : AsyncSeq<CommittedEvent.T<'T>> = asyncSeq {
        return Seq.empty
    }
    
    let getSnapshot<'T> (zero: 'T) (streamId: StreamId) = async {
        return zero
    }

    let tryWriteSnapshot (streamId: StreamId) snapshot = async {
        return ()
    }
        
