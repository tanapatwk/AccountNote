namespace AccountNote.Api.DTOs;

public record AccountChannelResponse(
    int Id,
    string Title,
    string? Description,
    string CreatedAt
);