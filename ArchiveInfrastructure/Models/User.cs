using Microsoft.AspNetCore.Identity;

namespace ArchiveDomain.Model;

public partial class User : IdentityUser
{
    public string? FullName { get; set; }
    //public int ReaderCardNumber { get; internal set; }
}
