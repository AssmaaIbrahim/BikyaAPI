using Bikya.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikya.Data.Configurations
{
    public class ExchangeStatusHistoryConfiguration : IEntityTypeConfiguration<ExchangeStatusHistory>
    {
        public void Configure(EntityTypeBuilder<ExchangeStatusHistory> builder)
        {
            builder.HasKey(h => h.Id);

            builder.Property(h => h.Status)
                   .IsRequired();

            builder.Property(h => h.ChangedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(h => h.ChangedByUserId)
                   .HasMaxLength(450);

            builder.Property(h => h.Message)
                   .HasMaxLength(500);

            // Relationship with ExchangeRequest
            builder.HasOne(h => h.ExchangeRequest)
                   .WithMany(r => r.StatusHistory)
                   .HasForeignKey(h => h.ExchangeRequestId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
