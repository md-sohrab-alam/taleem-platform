using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Taleem.Application.Abstractions.Ingestion;
using Taleem.Application.Abstractions.Persistence;
using Taleem.Application.Abstractions.Storage;
using Taleem.Application.Options;
using Taleem.Application.Services;
using Taleem.Infrastructure.Embeddings;
using Taleem.Infrastructure.Ingestion;
using Taleem.Infrastructure.Persistence;
using Taleem.Infrastructure.Services;
using Taleem.Infrastructure.Storage;

namespace Taleem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
                               ?? configuration["DATABASE_URL"]
                               ?? "Host=localhost;Port=5432;Database=taleem;Username=taleem;Password=taleem";

        services.AddDbContext<TaleemDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.MigrationsAssembly(typeof(TaleemDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TaleemDbContext>());
        services.AddScoped<IObjectStorage, LocalObjectStorage>();
        services.AddScoped<IDocumentExtractor, PdfPigDocumentExtractor>();
        services.AddScoped<IBookNormalizer, BookNormalizer>();
        services.AddScoped<IDocumentStructureAnalyzer, LayoutAwareStructureAnalyzer>();
        services.AddScoped<IChunkGenerator, LogicalChunkGenerator>();
        services.AddScoped<IIngestionPipeline, IngestionPipeline>();
        services.AddScoped<IBookPublisher, BookPublisher>();
        services.AddScoped<IPublicContentService, PublicContentService>();
        services.AddScoped<IAdminBookService, AdminBookService>();

        var embedding = configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>() ?? new EmbeddingOptions();
        var provider = (configuration["EMBEDDING_PROVIDER"] ?? embedding.Provider ?? "fake").ToLowerInvariant();
        var apiKey = configuration["EMBEDDING_API_KEY"] ?? embedding.ApiKey;

        if (provider is "openai" && !string.IsNullOrWhiteSpace(apiKey))
        {
            services.Configure<EmbeddingOptions>(o =>
            {
                o.Provider = "openai";
                o.ApiKey = apiKey;
                o.BaseUrl = configuration["EMBEDDING_BASE_URL"] ?? embedding.BaseUrl;
                o.Model = configuration["EMBEDDING_MODEL"] ?? embedding.Model;
                o.Dimensions = embedding.Dimensions;
            });
            services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }

        return services;
    }
}
