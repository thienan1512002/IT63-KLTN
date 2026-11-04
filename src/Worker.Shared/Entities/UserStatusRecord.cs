using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Worker.Shared.Entities;

/// <summary>
/// Lightweight entity mapped to the AspNetUsers table.
/// Only includes columns needed by the Worker for user status operations.
/// The Web project uses the full AppUser (IdentityUser) entity for the same table.
/// </summary>
[Table("AspNetUsers")]
public class AppUserSimplied
{
    public string Id { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? UserName { get; set; }

    public bool IsOnline { get; set; }
}
