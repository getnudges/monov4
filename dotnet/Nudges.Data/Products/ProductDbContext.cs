using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nudges.Data.Products.Models;

namespace Nudges.Data.Products;

public partial class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Discount> Discounts { get; set; }

    public virtual DbSet<DiscountCode> DiscountCodes { get; set; }

    public virtual DbSet<Plan> Plans { get; set; }

    public virtual DbSet<PlanFeature> PlanFeatures { get; set; }

    public virtual DbSet<PlanSubscription> PlanSubscriptions { get; set; }

    public virtual DbSet<PriceTier> PriceTiers { get; set; }

    public virtual DbSet<Trial> Trials { get; set; }

    public virtual DbSet<TrialOffer> TrialOffers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("discount_pkey");

            entity.ToTable("discount");

            entity.HasIndex(e => e.DiscountCodeId, "IX_discount_discount_code_id");

            entity.HasIndex(e => e.PlanSubscriptionId, "IX_discount_plan_subscription_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountCodeId).HasColumnName("discount_code_id");
            entity.Property(e => e.PlanSubscriptionId).HasColumnName("plan_subscription_id");

            entity.HasOne(d => d.DiscountCode).WithMany(p => p.Discounts)
                .HasForeignKey(d => d.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("discount_discount_code_id_fkey");

            entity.HasOne(d => d.PlanSubscription).WithMany(p => p.Discounts)
                .HasForeignKey(d => d.PlanSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("discount_plan_subscription_id_fkey");
        });

        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("discount_code_pkey");

            entity.ToTable("discount_code");

            entity.HasIndex(e => e.PriceTierId, "IX_discount_code_price_tier_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Discount)
                .HasPrecision(10, 2)
                .HasColumnName("discount");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PriceTierId).HasColumnName("price_tier_id");

            entity.HasOne(d => d.PriceTier).WithMany(p => p.DiscountCodes)
                .HasForeignKey(d => d.PriceTierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("discount_code_price_tier_id_fkey");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("plan_pkey");

            entity.ToTable("plan");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ForeignServiceId)
                .HasMaxLength(200)
                .HasColumnName("foreign_service_id");
            entity.Property(e => e.IconUrl)
                .HasMaxLength(1000)
                .HasColumnName("icon_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<PlanFeature>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("plan_features_pkey");

            entity.ToTable("plan_features");

            entity.Property(e => e.PlanId)
                .ValueGeneratedNever()
                .HasColumnName("plan_id");
            entity.Property(e => e.AiSupport)
                .HasDefaultValue(false)
                .HasColumnName("ai_support");
            entity.Property(e => e.MaxMessages).HasColumnName("max_messages");
            entity.Property(e => e.SupportTier)
                .HasMaxLength(100)
                .HasColumnName("support_tier");

            entity.HasOne(d => d.Plan).WithOne(p => p.PlanFeature)
                .HasForeignKey<PlanFeature>(d => d.PlanId)
                .HasConstraintName("plan_features_plan_id_fkey");
        });

        modelBuilder.Entity<PlanSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("plan_subscription_pkey");

            entity.ToTable("plan_subscription");

            entity.HasIndex(e => e.PriceTierId, "IX_plan_subscription_price_tier_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.PaymentConfirmationId).HasColumnName("payment_confirmation_id");
            entity.Property(e => e.PriceTierId).HasColumnName("price_tier_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'INACTIVE'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.PriceTier).WithMany(p => p.PlanSubscriptions)
                .HasForeignKey(d => d.PriceTierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("plan_subscription_price_tier_id_fkey");
        });

        modelBuilder.Entity<PriceTier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("price_tier_pkey");

            entity.ToTable("price_tier");

            entity.HasIndex(e => e.PlanId, "IX_price_tier_plan_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.ForeignServiceId)
                .HasMaxLength(200)
                .HasColumnName("foreign_service_id");
            entity.Property(e => e.IconUrl)
                .HasMaxLength(1000)
                .HasColumnName("icon_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Plan).WithMany(p => p.PriceTiers)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("price_tier_plan_id_fkey");
        });

        modelBuilder.Entity<Trial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trial_pkey");

            entity.ToTable("trial");

            entity.HasIndex(e => e.PlanSubscriptionId, "IX_trial_plan_subscription_id");

            entity.HasIndex(e => e.TrailOfferId, "IX_trial_trail_offer_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PlanSubscriptionId).HasColumnName("plan_subscription_id");
            entity.Property(e => e.TrailOfferId).HasColumnName("trail_offer_id");

            entity.HasOne(d => d.PlanSubscription).WithMany(p => p.Trials)
                .HasForeignKey(d => d.PlanSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("trial_plan_subscription_id_fkey");

            entity.HasOne(d => d.TrailOffer).WithMany(p => p.Trials)
                .HasForeignKey(d => d.TrailOfferId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("trial_trail_offer_id_fkey");
        });

        modelBuilder.Entity<TrialOffer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trial_offer_pkey");

            entity.ToTable("trial_offer");

            entity.HasIndex(e => e.PriceTierId, "IX_trial_offer_price_tier_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PriceTierId).HasColumnName("price_tier_id");

            entity.HasOne(d => d.PriceTier).WithMany(p => p.TrialOffers)
                .HasForeignKey(d => d.PriceTierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("trial_offer_price_tier_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
