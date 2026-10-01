using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Taleem.Application.Options;

namespace Taleem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddOptions<ChunkingOptions>().BindConfiguration(ChunkingOptions.SectionName);
        services.AddOptions<EmbeddingOptions>().BindConfiguration(EmbeddingOptions.SectionName);
        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName);
        services.AddOptions<AdminAuthOptions>().BindConfiguration(AdminAuthOptions.SectionName);
        return services;
    }
}
