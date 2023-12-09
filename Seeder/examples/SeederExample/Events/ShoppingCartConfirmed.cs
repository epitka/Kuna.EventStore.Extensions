namespace SeederExample.Events;

public record ShoppingCartConfirmed(Guid CartId, DateTime ConfirmedAt): IAggregateEvent;
