using System.ComponentModel.DataAnnotations;

namespace ArchiveDomain.Model;

public partial class DocumentType: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name="Тип документа")]
    public string Name { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
