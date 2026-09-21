namespace Drive.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "User is not authenticated.")
        : base(message)
    {
    }
}
