namespace Nudges.Data.Comms;

public partial class CommsDbContext : DbContext {
    public CommsDbContext(DbContextOptions<CommsDbContext> options)
        : base(options) {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
