namespace ArchiveDomain.Model;

public partial class AuthorDocument: Entity
{
    public int DocumentId { get; set; }

    public int AuthorId { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual Document Document { get; set; } = null!;
}
