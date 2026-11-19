using Core.Abstractions;
using Core.Configuration;

namespace Core.Storage;

public static class StoreFactory
{
    public static IBookStore Create(string[] args, StorageOptions? storage = null)
    {
        bool useFile = args.Contains("--file");
        string dataPath = (storage ?? new StorageOptions()).ResolveFullPath(AppContext.BaseDirectory);

        IBookStore baseStore = useFile
            ? new FileBookStore(dataPath)
            : new InMemoryBookStore(SampleData.Books());

        return new CachingBookStore(baseStore);
    }
}
