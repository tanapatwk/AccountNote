namespace AccountNote.Api.Exceptions;

public class AccountTypeNotFound : Exception
{
    public AccountTypeNotFound(string message) : base(message) { }
}
