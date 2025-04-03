namespace ArchiveDomain.Model;

public partial class Reservation: Entity
{
    //тут був UserId
    public string UserId { get; set; } = null!;

    public DateOnly ReservationStartDate { get; set; }

    public DateOnly? ReservationEndDate { get; set; }

    public virtual ICollection<ReservationDocument> ReservationDocuments { get; set; } = new List<ReservationDocument>();
}
