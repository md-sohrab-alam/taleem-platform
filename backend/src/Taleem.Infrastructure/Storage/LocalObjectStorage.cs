using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taleem.Application.Abstractions.Storage;
using Taleem.Application.Options;

namespace Taleem.Infrastructure.Storage;

public sealed class LocalObjectStorage : IObjectStorage
{
    private readonly StorageOptions _options;
    private readonly ILogger<LocalObjectStorage> _logger;

    public LocalObjectStorage(IOptions<StorageOptions> options, ILogger<LocalObjectStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        Directory.CreateDirectory(Path.GetFullPath(_options.RootPath));
    }

    public async Task<string> SaveAsync(Stream content, string preferredExtension, CancellationToken cancellationToken = default)
    {
        var ext = preferredExtension.StartsWith('.') ? preferredExtension : $".{preferredExtension}";
        var key = $"{Guid.NewGuid():N}{ext}";
        var path = ResolvePath(key);
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, cancellationToken);
        _logger.LogInformation("Stored object {StorageKey}", key);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            throw new FileNotFoundException("Stored object not found.", storageKey);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Deleted object {StorageKey}", storageKey);
        }
        return Task.CompletedTask;
    }

    public bool Exists(string storageKey) => File.Exists(ResolvePath(storageKey));

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Contains("..", StringComparison.Ordinal)
            || storageKey.Contains('/')
            || storageKey.Contains('\\')
            || Path.IsPathRooted(storageKey))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        var root = Path.GetFullPath(_options.RootPath);
        var full = Path.GetFullPath(Path.Combine(root, storageKey));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path traversal rejected.");
        return full;
    }
}
