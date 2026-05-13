namespace AccountNote.Api.Exceptions;

public class TransactionValidationError : Exception
{
    public TransactionValidationError(string message) : base(message) { }
}