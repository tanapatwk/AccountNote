namespace AccountNote.Api.DTOs;

public record AccountTypeRequest(
    int IsPaid,
    string Title
);