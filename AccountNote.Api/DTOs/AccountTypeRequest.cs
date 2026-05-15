namespace AccountNote.Api.DTOs;

public record AccountTypeRequest(
    bool IsPaid,
    string Title
);