namespace AccountNote.Api.Models;

public class AccountChannel
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}