﻿using Kuna.EventStore.Seeder;
using SeederExample.Events;

namespace SeederExample;

public class EventsGenerator : IEventsGenerator
{
    private const string cartStreamPrefix = "cart-";

    public IEnumerable<(string StreamPrefix, object Event, string AggregateId)> Events()
    {
        var aggregateId = Guid.NewGuid();
        var stringAggregateId = aggregateId.ToString().Replace("-","");

        yield return (cartStreamPrefix,  GetShoppingCartOpened(aggregateId), stringAggregateId);

        var added = GetShoppingCardProductAdded(aggregateId);

        yield return (cartStreamPrefix, added, stringAggregateId);

        if (DateTime.Now.Second % 2 == 0)
        {
            yield return (cartStreamPrefix,GetShoppingCartProductRemoved(added), stringAggregateId);

            yield return (cartStreamPrefix, GetShoppingCartCancelled(aggregateId), stringAggregateId);
        }
        else
        {
            yield return ( cartStreamPrefix, GetShoppingCardCartConfirmed(aggregateId), stringAggregateId);
        }
    }

    private static ShoppingCartOpened GetShoppingCartOpened(Guid aggregateId)
    {
        return new ShoppingCartOpened(aggregateId, Guid.NewGuid());
    }

    private static ShoppingCartProductAdded GetShoppingCardProductAdded(Guid aggregateId)
    {
        return new ShoppingCartProductAdded(
            aggregateId,
            Guid.NewGuid(),
            1,
            10m,
            10m);
    }

    private static ShoppingCartProductRemoved GetShoppingCartProductRemoved(ShoppingCartProductAdded added)
    {
        return new ShoppingCartProductRemoved(added.CartId, added.ProductId, added.Quantity, added.UnitPrice, added.TotalPrice);
    }

    private static ShoppingCartConfirmed GetShoppingCardCartConfirmed(Guid aggregateId)
    {
        return new ShoppingCartConfirmed(aggregateId, DateTime.UtcNow);
    }

    private static ShoppingCartCanceled GetShoppingCartCancelled(Guid aggregateId)
    {
        return new ShoppingCartCanceled(aggregateId, DateTime.UtcNow);
    }
}
