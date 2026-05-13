namespace AccountNote.Api.DTOs;

public record TransactionResponse(
    int Id,
    string AccDate,
    AccountTypeResponse AccountType,
    AccountChannelResponse AccountChannel,
    string Description, 
    double Amount,
    string? Remark,
    string CreatedAt
);