namespace ArchiveDomain.Model;

public partial class Reservation: Entity
{
    public string? UserId { get; set; }

    public DateTime ReservationStartDateTime { get; set; }

    public DateTime? ReservationEndDateTime { get; set; }

    public virtual ICollection<ReservationDocument> ReservationDocuments { get; set; } = new List<ReservationDocument>();
}
