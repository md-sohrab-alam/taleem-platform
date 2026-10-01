using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Options;

namespace Taleem.Infrastructure.Embeddings;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(HttpClient http, IOptions<EmbeddingOptions> options, ILogger<OpenAiEmbeddingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://api.openai.com/v1/"
            : _options.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public string ProviderName => "openai";
    public int Dimensions => _options.Dimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var batch = await EmbedBatchAsync([text], cancellationToken);
        return batch[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                model = _options.Model,
                input = texts,
                dimensions = _options.Dimensions
            };

            using var response = await _http.PostAsJsonAsync("embeddings", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Embedding provider unavailable: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException("Embedding provider unavailable.");
            }

            var parsed = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken: cancellationToken)
                         ?? throw new InvalidOperationException("Empty embedding response.");

            return parsed.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToList();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Embedding provider request failed");
            throw new InvalidOperationException("Embedding provider unavailable.", ex);
        }
    }

    private sealed class OpenAiEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAiEmbeddingData> Data { get; set; } = [];
    }

    private sealed class OpenAiEmbeddingData
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
