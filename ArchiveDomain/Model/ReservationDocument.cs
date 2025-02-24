namespace ArchiveDomain.Model;

public partial class ReservationDocument: Entity
{
    public int ReservationId { get; set; }

    public int DocumentInstanceId { get; set; }

    public virtual DocumentInstance DocumentInstance { get; set; } = null!;

    public virtual Reservation Reservation { get; set; } = null!;
}
