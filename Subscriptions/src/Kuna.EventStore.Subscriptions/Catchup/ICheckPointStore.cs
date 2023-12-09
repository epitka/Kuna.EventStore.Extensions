namespace Kuna.EventStore.Subscriptions.Catchup;

public interface ICheckPointStore
{
    Task<CheckPoint> GetCheckPoint(CancellationToken ct);
}
