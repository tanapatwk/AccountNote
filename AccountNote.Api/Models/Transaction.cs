namespace AccountNote.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public required string AccDate { get; set; }
    public required AccountType AccountType { get; set; }
    public required AccountChannel AccountChannel { get; set; }
    public required string Description { get; set; }
    public required double  Amount { get; set; }
    public string Remark { get; set; } = string.Empty;
    public required string CreatedAt { get; set; }
}