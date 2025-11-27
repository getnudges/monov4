using Nudges.Data.Users.Models;

namespace Nudges.Data.Users;

public partial class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options) {
    public virtual DbSet<Admin> Admins { get; set; } = null!;
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<Client> Clients { get; set; } = null!;
    public virtual DbSet<Subscriber> Subscribers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // =====================================================
        // USER  (identity root)
        // =====================================================
        modelBuilder.Entity<User>(entity => {
            entity.HasKey(e => e.Id).HasName("user_pkey");
            entity.ToTable("user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");

            entity.Property(e => e.Locale)
                .HasMaxLength(5)
                .HasColumnName("locale")
                .HasDefaultValueSql("'en-US'::character varying");

            entity.Property(e => e.Subject)
                .HasColumnName("subject");

            // Encryption + Hash Storage
            entity.Property(e => e.PhoneNumberEncrypted)
                .HasColumnName("phone_number_encrypted");

            entity.Property(e => e.PhoneNumberHash)
                .HasMaxLength(64)
                .HasColumnName("phone_number_hash");

            // Hash-based uniqueness
            entity.HasIndex(e => e.PhoneNumberHash)
                .IsUnique()
                .HasDatabaseName("user_phone_number_hash_key");

            entity.Property(e => e.JoinedDate)
                .HasColumnName("joined_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Role navigation mapping
            entity.HasOne(e => e.Admin)
                .WithOne(a => a.IdNavigation)
                .HasForeignKey<Admin>(a => a.Id)
                .HasConstraintName("admin_user_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Client)
                .WithOne(c => c.IdNavigation)
                .HasForeignKey<Client>(c => c.Id)
                .HasConstraintName("client_user_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subscriber)
                .WithOne(s => s.IdNavigation)
                .HasForeignKey<Subscriber>(s => s.Id)
                .HasConstraintName("subscriber_user_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);
        });


        // =====================================================
        // ADMIN  (role extension)
        // =====================================================
        modelBuilder.Entity<Admin>(entity => {
            entity.HasKey(e => e.Id).HasName("admin_pkey");
            entity.ToTable("admin");

            entity.Property(e => e.Id)
                  .ValueGeneratedNever()
                  .HasColumnName("id");
        });


        // =====================================================
        // CLIENT  (role extension)
        // =====================================================
        modelBuilder.Entity<Client>(entity => {
            entity.HasKey(e => e.Id).HasName("client_pkey");
            entity.ToTable("client");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("character varying");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("character varying");

            entity.Property(e => e.CustomerId)
                .HasColumnName("customer_id")
                .HasColumnType("character varying");

            entity.Property(e => e.SubscriptionId)
                .HasColumnName("subscription_id")
                .HasColumnType("character varying");

            entity.Property(e => e.Slug)
                .HasColumnName("slug")
                .HasMaxLength(12)
                .HasDefaultValueSql("''::character varying");

            entity.HasIndex(e => e.Slug)
                .IsUnique()
                .HasDatabaseName("client_slug_key");

            entity.HasIndex(e => e.Slug)
                .HasDatabaseName("idx_client_slug");

            // CLIENT ⇄ SUBSCRIBER many-to-many (via user IDs)
            entity.HasMany(c => c.Subscribers)
                  .WithMany(s => s.Clients)
                  .UsingEntity<Dictionary<string, object>>(
                      "ClientSubscriber",
                      r => r.HasOne<Subscriber>()
                            .WithMany()
                            .HasForeignKey("SubscriberId")
                            .HasConstraintName("client_subscriber_subscriber_id_fkey")
                            .OnDelete(DeleteBehavior.Cascade),
                      l => l.HasOne<Client>()
                            .WithMany()
                            .HasForeignKey("ClientId")
                            .HasConstraintName("client_subscriber_client_id_fkey")
                            .OnDelete(DeleteBehavior.Cascade),
                      j => {
                          j.HasKey("ClientId", "SubscriberId")
                           .HasName("client_subscriber_pkey");

                          j.ToTable("client_subscriber");

                          j.IndexerProperty<Guid>("ClientId")
                            .HasColumnName("client_id");

                          j.IndexerProperty<Guid>("SubscriberId")
                            .HasColumnName("subscriber_id");

                          j.HasIndex(["SubscriberId"], "idx_client_subscriber_subscriber_id");
                      });
        });


        // =====================================================
        // SUBSCRIBER  (role extension)
        // =====================================================
        modelBuilder.Entity<Subscriber>(entity => {
            entity.HasKey(e => e.Id).HasName("subscriber_pkey");
            entity.ToTable("subscriber");

            entity.Property(e => e.Id)
                  .ValueGeneratedNever()
                  .HasColumnName("id");
        });

        OnModelCreatingPartial(modelBuilder);
    }


    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
