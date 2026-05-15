namespace AccountNote.Api.Models;

public class AccountType
{
    public int Id { get; set; }
    public bool IsPaid { get; set; } 
    public required string Title { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}