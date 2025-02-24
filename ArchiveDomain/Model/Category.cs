using System.ComponentModel.DataAnnotations;

namespace ArchiveDomain.Model;

public partial class Category: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Категорія")]
    public string Name { get; set; } = null!;

    public virtual ICollection<CategoryDocument> CategoryDocuments { get; set; } = new List<CategoryDocument>();
}
