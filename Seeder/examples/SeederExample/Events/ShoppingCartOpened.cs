namespace SeederExample.Events;

public record ShoppingCartOpened(Guid CartId, Guid ClientId): IAggregateEvent;
