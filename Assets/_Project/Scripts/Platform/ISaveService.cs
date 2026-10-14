namespace Game.Platform
{
    /// <summary>Persistent storage for progress. Steam cloud or console storage replace the file implementation.</summary>
    public interface ISaveService
    {
        void Save<T>(string key, T data);

        bool TryLoad<T>(string key, out T data);
    }
}
