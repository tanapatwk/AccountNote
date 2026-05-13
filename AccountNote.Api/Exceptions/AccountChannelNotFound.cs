namespace AccountNote.Api.Exceptions;

public class AccountChannelNotFound : Exception
{
    public AccountChannelNotFound(string message) : base(message) { }
}