using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nudges.Data.Users.Models;

namespace Nudges.Data.Users;

public partial class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Subscriber> Subscribers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("admin_pkey");

            entity.ToTable("admin");

            entity.HasIndex(e => e.Id, "IX_admin_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("admin_id_fkey");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.MessageSid).HasName("announcement_pkey");

            entity.ToTable("announcement");

            entity.HasIndex(e => e.ClientId, "IX_announcement_client_id");

            entity.Property(e => e.MessageSid)
                .HasMaxLength(34)
                .IsFixedLength()
                .HasColumnName("message_sid");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.SentOn)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("sent_on");

            entity.HasOne(d => d.Client).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("announcement_client_id_fkey");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("client_pkey");

            entity.ToTable("client");

            entity.HasIndex(e => e.PhoneNumber, "client_phone_number_key").IsUnique();

            entity.HasIndex(e => e.Slug, "client_slug_key").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "idx_client_phone_number");

            entity.HasIndex(e => e.Slug, "idx_client_slug");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CustomerId)
                .HasColumnType("character varying")
                .HasColumnName("customer_id");
            entity.Property(e => e.JoinedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("joined_date");
            entity.Property(e => e.Locale)
                .HasMaxLength(5)
                .HasDefaultValueSql("'en-US'::character varying")
                .HasColumnName("locale");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Subject)
                .HasMaxLength(50)
                .HasColumnName("subject");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
            entity.Property(e => e.Slug)
                .HasMaxLength(12)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("slug");
            entity.Property(e => e.SubscriptionId)
                .HasColumnType("character varying")
                .HasColumnName("subscription_id");

            entity.HasMany(d => d.SubscriberPhoneNumbers).WithMany(p => p.Clients)
                .UsingEntity<Dictionary<string, object>>(
                    "ClientSubscriber",
                    r => r.HasOne<Subscriber>().WithMany()
                        .HasForeignKey("SubscriberPhoneNumber")
                        .HasConstraintName("client_subscriber_subscriber_phone_number_fkey"),
                    l => l.HasOne<Client>().WithMany()
                        .HasForeignKey("ClientId")
                        .HasConstraintName("client_subscriber_client_id_fkey"),
                    j =>
                    {
                        j.HasKey("ClientId", "SubscriberPhoneNumber").HasName("client_subscriber_pkey");
                        j.ToTable("client_subscriber");
                        j.HasIndex(new[] { "SubscriberPhoneNumber" }, "IX_client_subscriber_subscriber_phone_number");
                        j.IndexerProperty<Guid>("ClientId").HasColumnName("client_id");
                        j.IndexerProperty<string>("SubscriberPhoneNumber")
                            .HasMaxLength(15)
                            .HasColumnName("subscriber_phone_number");
                    });
        });

        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.HasKey(e => e.PhoneNumber).HasName("subscriber_pkey");

            entity.ToTable("subscriber");

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
            entity.Property(e => e.JoinedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("joined_date");
            entity.Property(e => e.Locale)
                .HasMaxLength(5)
                .HasDefaultValueSql("'en-US'::character varying")
                .HasColumnName("locale");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
