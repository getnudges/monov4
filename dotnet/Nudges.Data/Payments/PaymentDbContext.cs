using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nudges.Data.Payments.Models;

namespace Nudges.Data.Payments;

public partial class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MerchantService> MerchantServices { get; set; }

    public virtual DbSet<PaymentConfirmation> PaymentConfirmations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<MerchantService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("merchant_service_pkey");

            entity.ToTable("merchant_service");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<PaymentConfirmation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_confirmation_pkey");

            entity.ToTable("payment_confirmation");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ConfirmationCode)
                .HasMaxLength(300)
                .HasColumnName("confirmation_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.MerchantServiceId).HasColumnName("merchant_service_id");

            entity.HasOne(d => d.MerchantService).WithMany(p => p.PaymentConfirmations)
                .HasForeignKey(d => d.MerchantServiceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("payment_confirmation_merchant_service_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
