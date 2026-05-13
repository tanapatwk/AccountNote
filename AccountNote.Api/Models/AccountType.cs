namespace AccountNote.Api.Models;

public class AccountType
{
    public int Id { get; set; }
    public int IsPaid { get; set; } = 1;
    public required string Title { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}