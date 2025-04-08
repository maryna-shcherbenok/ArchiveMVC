using System.ComponentModel.DataAnnotations;

namespace ArchiveInfrastructure.ViewModels
{
    public class ReservationCreateViewModel
    {
        public int DocumentInstanceId { get; set; }

        [Display(Name = "Початок бронювання")]
        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "Вкажіть дату початку бронювання")]
        public DateTime StartDateTime { get; set; } = DateTime.Now;

        [Display(Name = "Завершення бронювання")]
        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "Вкажіть дату завершення бронювання")]
        public DateTime EndDateTime { get; set; } = DateTime.Now.AddHours(2);
    }
}
