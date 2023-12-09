namespace SeederExample.Events;

public record ShoppingCartCanceled(Guid CartId, DateTime CanceledAt): IAggregateEvent;
