namespace AccountNote.Api.DTOs;

public record AccountTypeResponse(
    int Id,
    bool IsPaid,
    string Title,
    string CreatedAt
);