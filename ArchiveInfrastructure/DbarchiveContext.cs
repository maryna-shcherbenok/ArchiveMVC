using ArchiveDomain.Model;
using Microsoft.EntityFrameworkCore;

//namespace ArchiveDomain.Model;
namespace ArchiveInfrastructure;

public partial class DbarchiveContext : DbContext
{
    public DbarchiveContext()
    {
    }

    public DbarchiveContext(DbContextOptions<DbarchiveContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<AuthorDocument> AuthorDocuments { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CategoryDocument> CategoryDocuments { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentInstance> DocumentInstances { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<ReservationDocument> ReservationDocuments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-MARYNA; Database=DBArchive; Trusted_Connection=True; TrustServerCertificate=True; ");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<AuthorDocument>(entity =>
        {
            entity.ToTable("AuthorDocument");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("AuthorID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");

            entity.HasOne(d => d.Author).WithMany(p => p.AuthorDocuments)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuthorDocument_Authors");

            entity.HasOne(d => d.Document).WithMany(p => p.AuthorDocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuthorDocument_Documents");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<CategoryDocument>(entity =>
        {
            entity.ToTable("CategoryDocument");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");

            entity.HasOne(d => d.Category).WithMany(p => p.CategoryDocuments)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoryDocument_Categories");

            entity.HasOne(d => d.Document).WithMany(p => p.CategoryDocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CategoryDocument_Documents");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Info).HasColumnType("ntext");
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.PublicationDate).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.TypeId).HasColumnName("TypeID");

            entity.HasOne(d => d.Type).WithMany(p => p.Documents)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_DocumentTypes");
        });

        modelBuilder.Entity<DocumentInstance>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.State).HasMaxLength(50);

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentInstances)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentInstances_Documents");
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
           
        });

        modelBuilder.Entity<ReservationDocument>(entity =>
        {
            entity.ToTable("ReservationDocument");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocumentInstanceId).HasColumnName("DocumentInstanceID");
            entity.Property(e => e.ReservationId).HasColumnName("ReservationID");

            entity.HasOne(d => d.DocumentInstance).WithMany(p => p.ReservationDocuments)
                .HasForeignKey(d => d.DocumentInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationDocument_DocumentInstances");

            entity.HasOne(d => d.Reservation).WithMany(p => p.ReservationDocuments)
                .HasForeignKey(d => d.ReservationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservationDocument_Reservations");
        });

        //тут був User

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
