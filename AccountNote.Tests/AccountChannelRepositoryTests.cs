using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using AccountNote.Api.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountNote.Tests;

public class AccountChannelRepositoryTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _connectionString = "Data Source=testdb;Mode=Memory;Cache=Shared";

    public AccountChannelRepositoryTests() 
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        _keepAlive.Execute(@"
            CREATE TABLE IF NOT EXISTS AccountChannel (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Title       TEXT NOT NULL,
                Description TEXT,
                CreatedAt   TEXT NOT NULL DEFAULT (datetime('now'))
            );

            INSERT INTO AccountChannel (Title, Description) VALUES ('เงินสด', NULL);
            INSERT INTO AccountChannel (Title, Description) VALUES ('บัตรเครดิต', 'KTC');
            INSERT INTO AccountChannel (Title, Description) VALUES ('บัญชีธนาคาร', 'KTB');
        ");
    }

    public void Dispose()
    {
        _keepAlive.Execute("DELETE FROM AccountChannel;");
        _keepAlive.Execute("DELETE FROM sqlite_sequence;");
        _keepAlive.Close();
    }

    [Fact]
    public async Task GetAllChannelSuccess()
    {
        var repo = new AccountChannelRepository(_connectionString);
        var result = (await repo.GetAllAccountChannelsAsync()).ToList();
        
        Assert.Equal(3, result.Count());
        Assert.Equal("เงินสด", result[0].Title);
        Assert.Null(result[0].Description);

        Assert.Equal("บัตรเครดิต", result[1].Title);
        Assert.Equal("KTC", result[1].Description);

        Assert.Equal("บัญชีธนาคาร", result[2].Title);
        Assert.Equal("KTB", result[2].Description);
    }
    
    [Fact]
    public async Task GetAccountChannelByIdSuccess()
    {
        var repo = new AccountChannelRepository(_connectionString);
        var result = await repo.GetAccountChannelByIdAsync(1);
    
        Assert.Equal(1, result.Id);
        Assert.Equal("เงินสด", result.Title);
        Assert.Null(result.Description);
    }
    
    [Fact]
    public async Task GetAccountChannelByIdFail_IdInvalid()
    {
        var repo = new AccountChannelRepository(_connectionString);
        await Assert.ThrowsAsync<AccountChannelValidationError>(() => repo.GetAccountChannelByIdAsync(-1));
    }

    [Fact]
    public async Task GetAccountChannelByIdFail_IdNotFound()
    {
        var repo = new AccountChannelRepository(_connectionString);
        await Assert.ThrowsAsync<AccountChannelNotFound>(() => repo.GetAccountChannelByIdAsync(999));
    }

    [Fact]
    public async Task AddAccountChannelSuccess()
    {
        var repo = new AccountChannelRepository(_connectionString);
        var accCh = new AccountChannel
        {
            Title = "เงินปันผล",
            Description = "CPALL"
        };
        await repo.AddAccountChannelAsync(accCh);
        var result = await repo.GetAllAccountChannelsAsync();
        Assert.Contains(result, r => r.Title == accCh.Title && r.Description == accCh.Description);
    }

    [Fact]
    public async Task AddAccountChannellFail_TitleEmpty()
    {
        var repo = new AccountChannelRepository(_connectionString);
        var accCh = new AccountChannel
        {
            Title = "",
            Description = "CPALL"
        };
        
        await Assert.ThrowsAsync<AccountChannelValidationError>(() => repo.AddAccountChannelAsync(accCh));
    }
    
    [Fact]
    public async Task UpdateAccountChannelSuccess()
    {
        var repo = new AccountChannelRepository(_connectionString);
        var accCh = await repo.GetAccountChannelByIdAsync(1);
        
        accCh.Title = "Cache";
        
        await repo.UpdateAccountChannelAsync(accCh);
        var result = await repo.GetAccountChannelByIdAsync(accCh.Id);
        Assert.Equal("Cache", result.Title);
    }

    [Fact]
    public async Task UpdateAccountChannelFail_TitleEmpty()
    {
        var repo = new AccountChannelRepository(_connectionString);
        
        var accCh = await repo.GetAccountChannelByIdAsync(1);
        accCh.Title = "";
        
        await Assert.ThrowsAsync<AccountChannelValidationError>(() => repo.UpdateAccountChannelAsync(accCh));
    }
    
    [Fact]
    public async Task DeleteAccountChannelSuccess()
    {
        var repo = new AccountChannelRepository(_connectionString);
        await repo.DeleteAccountChannelAsync(1);
        await Assert.ThrowsAsync<AccountChannelNotFound>(() => repo.GetAccountChannelByIdAsync(1));
    }

    [Fact]
    public async Task DeleteAccountChannelFail_IdNotFound()
    {
        var repo = new AccountChannelRepository(_connectionString);
        await Assert.ThrowsAsync<AccountChannelNotFound>(() => repo.DeleteAccountChannelAsync(999));
    }
    
    [Fact]
    public async Task DeleteAccountChannelFail_IdInvalid()
    {
        var repo = new AccountChannelRepository(_connectionString);
        await Assert.ThrowsAsync<AccountChannelValidationError>(() => repo.DeleteAccountChannelAsync(-1));
    }
}