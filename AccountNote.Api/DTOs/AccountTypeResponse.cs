namespace AccountNote.Api.DTOs;

public record AccountTypeResponse(
    int Id,
    int IsPaid,
    string Title,
    string CreatedAt
);