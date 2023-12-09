namespace Kuna.EventStore.Subscriptions.Catchup
{
    public record CheckPoint(ulong CommitPosition, ulong PreparePosition)
    {
    }
}
