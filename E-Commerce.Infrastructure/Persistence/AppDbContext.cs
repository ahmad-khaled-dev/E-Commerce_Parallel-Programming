using E_Commerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Inventory> Inventories => Set<Inventory>();
         
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            ConfigureUser(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureInventory(modelBuilder);
            ConfigureCart(modelBuilder);
            ConfigureCartItem(modelBuilder);
            ConfigureOrder(modelBuilder);
            ConfigureOrderItem(modelBuilder);
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {

                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserName)
                .IsRequired()
                .HasMaxLength(100);

                entity.Property(x => x.Email)
              .IsRequired()
              .HasMaxLength(200);

                entity.Property(x => x.PasswordHash)
                    .IsRequired();

                entity.HasIndex(x => x.Email)
                    .IsUnique();
            });

        }
        private static void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

                entity.Property(x => x.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.IsActive)
                    .IsRequired();
            });
        }

         private static void ConfigureInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Quantity)
                    .IsRequired();

                entity.Property(x => x.RowVersion)
                .IsRowVersion();

                entity.HasOne(x => x.Product)
                    .WithOne(x => x.Inventory)
                    .HasForeignKey<Inventory>(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureCart(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.User)
                    .WithOne(x => x.Cart)
                    .HasForeignKey<Cart>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureCartItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Quantity)
                    .IsRequired();

                entity.HasOne(x => x.Cart)
                    .WithMany(x => x.CartItems)
                    .HasForeignKey(x => x.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureOrder(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.OrderStatus)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Orders)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureOrderItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Quantity)
                    .IsRequired();

                entity.Property(x => x.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.HasOne(x => x.Order)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });




        }
    }
}