namespace ReactCore.Backend.Exceptions;

public sealed class ApiUnavailableException : Exception
{
    public ApiUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
