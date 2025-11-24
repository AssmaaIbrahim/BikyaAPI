using Bikya.Data.Configurations;
using Bikya.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bikya.Data
{
    public class BikyaContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public BikyaContext(DbContextOptions<BikyaContext> options)
            : base(options) 
        { 
        
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<ShippingInfo> ShippingInfos { get; set; }
        public DbSet<Category> Categories { get; set; }
       // public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ExchangeRequest> ExchangeRequests { get; set; }
        public DbSet<ExchangeStatusHistory> ExchangeStatusHistories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<WishList> WishLists { get; set; }
        public DbSet<ChatBotFaq> ChatBotFaqs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new ProductImageConfiguration());
            modelBuilder.ApplyConfiguration(new ReviewConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new ShippingInfoConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
           // modelBuilder.ApplyConfiguration(new WalletConfiguration());
            modelBuilder.ApplyConfiguration(new TransactionConfiguration());
            modelBuilder.ApplyConfiguration(new ExchangeRequestConfiguration());
            modelBuilder.ApplyConfiguration(new ExchangeStatusHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new WishlistConfiguration());


            base.OnModelCreating(modelBuilder);
        }
    }
}
