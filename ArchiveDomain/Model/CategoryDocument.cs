namespace ArchiveDomain.Model;

public partial class CategoryDocument: Entity
{
    public int DocumentId { get; set; }

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Document Document { get; set; } = null!;
}
