namespace AccountNote.Api.DTOs;

public record TransactionRequest(
    int Id,
    string AccDate, 
    int AccTypeId,
    int AccChId,
    string Description,
    double Amount,
    string Remark
);