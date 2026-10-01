using Taleem.Application.DocumentIr;

namespace Taleem.Infrastructure.Ingestion;

/// <summary>
/// Compatibility entry point — delegates to <see cref="LogicalTextReconstructor"/>.
/// </summary>
public static class ParagraphReconstructor
{
    public static void Reconstruct(LayoutDocument document)
        => LogicalTextReconstructor.Reconstruct(document);
}
