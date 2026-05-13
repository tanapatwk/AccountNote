namespace AccountNote.Api.Exceptions;

public class AccountTypeValidationError : Exception
{
    public  AccountTypeValidationError(string message) : base(message) { }
}