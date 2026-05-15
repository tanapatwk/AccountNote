using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using AccountNote.Api.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountNote.Tests;

public class AccountTypeRepositoryTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _connectionString = "Data Source=testdb;Mode=Memory;Cache=Shared";

    public AccountTypeRepositoryTests() 
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        _keepAlive.Execute(@"
                CREATE TABLE IF NOT EXISTS AccountType (
                   Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                   IsPaid    INTEGER NOT NULL DEFAULT 1,  -- 1=รายจ่าย, 0=รายรับ
                   Title     TEXT    NOT NULL,
                   CreatedAt TEXT    NOT NULL DEFAULT (datetime('now'))
                );

                INSERT INTO AccountType (IsPaid, Title) VALUES (1, 'อาหาร');
                INSERT INTO AccountType (IsPaid, Title) VALUES (0, 'เงินเดือน');
        ");
    }

    public void Dispose()
    {
        _keepAlive.Execute("DELETE FROM AccountType;");
        _keepAlive.Execute("DELETE FROM sqlite_sequence;");
        _keepAlive.Close();
    }

    [Fact]
    public async Task GetAllAccountTypes_ReturnAllAccountTypes()
    {
        var repo = new AccountTypeRepository(_connectionString);
        var result = await repo.GetAllAccountTypesAsync();
        
        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Title == "อาหาร" && t.IsPaid);
        Assert.Contains(result, t => t.Title == "เงินเดือน" && !t.IsPaid);
    }
    
    [Fact]
    public async Task GetAccountTypeById_ReturnAccountType()
    {
        var repo = new AccountTypeRepository(_connectionString);
        var result = await repo.GetAccountTypeByIdAsync(1);
        
        Assert.Equal("อาหาร", result.Title);
        Assert.True(result.IsPaid);
    }
    
    [Fact]
    public async Task GetAccountTypeByIdFail_InvalidId()
    {
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeValidationError>(() => repo.GetAccountTypeByIdAsync(-1));
    }
    
    [Fact]
    public async Task GetAccountTypeByIdFail_NotFound()
    {
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeNotFound>(() => repo.GetAccountTypeByIdAsync(999));
    }
    
    [Fact]
    public async Task GetAccountTypeByIdFail_IdIsZero_ShouldThrow()
    {
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeValidationError>(() => repo.GetAccountTypeByIdAsync(0));
    }

    [Fact]
    public async Task AddAccountTypeSuccess_NoReturn()
    {
        var accType = new AccountType { 
            IsPaid = true,
            Title = "เดินทาง" 
        };
        var repo = new AccountTypeRepository(_connectionString);
        await repo.AddAccountTypeAsync(accType);
        var result = await repo.GetAllAccountTypesAsync();

        Assert.Contains(result, t => t.Title == "เดินทาง" && t.IsPaid);
    }
    
    [Fact]
    public async Task AddAccountTypeFail_TitleIsEmpty()
    {
        var accType = new AccountType
        {
            IsPaid = true,
            Title = ""
        };
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeValidationError>(() => repo.AddAccountTypeAsync(accType));
    }

    [Fact]
    public async Task UpdateAccountTypeIsPaidSuccess_NoReturn()
    {
        var repo = new AccountTypeRepository(_connectionString);
        var accType = await repo.GetAccountTypeByIdAsync(1);
        accType.IsPaid = false;
        
        await repo.UpdateAccountTypeAsync(accType);
        
        var result = await repo.GetAccountTypeByIdAsync(1);
        Assert.False(result.IsPaid);
    }
    
    [Fact]
    public async Task UpdateAccountTypeFail_TitleIsEmpty()
    {
        var accType = new AccountType
        {
            IsPaid = true,
            Title = ""
        };
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeValidationError>(() =>
            repo.UpdateAccountTypeAsync(accType));
    }

    [Fact]
    public async Task DeleteAccountTypeSuccess_NoReturn()
    {
        var repo = new AccountTypeRepository(_connectionString);
        await repo.DeleteAccountTypeAsync(1);
        
        await Assert.ThrowsAsync<AccountTypeNotFound>(() => repo.GetAccountTypeByIdAsync(1));
    }

    [Fact]
    public async Task DeleteAccountTypeFail_IdNotFound()
    {
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeNotFound>(() => repo.DeleteAccountTypeAsync(999));
    }
    
    [Fact]
    public async Task DeleteAccountTypeFail_IdInValid()
    {
        var repo = new AccountTypeRepository(_connectionString);
        await Assert.ThrowsAsync<AccountTypeValidationError>(() => repo.DeleteAccountTypeAsync(-1));
    }
    
}