using System.Globalization;
using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountNote.Api.Repositories;

public class TransactionRepository(string connectionString) : ITransactionRepository
{
    private readonly string _connectionString = connectionString;

    public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        var result = await conn.QueryAsync<Transaction, AccountType, AccountChannel, Transaction>(@"
            SELECT t.Id, t.AccDate, t.AccTypeId, t.AccChId, t.Description, t.Amount, 
                    t.Remark, t.CreatedAt, acct.Id, acct.IsPaid, acct.Title, acct.CreatedAt, 
                    accCh.Id, accCh.Title, accCh.Description, accCh.CreatedAt
            FROM Transactions t
            INNER JOIN AccountType acct ON t.AccTypeId = acct.Id
            INNER JOIN AccountChannel accCh ON t.AccChId = accCh.Id",
            map: (transaction, accountType, accountChannel) =>
            {
                transaction.AccountType = accountType;
                transaction.AccountChannel = accountChannel;
                return transaction;
            },
            splitOn: "Id,Id");
        return result.ToList();
    }

    public async Task<Transaction> GetTransactionByIdAsync(int id)
    {
        if (id <= 0)
            throw new TransactionValidationError("ค่า Id ของ Transaction ต้องมากว่า 0");

        await using var conn = new SqliteConnection(_connectionString);
        var result = await conn.QueryAsync<Transaction, AccountType, AccountChannel, Transaction>(@"
                SELECT t.Id, t.AccDate, t.AccTypeId, t.AccChId, t.Description, t.Amount,
                        t.Remark, t.CreatedAt , acct.Id, acct.IsPaid, acct.Title, acct.CreatedAt,
                        accCh.Id, accCh.Title, accCh.Description, accCh.CreatedAt
                FROM Transactions t
                INNER JOIN AccountType acct ON t.AccTypeId = acct.Id
                INNER JOIN AccountChannel accCh ON t.AccChId = accCh.Id
                WHERE t.Id = @Id",
            map: (transaction, accountType, accountChannel) =>
            {
                transaction.AccountType = accountType;
                transaction.AccountChannel = accountChannel;
                return transaction;
            },
            param: new { Id = id },
            splitOn: "Id,Id"
        );
        return result.FirstOrDefault() ?? throw new TransactionNotFound($"ไม่พบ Transaction id:{id}");
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        ValidateTransaction(transaction);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
                INSERT INTO Transactions (AccDate, AccTypeId, AccChId, Description, Amount, Remark)
                VALUES (@AccDate, @AccTypeId, @AccChId, @Description, @Amount,  @Remark);",
            new
            {
                AccDate = transaction.AccDate,
                AccTypeId = transaction.AccountType.Id,
                AccChId = transaction.AccountChannel.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Remark = transaction.Remark
            }
        );
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        ValidateTransaction(transaction);

        await GetTransactionByIdAsync(transaction.Id);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            UPDATE Transactions SET AccDate = @AccDate, AccTypeId = @AccTypeId,
                   AccChId = @AccChId, Description = @Description, Amount = @Amount,
                   Remark = @Remark
            WHERE Id = @Id",
            new
            {
                Id = transaction.Id,
                AccDate = transaction.AccDate,
                AccTypeId = transaction.AccountType.Id,
                AccChId = transaction.AccountChannel.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Remark = transaction.Remark
            }
        );
    }

    public async Task DeleteTransactionAsync(int id)
    {
        if (id <= 0)
            throw new TransactionValidationError("ค่า Id ของ Transaction ต้องมากกว่า 0");

        await GetTransactionByIdAsync(id);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            DELETE FROM Transactions WHERE Id = @Id", new { Id = id });
    }

    private void ValidateTransaction(Transaction transaction)
    {
        if (transaction is null)
            throw new TransactionValidationError("ค่า Transaction เป็น Null");

        if (!DateTime.TryParseExact(
                transaction.AccDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            throw new TransactionValidationError("รูปแบบวันต้องเป็น yyyy-MM-dd");
        
        if (transaction.AccountType is null)
            throw new TransactionValidationError("ค่าของ AccountType เป็น Null");
        
        if (transaction.AccountChannel is null)
            throw new TransactionValidationError("ค่าของ AccountChannel เป็น Null");
        
        if (String.IsNullOrWhiteSpace(transaction.Description))
            throw new TransactionValidationError("ค่าของ Description ต้องไม่ว่าง");

        if (transaction.Amount <= 0)
            throw new TransactionValidationError("ค่าของ Amount ต้องมากกว่า 0");
    }
}