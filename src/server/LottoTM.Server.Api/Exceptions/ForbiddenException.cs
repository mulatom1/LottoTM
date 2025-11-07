namespace LottoTM.Server.Api.Exceptions;

/// <summary>
/// Exception thrown when user lacks required permissions
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
