using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountNote.Api.Repositories;

public class AccountChannelRepository(string connectionString) : IAccountChannelRepository
{
    private readonly string _connectionString = connectionString;
    
    public async Task<IEnumerable<AccountChannel>> GetAllAccountChannelsAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        var result = await conn.QueryAsync<AccountChannel>(@"
            SELECT Id, Title, Description, CreatedAt FROM AccountChannel;
        ");
        return result.ToList();
    }

    public async Task<AccountChannel> GetAccountChannelByIdAsync(int id)
    {
        if (id <= 0)
            throw new AccountChannelValidationError("รหัส AccountChannell ต้องมีค่ามากกว่า 0");
        
        await using var conn = new SqliteConnection(_connectionString);
        var result = await conn.QueryFirstOrDefaultAsync<AccountChannel>(@"
            SELECT Id, Title, Description, CreatedAt FROM AccountChannel
            WHERE Id = @Id;",
            new { Id = id });
        
        return result ?? throw new AccountChannelNotFound($"ไม่พบ AccountChannel {id}");
    }

    public async Task AddAccountChannelAsync(AccountChannel accountChannel)
    {
        if(accountChannel is null)
            throw new AccountChannelValidationError("ค่าของ AccountChannel เป็น null");
        
        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            INSERT INTO AccountChannel (Title, Description)
            VALUES (@Title, @Description);",
            new
            {
                Title = accountChannel.Title,
                Description = accountChannel.Description
            }
        );
    }

    public async Task UpdateAccountChannelAsync(AccountChannel accountChannel)
    {
        if(accountChannel is null)
            throw new AccountChannelValidationError("ค่า AccountChannel เป็น null");
        await GetAccountChannelByIdAsync(accountChannel.Id);
        
        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            UPDATE AccountChannel SET Title = @Title, Description = @Description
            WHERE Id = @Id;", 
            new
            {
                Id = accountChannel.Id,
                Title = accountChannel.Title,
                Description = accountChannel.Description
            }
        );
    }

    public async Task DeleteAccountChannelAsync(int id)
    {
        await GetAccountChannelByIdAsync(id);
        
        await using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            DELETE FROM AccountChannel
            WHERE Id = @Id;", new { Id = id }
        );
    }
}