namespace LottoTM.Server.Api.Services;

/// <summary>
/// Service for managing lottery tickets and related operations.
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Gets the count of tickets owned by a specific user.
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tickets owned by user</returns>
    Task<int> GetUserTicketCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates 6 random unique numbers in range 1-49 for lottery ticket.
    /// </summary>
    /// <returns>Array of 6 sorted unique numbers</returns>
    int[] GenerateRandomNumbers();

    /// <summary>
    /// Creates a ticket with associated numbers in a database transaction.
    /// </summary>
    /// <param name="userId">Owner user ID</param>
    /// <param name="numbers">Array of exactly 6 unique numbers (1-49)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of created ticket (int)</returns>
    /// <exception cref="ArgumentException">If numbers array is invalid</exception>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">If database operation fails</exception>
    Task<int> CreateTicketWithNumbersAsync(int userId, int[] numbers, CancellationToken cancellationToken = default);
}
