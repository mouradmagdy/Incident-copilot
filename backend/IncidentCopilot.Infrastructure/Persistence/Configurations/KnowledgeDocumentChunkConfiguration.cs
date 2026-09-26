using IncidentCopilot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentCopilot.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeDocumentChunkConfiguration
    : IEntityTypeConfiguration<KnowledgeDocumentChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocumentChunk> builder)
    {
        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Id)
            .ValueGeneratedOnAdd();

        builder.Property(chunk => chunk.Content)
            .IsRequired();

        builder.Property(chunk => chunk.Section)
            .HasMaxLength(500);

        builder.Property(chunk => chunk.Embedding)
            .HasColumnType("vector(384)");

        builder.HasIndex(chunk => new
        {
            chunk.DocumentId,
            chunk.ChunkIndex
        }).IsUnique();
    }
}
