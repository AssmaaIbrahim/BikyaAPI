using Bikya.Data.Enums;
using Bikya.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data.Configurations
{
    public class ExchangeRequestConfiguration : IEntityTypeConfiguration<ExchangeRequest>
    {
        public void Configure(EntityTypeBuilder<ExchangeRequest> builder)
        {
            builder.HasKey(e => e.Id);

            // Status and timestamps
            builder.Property(e => e.Status)
                   .HasConversion<string>()
                   .HasDefaultValue(ExchangeStatus.Pending);
                   
            builder.Property(e => e.StatusMessage)
                   .HasMaxLength(500);
                   
            builder.Property(e => e.RequestedAt)
                   .HasDefaultValueSql("GETUTCDATE()");
                   
            builder.Property(e => e.RespondedAt)
                   .IsRequired(false);
                   
            builder.Property(e => e.CompletedAt)
                   .IsRequired(false);
                   
            builder.Property(e => e.ExpiresAt)
                   .IsRequired(false);

            builder.Property(e => e.Message)
                   .HasMaxLength(500);

            builder.Property(e => e.ProcessedAt)
                   .IsRequired(false);

            builder.Property(e => e.ProcessedBy)
                   .IsRequired(false);

            builder.Property(e => e.UserId)
                   .IsRequired(false);

            builder.HasOne<ApplicationUser>(e => e.User)
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.OfferedProduct)
                   .WithMany() // لا تربطه بمجموعة Requests معينة داخل Product
                   .HasForeignKey(e => e.OfferedProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.RequestedProduct)
                   .WithMany()
                   .HasForeignKey(e => e.RequestedProductId)
                   .OnDelete(DeleteBehavior.Restrict);
                   
            // Order relationships
            builder.HasOne(e => e.OrderForOfferedProduct)
                   .WithOne()
                   .HasForeignKey<ExchangeRequest>(e => e.OrderForOfferedProductId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
                   
            builder.HasOne(e => e.OrderForRequestedProduct)
                   .WithOne()
                   .HasForeignKey<ExchangeRequest>(e => e.OrderForRequestedProductId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
