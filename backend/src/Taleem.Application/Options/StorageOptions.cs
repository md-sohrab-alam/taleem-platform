namespace Taleem.Application.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string RootPath { get; set; } = "storage";
    public long MaxUploadBytes { get; set; } = 50 * 1024 * 1024;
    public string[] AllowedMimeTypes { get; set; } = ["application/pdf"];
    public string[] AllowedExtensions { get; set; } = [".pdf"];
}
