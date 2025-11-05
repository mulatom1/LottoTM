namespace LottoTM.Server.Api.Entities;

/// <summary>
/// Represents a lottery draw result (global registry shared by all users).
/// The actual drawn numbers are stored in the DrawNumbers table (normalized structure).
/// Each draw date must be unique (one draw per day).
/// Only admin users can create/edit/delete draws.
/// </summary>
public class Draw
{
    /// <summary>
    /// Primary key - auto-incremented integer identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date of the lottery draw (without time component).
    /// Must be unique - only one draw per day allowed.
    /// </summary>
    public DateOnly DrawDate { get; set; }

    /// <summary>
    /// UTC timestamp when this draw was entered into the system
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Foreign key to the admin user who created this draw result.
    /// Used for tracking and audit purposes.
    /// </summary>
    public int CreatedByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the admin user who created this draw
    /// </summary>
    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Collection of exactly 6 numbers for this draw (positions 1-6).
    /// Numbers must be in range 1-49 and unique within the draw.
    /// </summary>
    public ICollection<DrawNumber> Numbers { get; set; } = new List<DrawNumber>();
}
