namespace AccountNote.Api.Exceptions;

public class TransactionNotFound : Exception
{
    public TransactionNotFound(string message) : base(message) { }
}