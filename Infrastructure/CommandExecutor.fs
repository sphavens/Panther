namespace Infrastructure

module CommandExecutor =
    open EventSourcing

    type Decide<'State, 'DomainCommand, 'DomainEvent> = 'State -> 'DomainCommand -> 'DomainEvent
    type Apply<'State, 'DomainEvent> = 'State -> 'DomainEvent -> 'State

    //type Exec<'State, 'DomainCommand, 'DomainEvent> = 'State -> 'DomainCommand -> ('State * 'DomainEvent)
    type PersistEvent<'DomainEvent> = 'DomainEvent -> Async<CommitResult<'DomainEvent>>
    type PersistEvents<'DomainCommand, 'DomainEvent> = ('DomainCommand * 'DomainEvent) seq -> Async<BatchCommitResult<'DomainCommand, 'DomainEvent>>

    let execCmds (execFn : Decide<'state, 'cmd, 'ev>) (applyFn : Apply<'state, 'ev>) cmds originalState =
        let rec loop acc = function
        | [] -> acc
        | cmd :: remainingCmds ->
            let latestState =
                match acc with
                | [] -> originalState
                | (_, _, s) :: _ -> s
            let ev = execFn latestState cmd
            let newState = applyFn latestState ev
            let result = cmd, ev, newState
            loop (result :: acc) remainingCmds
        loop [] cmds

    let rec executeCommand decidefn applyfn (persistEv : PersistEvent<'ev>) (persistState : WriteSnapshot<'State>) (getState) (aggregateId : StreamId) cmd = async{
            let! originalState = getState aggregateId

            let ev = decidefn originalState cmd 
            let newState = applyfn originalState ev

            let! evResult = persistEv ev
            match evResult with
            | CommitResult.Success _ ->
                return! persistState aggregateId newState
            | CommitResult.OptimisticConcurrencyFailure ->
                return! executeCommand decidefn applyfn persistEv persistState getState aggregateId cmd
            | CommitResult.OtherFailure exc ->
                return! executeCommand decidefn applyfn persistEv persistState getState aggregateId cmd
        }

    let rec executeCommandBatch decidefn applyfn (persistEvs : PersistEvents<'cmd, 'ev>) (persistState : WriteSnapshot<'State>) getState (aggregateId : StreamId) cmds = async{
            let! originalState = getState aggregateId

            let reversedResult =
                let rec loop acc = function
                | [] -> acc
                | cmd :: remainingCmds ->
                    let latestState =
                        match acc with
                        | [] -> originalState
                        | (_, _, s) :: _ -> s
                    let ev = decidefn latestState cmd
                    let newState = applyfn latestState ev
                    let result = cmd, ev, newState
                    loop (result :: acc) remainingCmds
                loop [] cmds

            let _, _, newState = reversedResult |> List.head
            let cmdsAndEvs =
                reversedResult
                |> List.rev
                |> List.map (fun (c,e,_) -> c,e)

            let! evResult = persistEvs cmdsAndEvs
            match evResult with
            | BatchCommitResult.Success _ ->
                return! persistState aggregateId newState
            | BatchCommitResult.PartialSuccess (_, failedEvs) ->
                //probably doesn't make sense to try committing state post-success first?
                let remainingCmds = failedEvs |> List.map (fun (c,_) -> c)
                return! executeCommandBatch decidefn applyfn persistEvs persistState getState aggregateId remainingCmds
            | BatchCommitResult.OtherFailure exc ->
                return! executeCommandBatch decidefn applyfn persistEvs persistState getState aggregateId cmds
        }
