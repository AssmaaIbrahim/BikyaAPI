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
    public class WishlistConfiguration : IEntityTypeConfiguration<WishList>
    {


        public void Configure(EntityTypeBuilder<WishList> builder)
        {

            builder.HasKey(w => w.Id);

            // Relationships
            builder.HasOne(w => w.User)
                   .WithMany()
                   .HasForeignKey(w => w.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // or Restrict if you don't want auto-deletion

            builder.HasOne(w => w.Product)
                   .WithMany(w=>w.Wishlists)
                   .HasForeignKey(w => w.ProductId)
                   .OnDelete(DeleteBehavior.NoAction); // same here

            // Optional: configure CreatedAt with default value
            builder.Property(w => w.CreatedAt)
                   .HasDefaultValueSql("GETDATE()");


        }
    }
}
