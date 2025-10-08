namespace OfdrwNet.Pkg.Container
{
    public class OfdContainer : IDisposable
    {
        public OfdContainer(string path)
        {
        }
        public OfdContainer(Stream stream)
        {
        }

        public void Add(string fullPath, byte[] data)
        {
        }
        public void Add(string fullPath, string filePath)
        {
        }
        public Task AddAsync(string fullPath, byte[] data) => Task.CompletedTask;

        public void Close()
        {
        }
        public Task CloseAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
