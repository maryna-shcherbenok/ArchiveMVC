using System.ComponentModel.DataAnnotations;

namespace ArchiveDomain.Model;

public partial class DocumentInstance: Entity
{
    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Інвентарний номер")]
    public int InventoryNumber { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Стан")]
    public string State { get; set; } = null!;

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Доступність")]
    public bool Available { get; set; }

    [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
    [Display(Name = "Документ")]
    public int DocumentId { get; set; }

    public virtual Document? Document { get; set; }

    public virtual ICollection<ReservationDocument> ReservationDocuments { get; set; } = new List<ReservationDocument>();
}
