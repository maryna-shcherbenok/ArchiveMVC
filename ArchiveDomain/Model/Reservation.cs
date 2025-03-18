namespace ArchiveDomain.Model;

public partial class Reservation: Entity
{
    public int UserId { get; set; }

    public DateOnly ReservationStartDate { get; set; }

    public DateOnly? ReservationEndDate { get; set; }

    public virtual ICollection<ReservationDocument> ReservationDocuments { get; set; } = new List<ReservationDocument>();

    public virtual User? User { get; set; } = null!;
}
