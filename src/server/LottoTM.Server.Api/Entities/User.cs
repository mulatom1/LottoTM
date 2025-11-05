namespace LottoTM.Server.Api.Entities;

/// <summary>
/// Represents a user in the LottoTM system.
/// Stores authentication credentials, admin privileges, and audit information.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key - auto-incremented integer identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique email address used for login (max 255 characters)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password (min. 10 rounds recommended)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating if user has administrator privileges.
    /// Admins can create/edit/delete lottery draws.
    /// Default: false
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// UTC timestamp when user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of lottery tickets owned by this user.
    /// Maximum 100 tickets per user (enforced in backend).
    /// </summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>
    /// Collection of lottery draws created by this user (admin only).
    /// Used for tracking and audit purposes.
    /// </summary>
    public ICollection<Draw> CreatedDraws { get; set; } = new List<Draw>();
}
