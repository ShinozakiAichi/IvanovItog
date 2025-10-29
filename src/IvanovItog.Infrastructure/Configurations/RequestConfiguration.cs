using IvanovItog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IvanovItog.Infrastructure.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasOne(r => r.Category)
            .WithMany(c => c.Requests)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Status)
            .WithMany(s => s.Requests)
            .HasForeignKey(r => r.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedBy)
            .WithMany(u => u.CreatedRequests)
            .HasForeignKey(r => r.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AssignedTo)
            .WithMany(u => u.AssignedRequests)
            .HasForeignKey(r => r.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.Title, r.Description }).HasDatabaseName("IX_Requests_Search");
    }
}
