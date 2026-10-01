using Microsoft.EntityFrameworkCore;
using Pgvector;
using Taleem.Application.Abstractions.Persistence;
using Taleem.Domain.Entities;

namespace Taleem.Infrastructure.Persistence;

public sealed class TaleemDbContext : DbContext, IUnitOfWork
{
    public TaleemDbContext(DbContextOptions<TaleemDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookEdition> BookEditions => Set<BookEdition>();
    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<ExtractedPage> ExtractedPages => Set<ExtractedPage>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<TaleemUnit> TaleemUnits => Set<TaleemUnit>();
    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TaleemUnitTopic> TaleemUnitTopics => Set<TaleemUnitTopic>();
    public DbSet<SearchChunk> SearchChunks => Set<SearchChunk>();
    public DbSet<ReadingProgress> ReadingProgresses => Set<ReadingProgress>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<ReadingHistory> ReadingHistories => Set<ReadingHistory>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<CalendarRecommendationHook> CalendarRecommendationHooks => Set<CalendarRecommendationHook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Book>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.DefaultLanguage).HasMaxLength(16);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<BookEdition>(e =>
        {
            e.HasOne(x => x.Book).WithMany(x => x.Editions).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SourceDocument>(e =>
        {
            e.HasIndex(x => x.StorageKey).IsUnique();
            e.HasOne(x => x.Book).WithMany(x => x.SourceDocuments).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IngestionJob>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasOne(x => x.SourceDocument).WithMany(x => x.IngestionJobs).HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExtractedPage>(e =>
        {
            e.HasIndex(x => new { x.SourceDocumentId, x.PageNumber }).IsUnique();
            e.HasOne(x => x.SourceDocument).WithMany(x => x.ExtractedPages).HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Section>(e =>
        {
            e.HasOne(x => x.Book).WithMany(x => x.Sections).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ParentSection).WithMany(x => x.Children).HasForeignKey(x => x.ParentSectionId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Chapter>(e =>
        {
            e.HasOne(x => x.Book).WithMany(x => x.Chapters).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Section).WithMany(x => x.Chapters).HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaleemUnit>(e =>
        {
            e.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(32);
            e.HasOne(x => x.Book).WithMany(x => x.TaleemUnits).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Chapter).WithMany(x => x.TaleemUnits).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentBlock>(e =>
        {
            e.Property(x => x.BlockType).HasConversion<string>().HasMaxLength(64);
            e.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(32);
            e.HasOne(x => x.Book).WithMany(x => x.ContentBlocks).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TaleemUnit).WithMany(x => x.ContentBlocks).HasForeignKey(x => x.TaleemUnitId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Topic>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(200);
            e.Property(x => x.DisplayName).HasMaxLength(200);
        });

        modelBuilder.Entity<TaleemUnitTopic>(e =>
        {
            e.HasKey(x => new { x.TaleemUnitId, x.TopicId });
            e.HasOne(x => x.TaleemUnit).WithMany(x => x.Topics).HasForeignKey(x => x.TaleemUnitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Topic).WithMany(x => x.Units).HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SearchChunk>(e =>
        {
            e.Property(x => x.MetadataJson).HasColumnType("jsonb");
            e.Property(x => x.Embedding)
                .HasColumnType("vector(1536)")
                .HasConversion(
                    v => v == null ? null : new Vector(v),
                    v => v == null ? null : v.ToArray());
            e.HasOne(x => x.Book).WithMany(x => x.SearchChunks).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TaleemUnit).WithMany(x => x.SearchChunks).HasForeignKey(x => x.TaleemUnitId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReadingProgress>(e =>
        {
            e.HasIndex(x => new { x.UserKey, x.BookId }).IsUnique();
        });

        modelBuilder.Entity<Bookmark>(e =>
        {
            e.HasIndex(x => new { x.UserKey, x.TaleemUnitId }).IsUnique();
        });

        modelBuilder.Entity<UserPreference>(e =>
        {
            e.HasIndex(x => new { x.UserKey, x.Key }).IsUnique();
        });

        modelBuilder.Entity<AnalyticsEvent>(e =>
        {
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(64);
        });

        modelBuilder.Entity<CalendarRecommendationHook>(e =>
        {
            e.Property(x => x.HookType).HasMaxLength(64);
        });
    }
}
