using System;
using System.Collections.Generic;
using InventoryZeroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryZeroAPI.Data;

public partial class InventoryZeroDbContext : DbContext
{
    public InventoryZeroDbContext()
    {
    }

    public InventoryZeroDbContext(DbContextOptions<InventoryZeroDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Dispute> Disputes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payout> Payouts { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<SavedProduct> SavedProducts { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=InventoryZeroDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Activity__3214EC073BDE08A4");

            entity.HasIndex(e => e.AdminUserId, "IX_ActivityLogs_AdminUserId");

            entity.HasIndex(e => e.CreatedAt, "IX_ActivityLogs_CreatedAt");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_ActivityLogs_EntityType_EntityId");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.NewValue).HasMaxLength(2000);
            entity.Property(e => e.OldValue).HasMaxLength(2000);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.AdminUser).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.AdminUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ActivityL__Admin__10566F31");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC078A50F8CA");

            entity.HasIndex(e => e.Slug, "UQ__Categori__BC7B5FB699F9BE0F").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Slug).HasMaxLength(100);

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK__Categorie__Paren__398D8EEE");
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Disputes__3214EC075E838A41");

            entity.HasIndex(e => e.OrderId, "IX_Disputes_OrderId");

            entity.HasIndex(e => e.Status, "IX_Disputes_Status");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.EvidenceUrls).HasMaxLength(2000);
            entity.Property(e => e.Reason).HasMaxLength(50);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Open");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Disputes)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Disputes__OrderI__7D439ABD");

            entity.HasOne(d => d.RaisedByUser).WithMany(p => p.DisputeRaisedByUsers)
                .HasForeignKey(d => d.RaisedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Disputes__Raised__7E37BEF6");

            entity.HasOne(d => d.ResolvedByUser).WithMany(p => p.DisputeResolvedByUsers)
                .HasForeignKey(d => d.ResolvedByUserId)
                .HasConstraintName("FK__Disputes__Resolv__00200768");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC071B32BDAD");

            entity.HasIndex(e => e.CreatedAt, "IX_Notifications_CreatedAt");

            entity.HasIndex(e => e.IsRead, "IX_Notifications_IsRead");

            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Data).HasMaxLength(2000);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__03F0984C");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC07E4D934FF");

            entity.HasIndex(e => e.BuyerId, "IX_Orders_BuyerId");

            entity.HasIndex(e => e.CreatedAt, "IX_Orders_CreatedAt");

            entity.HasIndex(e => e.OrderNumber, "IX_Orders_OrderNumber");

            entity.HasIndex(e => e.OrderStatus, "IX_Orders_OrderStatus");

            entity.HasIndex(e => e.PaymentStatus, "IX_Orders_PaymentStatus");

            entity.HasIndex(e => e.ShopId, "IX_Orders_ShopId");

            entity.HasIndex(e => e.OrderNumber, "UQ__Orders__CAC5E743568F612D").IsUnique();

            entity.Property(e => e.BuyerNotes).HasMaxLength(500);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.CancelledAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeliveredAt).HasColumnType("datetime");
            entity.Property(e => e.OrderNumber).HasMaxLength(50);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.PaidAt).HasColumnType("datetime");
            entity.Property(e => e.PaymentIntentId).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.PlatformFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.SellerNotes).HasMaxLength(500);
            entity.Property(e => e.SellerPayout).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShippedAt).HasColumnType("datetime");
            entity.Property(e => e.ShippingAddressLine1).HasMaxLength(255);
            entity.Property(e => e.ShippingAddressLine2).HasMaxLength(255);
            entity.Property(e => e.ShippingCity).HasMaxLength(100);
            entity.Property(e => e.ShippingCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShippingCountry)
                .HasMaxLength(100)
                .HasDefaultValue("South Africa");
            entity.Property(e => e.ShippingPhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ShippingPostalCode).HasMaxLength(20);
            entity.Property(e => e.ShippingProvince).HasMaxLength(100);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrackingCarrier).HasMaxLength(100);
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__BuyerId__5629CD9C");

            entity.HasOne(d => d.Shop).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__ShopId__5812160E");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07053C7118");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Order__619B8048");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Produ__628FA481");
        });

        modelBuilder.Entity<Payout>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payouts__3214EC07CE630C45");

            entity.HasIndex(e => e.ShopId, "IX_Payouts_ShopId");

            entity.HasIndex(e => e.Status, "IX_Payouts_Status");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.StripeTransferId).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.Payouts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payouts__OrderId__787EE5A0");

            entity.HasOne(d => d.Shop).WithMany(p => p.Payouts)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payouts__ShopId__778AC167");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC0734CEADB8");

            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId");

            entity.HasIndex(e => e.CreatedAt, "IX_Products_CreatedAt");

            entity.HasIndex(e => e.ListingEndDate, "IX_Products_ListingEndDate");

            entity.HasIndex(e => e.SalePrice, "IX_Products_SalePrice");

            entity.HasIndex(e => e.ShopId, "IX_Products_ShopId");

            entity.HasIndex(e => e.Status, "IX_Products_Status");

            entity.HasIndex(e => e.Slug, "UQ__Products__BC7B5FB6629C61E6").IsUnique();

            entity.Property(e => e.ApprovedAt).HasColumnType("datetime");
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Condition)
                .HasMaxLength(50)
                .HasDefaultValue("New");
            entity.Property(e => e.ConditionNotes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.Height).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Length).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ListingEndDate)
                .HasDefaultValueSql("(dateadd(day,(7),getdate()))")
                .HasColumnType("datetime");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Weight).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Width).HasColumnType("decimal(10, 2)");

            modelBuilder.Entity<Product>()
              .Ignore(p => p.RemainingQuantity);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__412EB0B6");

            entity.HasOne(d => d.Shop).WithMany(p => p.Products)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__ShopId__403A8C7D");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductI__3214EC070463B40F");

            entity.Property(e => e.AltText).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(500);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductIm__Produ__4F7CD00D");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC0730B6A75F");

            entity.HasIndex(e => e.Rating, "IX_Reviews_Rating");

            entity.HasIndex(e => e.ShopId, "IX_Reviews_ShopId");

            entity.Property(e => e.Comment).HasMaxLength(2000);
            entity.Property(e => e.Cons).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsVerifiedPurchase).HasDefaultValue(true);
            entity.Property(e => e.Pros).HasMaxLength(500);
            entity.Property(e => e.SellerResponse).HasMaxLength(1000);
            entity.Property(e => e.SellerResponseAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Approved");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__OrderId__6754599E");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__Product__68487DD7");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__Reviewe__693CA210");

            entity.HasOne(d => d.Shop).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ShopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__ShopId__6A30C649");
        });

        modelBuilder.Entity<SavedProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SavedPro__3214EC07A900DD16");

            entity.HasIndex(e => e.ProductId, "IX_SavedProducts_ProductId");

            entity.HasIndex(e => e.UserId, "IX_SavedProducts_UserId");

            entity.HasIndex(e => new { e.UserId, e.ProductId }, "UQ_UserProduct").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.SavedProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedProd__Produ__73BA3083");

            entity.HasOne(d => d.User).WithMany(p => p.SavedProducts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedProd__UserI__72C60C4A");
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shops__3214EC076DDCF286");

            entity.HasIndex(e => e.City, "IX_Shops_City");

            entity.HasIndex(e => e.IsVerified, "IX_Shops_IsVerified");

            entity.HasIndex(e => e.Status, "IX_Shops_Status");

            entity.HasIndex(e => e.UserId, "IX_Shops_UserId");

            entity.Property(e => e.AddressLine1).HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasMaxLength(255);
            entity.Property(e => e.BusinessRegistrationNumber).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CommissionRate)
                .HasDefaultValueSql("((15.00))")
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("South Africa");
            entity.Property(e => e.CoverImageUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Longitude).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PayoutDelayDays).HasDefaultValue(7);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.ShopDescription).HasMaxLength(1000);
            entity.Property(e => e.ShopName).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TaxNumber).HasMaxLength(100);
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VerificationDate).HasColumnType("datetime");
            entity.Property(e => e.VerificationNotes).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.Shops)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Shops__UserId__2E1BDC42");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC077023EE80");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.Role, "IX_Users_Role");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534769F5C47").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("Buyer");
            entity.Property(e => e.StripeAccountId).HasMaxLength(255);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(255);
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserAddr__3214EC07118C9219");

            entity.HasIndex(e => e.IsDefault, "IX_UserAddresses_IsDefault");

            entity.HasIndex(e => e.UserId, "IX_UserAddresses_UserId");

            entity.Property(e => e.AddressLine1).HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasMaxLength(255);
            entity.Property(e => e.AddressType)
                .HasMaxLength(50)
                .HasDefaultValue("Home");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("South Africa");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.RecipientName).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserAddre__UserI__09A971A2");
        });

        // ─── SEED ADMIN USER ──────────────────────────────────────
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Email = "admin@inventoryzero.com",
                FullName = "System Administrator",
                PasswordHash = passwordHash,
                Role = "Admin",
                IsActive = true,
                IsEmailVerified = true,
                IsPhoneVerified = false,
                Rating = 0,
                TotalReviews = 0,
                CreatedAt = DateTime.Now
            }
        );

        // ─── SEED CATEGORIES ──────────────────────────────────────
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Clothing", Slug = "clothing", IconUrl = "🧥", Description = "Fashion, apparel, and accessories", SortOrder = 1, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 2, Name = "Electronics", Slug = "electronics", IconUrl = "📱", Description = "Phones, laptops, gadgets", SortOrder = 2, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 3, Name = "Food & Drinks", Slug = "food-drinks", IconUrl = "🥤", Description = "Food items and beverages", SortOrder = 3, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 4, Name = "Furniture", Slug = "furniture", IconUrl = "🛋️", Description = "Home and office furniture", SortOrder = 4, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 5, Name = "Hardware", Slug = "hardware", IconUrl = "🔧", Description = "Tools and building materials", SortOrder = 5, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 6, Name = "Sport & Fitness", Slug = "sport-fitness", IconUrl = "🏋️", Description = "Sports equipment and fitness gear", SortOrder = 6, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 7, Name = "Beauty & Health", Slug = "beauty-health", IconUrl = "💄", Description = "Cosmetics and health products", SortOrder = 7, IsActive = true, CreatedAt = DateTime.Now },
            new Category { Id = 8, Name = "Other", Slug = "other", IconUrl = "📦", Description = "Everything else", SortOrder = 8, IsActive = true, CreatedAt = DateTime.Now }
        );

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}