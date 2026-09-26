using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeDocumentConfiguration
    : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .ValueGeneratedOnAdd();

        builder.Property(document => document.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(document => document.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(document => document.IndexedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasMany(document => document.Chunks)
            .WithOne(chunk => chunk.Document)
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
