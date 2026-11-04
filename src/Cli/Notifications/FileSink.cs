using Core.Abstractions;
using Core.Events;

namespace Notifications;

public sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = Path.GetFullPath(path);

    public void Notify(CatalogChangedEventArgs e)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_path)!);

        string line =
            $"{e.At:yyyy-MM-dd HH:mm:ss} | {e.Kind} | " +
            $"{e.CopyId} | {e.ReaderId}";

        File.AppendAllText(
            _path,
            line + Environment.NewLine);
    }
}