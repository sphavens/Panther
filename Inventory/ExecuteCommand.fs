namespace Inventory

module ExecuteCommand =
    open System
    open Microsoft.FSharp.Reflection
    open Infrastructure
    open Infrastructure.EventSourcing

    let wrapDomainEvent (ev:Domain.Event) =
        let pev : ProvisionalEvent<Domain.Event> =
            let toStr (x:'a) =
                match FSharpValue.GetUnionFields(x, typeof<'a>) with
                | case, _ -> case.Name
            {
                streamId = ""
                eventType = toStr ev
                eventData = ev
                timestamp = DateTimeOffset.UtcNow
                ttl = -1L
            }
        pev

    let persistEvCosmos = wrapDomainEvent >> CosmosEventStore.tryAppendEvent

    let persistEvsCosmos =
        Seq.map (fun (c,e) -> (c, wrapDomainEvent e))
        >> CosmosEventStore.tryAppendEvents


    let persistStateCosmos = CosmosEventStore.tryWriteSnapshot
    let getStateCosmos = CosmosEventStore.getSnapshot Domain.State.initial

    let run = async {
            let cmd0 : Domain.Command =
                Domain.Command.UpdateInventory
                    {
                        cmdId = Guid.NewGuid().ToString("N")
                        fulfillmentNodeId = "NY1"
                        sku = "ABCD"
                        quantity = Domain.Quantity.Each(1)
                    }
            let! result = CommandExecutor.executeCommand Domain.decide Domain.apply persistEvCosmos persistStateCosmos getStateCosmos "fakesku" cmd0
            return ()
        }

    let runBatch = async {
            let orig _ = async { return Domain.State.initial }
            let cmd0 : Domain.Command =
                Domain.Command.UpdateInventory
                    {
                        cmdId = Guid.NewGuid().ToString("N")
                        fulfillmentNodeId = "NY1"
                        sku = "ABCD"
                        quantity = Domain.Quantity.Each(1)
                    }
            let! result = CommandExecutor.executeCommandBatch Domain.decide Domain.apply persistEvsCosmos persistStateCosmos orig "fakesku" [cmd0]
            return ()
        }
