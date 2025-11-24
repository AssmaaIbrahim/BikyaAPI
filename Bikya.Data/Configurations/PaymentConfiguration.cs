using Bikya.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bikya.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                   .IsRequired()
                   .HasColumnType("decimal(18,2)"); // ⬅️ ده بيحل التحذير

            builder.Property(p => p.Status)
                   .IsRequired();

            builder.Property(p => p.Gateway)
                   .IsRequired();

            builder.Property(p => p.StripeSessionId)
                   .HasMaxLength(255);

            builder.Property(p => p.CreatedAt)
                   .HasDefaultValueSql("GETDATE()");
        }
    }
}
