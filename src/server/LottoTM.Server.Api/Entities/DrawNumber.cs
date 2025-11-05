namespace LottoTM.Server.Api.Entities;

/// <summary>
/// Represents a single drawn number in a lottery draw (normalized structure).
/// Each draw has exactly 6 numbers in positions 1-6.
/// Numbers must be unique within a draw and in range 1-49.
/// </summary>
public class DrawNumber
{
    /// <summary>
    /// Primary key - auto-incremented integer identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the draw this number belongs to
    /// </summary>
    public int DrawId { get; set; }

    /// <summary>
    /// The drawn lottery number (must be between 1 and 49)
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Position of this number in the draw (1-6).
    /// Must be unique per draw.
    /// </summary>
    public byte Position { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the parent draw
    /// </summary>
    public Draw Draw { get; set; } = null!;
}
