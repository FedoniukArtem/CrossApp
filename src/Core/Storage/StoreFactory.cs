using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static IBookStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "library.json");

        IBookStore baseStore = useFile
            ? new FileBookStore(dataPath)
            : new InMemoryBookStore(SampleData.Books());

        return new CachingBookStore(baseStore);
    }
}