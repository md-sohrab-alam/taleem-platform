namespace Taleem.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task<string> SaveAsync(Stream content, string preferredExtension, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    bool Exists(string storageKey);
}
