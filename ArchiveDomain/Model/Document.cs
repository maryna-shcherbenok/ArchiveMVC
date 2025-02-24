using System.ComponentModel.DataAnnotations;

namespace ArchiveDomain.Model;

public partial class Document: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Назва документа")]
    public string Title { get; set; } = null!;

    [Display(Name = "Дата публікації документа")]
    public string? PublicationDate { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Мова документа")]
    public string? Language { get; set; }

    [Display(Name = "Інформація про документ")]
    public string? Info { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Автор документа")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Автор документа")]
    public int TypeId { get; set; }

    public virtual ICollection<AuthorDocument> AuthorDocuments { get; set; } = new List<AuthorDocument>();

    public virtual ICollection<CategoryDocument> CategoryDocuments { get; set; } = new List<CategoryDocument>();

    public virtual ICollection<DocumentInstance> DocumentInstances { get; set; } = new List<DocumentInstance>();

    public virtual DocumentType? Type { get; set; }
}
