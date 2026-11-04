using Core.Abstractions;
using Core.Events;

namespace Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e)
    {
        Console.WriteLine(
            $"[SINK] {e.Kind}: {e.CopyId}, читач: {e.ReaderId}");
    }
}