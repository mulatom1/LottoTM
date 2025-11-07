namespace LottoTM.Server.Api.Exceptions;

/// <summary>
/// Exception thrown when requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
