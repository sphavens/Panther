namespace Inventory
open System

module Domain =
    type FulfillmentNodeId = string
    type Sku = string
    type Units = string
    type Quantity =
    | Each of int32
    | Measure of decimal * Units
    type ReservationId = string
    type OrderId = string
    type CommandId = string
    type EventId = string
    type StreamSequenceNumber = int64

    type InventoryUpdated =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
        }
    type ItemReserved =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
            reservationId : ReservationId
        }
    type OrderCreated =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
            reservationId : ReservationId
            orderId : OrderId
        }
    type OrderItemCancelled =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            reservationId : ReservationId
        }
    type ItemShipped =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
            reservationId : ReservationId
        }

    type Event = 
    | InventoryUpdated of InventoryUpdated
    | ItemReserved of ItemReserved
    | OrderCreated of OrderCreated
    | OrderItemCancelled of OrderItemCancelled
    | ItemShipped of ItemShipped

    type UpdateInventory =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
        }

    type ReserveItem =
        {
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            reservationId : ReservationId
            quantity : Quantity
        }

    type CreateOrder =
        {
            reservationId : ReservationId
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
            orderId : OrderId
        }

    type CancelOrderItem =
        {
            reservationId : ReservationId
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
        }

    type ShipItem =
        {
            reservationId : ReservationId
            //fulfillmentNodeId : FulfillmentNodeId
            //sku : Sku
            quantity : Quantity
        }

    type Command =
    | UpdateInventory of UpdateInventory
    | ReserveItem of ReserveItem
    | CreateOrder of CreateOrder
    | CancelOrderItem of CancelOrderItem
    | ShipItem of ShipItem


    type State =
        {
            fulfillmentNodeId : FulfillmentNodeId
            sku : Sku
            onHand : Quantity
            reserved : Quantity
            availableToSell : Quantity
            reservations : Reservation list
            lastIncludedSequenceNumber : StreamSequenceNumber
        }
        with
            static member initial = 
                {
                    fulfillmentNodeId = String.Empty
                    sku = String.Empty
                    onHand = Each(0)
                    reserved = Each(0)
                    availableToSell = Each(0)
                    reservations = []
                    lastIncludedSequenceNumber = -1L
                }
    and Reservation =
        {
            id : ReservationId
            quantity : Quantity
            state : ReservationState
        }
    and ReservationState =
    | Reserved
    | OrderCreated
    | Cancelled
    | Shipped

    let decide state cmd =
        let floorQty q =
            match q with
            | Each qint -> Each(Math.Max(qint, 0))
            | Measure (qdec, units) -> Measure(Math.Max(qdec, 0M), units)

        match cmd with
        | UpdateInventory c ->
            let q = floorQty c.quantity
            let e : InventoryUpdated =
                {
                    //fulfillmentNodeId = c.fulfillmentNodeId
                    //sku = c.sku
                    quantity = q
                }
            InventoryUpdated(e)
        | ReserveItem c ->
            let q = floorQty c.quantity
            let e : ItemReserved =
                {
                    //fulfillmentNodeId = c.fulfillmentNodeId
                    //sku = c.sku
                    quantity = q
                    reservationId = c.reservationId
                }
            ItemReserved(e)
        | CreateOrder c ->
            let q = floorQty c.quantity
            let e : OrderCreated =
                {
                    //fulfillmentNodeId = c.fulfillmentNodeId
                    //sku = c.sku
                    quantity = q
                    reservationId = c.reservationId
                    orderId = c.orderId
                }
            Event.OrderCreated(e)
        | CancelOrderItem c ->
            let e : OrderItemCancelled =
                {
                    //fulfillmentNodeId = c.fulfillmentNodeId
                    //sku = c.sku
                    reservationId = c.reservationId
                }
            OrderItemCancelled(e)
        | ShipItem c ->
            let q = floorQty c.quantity
            let e : ItemShipped =
                {
                    //fulfillmentNodeId = c.fulfillmentNodeId
                    //sku = c.sku
                    quantity = c.quantity
                    reservationId = c.reservationId
                }
            ItemShipped(e)

    let apply (state:State) ev =
        match ev with
        | InventoryUpdated e ->
            { state with
                onHand = e.quantity;
                availableToSell = e.quantity - state.reserved}
        | ItemReserved e ->
            state
        | Event.OrderCreated e ->
            state
        | OrderItemCancelled e ->
            state
        | ItemShipped e ->
            state