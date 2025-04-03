using Microsoft.AspNetCore.Identity;

namespace ArchiveDomain.Model;

public partial class User : IdentityUser
{
    public string? Position { get; set; }
}
