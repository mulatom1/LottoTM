namespace LottoTM.Server.Api.Entities;

/// <summary>
/// Represents a single number in a lottery ticket (normalized structure).
/// Each ticket has exactly 6 numbers in positions 1-6.
/// Numbers must be unique within a ticket and in range 1-49.
/// </summary>
public class TicketNumber
{
    /// <summary>
    /// Primary key - auto-incremented integer identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the ticket this number belongs to
    /// </summary>
    public int TicketId { get; set; }

    /// <summary>
    /// The lottery number (must be between 1 and 49)
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Position of this number in the ticket (1-6).
    /// Must be unique per ticket.
    /// </summary>
    public byte Position { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the parent ticket
    /// </summary>
    public Ticket Ticket { get; set; } = null!;
}
