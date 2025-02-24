using System.ComponentModel.DataAnnotations;

namespace ArchiveDomain.Model;

public partial class Author: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Автор документа")]
    public string Name { get; set; } = null!;

    public virtual ICollection<AuthorDocument> AuthorDocuments { get; set; } = new List<AuthorDocument>();
}
