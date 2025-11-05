namespace LottoTM.Server.Api.Entities;

/// <summary>
/// Represents a lottery ticket (set of numbers) owned by a user.
/// The actual numbers are stored in the TicketNumbers table (normalized structure).
/// Each user can have maximum 100 tickets (validated in backend).
/// </summary>
public class Ticket
{
    /// <summary>
    /// Primary key - GUID for better scalability and security
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the user who owns this ticket
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// UTC timestamp when ticket was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the owner of this ticket
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Collection of exactly 6 numbers for this ticket (positions 1-6).
    /// Numbers must be in range 1-49 and unique within the ticket.
    /// </summary>
    public ICollection<TicketNumber> Numbers { get; set; } = new List<TicketNumber>();
}
