using System.ComponentModel.DataAnnotations;

namespace Web.Models.Dtos.User;

public class UpdateUserStatusRequestDto
{
    public string UserName { get; set; }

    /// <summary>
    /// True = go inactive (requires From, To, Comment), False = go active (no schedule info needed).
    /// </summary>
    [Required]
    public bool GoInactive { get; set; }

    /// <summary>
    /// Start of the inactive period. Required when GoInactive is true.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// End of the inactive period. Required when GoInactive is true.
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Reason / comment for the inactivity. Required when GoInactive is true.
    /// </summary>
    [MaxLength(1000)]
    public string Comment { get; set; }
}
