namespace AccountNote.Api.Exceptions;

public class AccountChannelValidationError : Exception
{
    public  AccountChannelValidationError(string message) : base(message)
    {
    }
}