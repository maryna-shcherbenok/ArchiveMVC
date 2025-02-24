namespace ArchiveDomain.Model;

public partial class User: Entity
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int ReaderCardNumber { get; set; }

    public string? Position { get; set; }

    public int PasswordAccount { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
