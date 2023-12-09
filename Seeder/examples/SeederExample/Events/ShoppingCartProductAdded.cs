namespace SeederExample.Events;

public sealed record ShoppingCartProductAdded(
    Guid CartId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice): IAggregateEvent;
