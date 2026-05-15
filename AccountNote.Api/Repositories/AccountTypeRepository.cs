using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountNote.Api.Repositories;

public class AccountTypeRepository(string connectionString) : IAccountTypeRepository
{
    private readonly string _connectionString = connectionString;

    public async Task<IEnumerable<AccountType>> GetAllAccountTypesAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        var result = await conn.QueryAsync<AccountType>(@"
            SELECT Id, IsPaid, Title, CreatedAt FROM AccountType");
        return result.ToList();
    }

    public async Task<AccountType> GetAccountTypeByIdAsync(int id)
    {
        if (id <= 0)
            throw new AccountTypeValidationError("รหัส AccountType ต้องมีค่ามากกว่า 0");

        await using var conn = new SqliteConnection(_connectionString);
        var result = await conn.QueryFirstOrDefaultAsync<AccountType>(@"
            SELECT Id, IsPaid, Title, CreatedAt 
            FROM AccountType
            WHERE Id = @Id",
            new { Id = id }
        );
        return result ?? throw new AccountTypeNotFound($"ไม่พบ AccountType id {id}");
    }

    public async Task AddAccountTypeAsync(AccountType accountType)
    {
        ValidateAccountType(accountType);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            INSERT INTO AccountType( IsPaid, Title) 
            VALUES (@IsPaid, @Title);",
            new
            {
                IsPaid = accountType.IsPaid,
                Title = accountType.Title
            }
        );
    }

    public async Task UpdateAccountTypeAsync(AccountType accountType)
    {
        ValidateAccountType(accountType);

        if (accountType.Id <= 0)
            throw new AccountTypeValidationError("ค่า Id ของ AccountType ต้องมากกว่า 0");

        await GetAccountTypeByIdAsync(accountType.Id);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            UPDATE AccountType  SET IsPaid = @IsPaid, Title = @Title
            WHERE Id = @Id;",
            new
            {
                Id = accountType.Id,
                IsPaid = accountType.IsPaid,
                Title = accountType.Title
            }
        );
    }

    public async Task DeleteAccountTypeAsync(int id)
    {
        if (id <= 0)
            throw new AccountTypeValidationError("ค่า Id ของ AccountType ต้องมากกว่า 0");
        await GetAccountTypeByIdAsync(id);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync("DELETE FROM AccountType WHERE Id = @Id;"
            , new { Id = id });
    }

    public void ValidateAccountType(AccountType accountType)
    {
        if (accountType is null)
            throw new AccountTypeValidationError("ค่า Account เป็น Null");

        if (string.IsNullOrWhiteSpace(accountType.Title))
            throw new AccountTypeValidationError("ค่า Title ของ AccountType เป็นค่าว่าง");
    }
}

