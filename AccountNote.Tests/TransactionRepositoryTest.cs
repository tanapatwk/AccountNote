using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using AccountNote.Api.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountNote.Tests;

public class TransactionRepositoryTest : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly string _connectionString = "Data Source=transactionDb;Mode=Memory;Cache=Shared";

    public TransactionRepositoryTest() 
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

            CREATE TABLE IF NOT EXISTS  AccountChannel (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Title       TEXT NOT NULL,
                Description TEXT,
                CreatedAt   TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS  Transactions (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                AccDate     TEXT    NOT NULL CHECK(AccDate GLOB '????-??-??'),
                AccTypeId   INTEGER NOT NULL,
                AccChId     INTEGER NOT NULL,
                Description TEXT    NOT NULL,
                Amount      REAL    NOT NULL CHECK(Amount > 0),
                Remark      TEXT,
                CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (AccTypeId) REFERENCES AccountType(Id),
                FOREIGN KEY (AccChId)   REFERENCES AccountChannel(Id)
            );

            -- Index สำหรับ FK columns ที่ถูก query บ่อย
            CREATE INDEX IF NOT EXISTS  idx_transactions_type    ON Transactions(AccTypeId);
            CREATE INDEX IF NOT EXISTS  idx_transactions_channel ON Transactions(AccChId);
            CREATE INDEX IF NOT EXISTS  idx_transactions_date    ON Transactions(AccDate);


            -- Seed Data
            INSERT INTO AccountType(Title, IsPaid) VALUES ('อาหาร', true);
            INSERT INTO AccountType(Title, IsPaid) VALUES ('ของใช้ส่วนตัว', true);
            INSERT INTO AccountType(Title, IsPaid) VALUES ('เงินเดือน', false);

            INSERT INTO AccountChannel(Title, Description) VALUES ('เงินสด', NULL);
            INSERT INTO AccountChannel(Title, Description) VALUES ('บัญชีธนาคาร', 'KTB');
            INSERT INTO AccountChannel(Title, Description) VALUES ('บัตรเครดิต', 'KTC');

            INSERT INTO Transactions(AccDate, AccTypeId, AccChId, Description, Amount, Remark)
                    VALUES ('2026-04-01', 3, 2, 'เมษายน', 25000, 'เริ่มเดือนแรก');
            INSERT INTO Transactions(AccDate, AccTypeId, AccChId, Description, Amount, Remark)
                    VALUES ('2026-04-02', 1, 1, 'ก๋วยเตี๋ยว', 150, 'ร้านหน้าปากซอย');
            INSERT INTO Transactions(AccDate, AccTypeId, AccChId, Description, Amount, Remark)
                    VALUES ('2026-04-02', 2, 2, 'ซื้อที่ 7-11', 1000, '');
            
        ");
    }

    public void Dispose()
    {
        _keepAlive.Execute("DELETE FROM Transactions;");
        _keepAlive.Execute("DELETE FROM AccountType;");
        _keepAlive.Execute("DELETE FROM AccountChannel;");
        _keepAlive.Execute("DELETE FROM sqlite_sequence;");
        _keepAlive.Close();
    }

    [Fact]
    public async Task GetAllTransactionsSuccess()
    {
        var repo = new TransactionRepository(_connectionString);
        var result = await repo.GetAllTransactionsAsync();

        var resultList = result.ToList();

        Assert.Equal(3, resultList.Count);
        Assert.Equal("2026-04-01", resultList[0].AccDate);
        Assert.Equal(3, resultList[0].AccountType.Id);
        Assert.Equal(2, resultList[0].AccountChannel.Id);
        Assert.Equal("เมษายน", resultList[0].Description);
        Assert.Equal(25000, resultList[0].Amount);
        Assert.Equal("เริ่มเดือนแรก", resultList[0].Remark);

        Assert.Equal("2026-04-02", resultList[1].AccDate);
        Assert.Equal(1, resultList[1].AccountType.Id);
        Assert.Equal(1, resultList[1].AccountChannel.Id);
        Assert.Equal("ก๋วยเตี๋ยว", resultList[1].Description);
        Assert.Equal(150, resultList[1].Amount);
        Assert.Equal("ร้านหน้าปากซอย", resultList[1].Remark);

        Assert.Equal("2026-04-02", resultList[2].AccDate);
        Assert.Equal(2, resultList[2].AccountType.Id);
        Assert.Equal(2, resultList[2].AccountChannel.Id);
        Assert.Equal("ซื้อที่ 7-11", resultList[2].Description);
        Assert.Equal(1000, resultList[2].Amount);
        Assert.Equal("", resultList[2].Remark);
    }
    
    [Fact]
    public async Task GetTransactionByIdSuccess()
    {
        var repo = new TransactionRepository(_connectionString);
        var result = await repo.GetTransactionByIdAsync(1);
        
        Assert.Equal("2026-04-01", result.AccDate);
        Assert.Equal(3, result.AccountType.Id);
        Assert.Equal(2, result.AccountChannel.Id);
        Assert.Equal("เมษายน", result.Description);
        Assert.Equal(25000, result.Amount);
        Assert.Equal("เริ่มเดือนแรก", result.Remark);
    }
    
    [Fact]
    public async Task GetTransactionByIdFail_IdInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.GetTransactionByIdAsync(0));
    }
    
    [Fact]
    public async Task GetTransactionByIdFail_IdNotFound()
    {
        var repo = new TransactionRepository(_connectionString);
        await Assert.ThrowsAsync<TransactionNotFound>(() => repo.GetTransactionByIdAsync(999));
    }

    [Fact]
    public async Task AddTransactionSuccess()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);

        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);

        var  trans = new Transaction
        {
            AccDate = "2026-04-10",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "เสื้อยืด",
            Amount = 250,
            Remark = "เซ็นทรัล"
        };
        
        await repo.AddTransactionAsync(trans);
        var resultList = await repo.GetAllTransactionsAsync();
        var result = resultList.LastOrDefault();
        
        Assert.Equal(trans.AccDate, result!.AccDate);
        Assert.Equal(trans.AccountType.Id, result.AccountType.Id);
        Assert.Equal(trans.AccountChannel.Id, result.AccountChannel.Id);
        Assert.Equal(trans.Description, result.Description);
        Assert.Equal(trans.Amount, result.Amount);
        Assert.Equal(trans.Remark, result.Remark);
    }
    
    [Fact]
    public async Task AddTransactionFail_DateInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);

        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);
        
        var  trans = new Transaction
        {
            AccDate = "25160410",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "เสื้อยืด",
            Amount = 250,
            Remark = "เซ็นทรัล"
        };
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.AddTransactionAsync(trans));
    }
    
    [Fact]
    public async Task AddTransactionFail_DescriptionInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);

        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);
        
        var  trans = new Transaction
        {
            AccDate = "2026-04-10",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "",
            Amount = 100,
            Remark = "เซ็นทรัล"
        };
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.AddTransactionAsync(trans));
    }
    
    [Fact]
    public async Task AddTransactionFail_AmountInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);

        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);
        
        var  trans = new Transaction
        {
            AccDate = "2026-04-10",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "เสื้อยืด",
            Amount = -10,
            Remark = "เซ็นทรัล"
        };
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.AddTransactionAsync(trans));
    }

    
    [Fact]
    public async Task UpdateTransactionSuccess()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);
        
        var accType = await repoAccType.GetAccountTypeByIdAsync(1);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(1);
        
        var trans = await repo.GetTransactionByIdAsync(1);
        trans.AccDate = "2026-01-01";
        trans.AccountType = accType;
        trans.AccountChannel = accChannel;
        trans.Description = "ข้าวมันไก่";
        trans.Amount = 50;
        trans.Remark = "โกไก่";
        
        await repo.UpdateTransactionAsync(trans);
        var result = await repo.GetTransactionByIdAsync(1);
        
        Assert.Equal(trans.AccDate, result.AccDate);
        Assert.Equal(trans.AccountType.Id, result.AccountType.Id);
        Assert.Equal(trans.AccountChannel.Id, result.AccountChannel.Id);
        Assert.Equal(trans.Description, result.Description);
        Assert.Equal(trans.Amount, result.Amount);
        Assert.Equal(trans.Remark, result.Remark);
    }
    
    [Fact]
    public async Task UpdateTransactionFail_DateInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);
        
        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);

        var transUpdate = new Transaction
        {
            AccDate = "26-04-10",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "กาแฟ",
            Amount = 250,
            Remark = "Starbucks"
        };
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.UpdateTransactionAsync(transUpdate));
    }
    
    [Fact]
    public async Task UpdateTransactionFail_DescriptionInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);
        
        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);

        var transUpdate = new Transaction
        {
            AccDate = "2026-04-10",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "",
            Amount = 250,
            Remark = "Starbucks"
        };
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.UpdateTransactionAsync(transUpdate));
    }
    
    [Fact]
    public async Task UpdateTransactionFail_AmountInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        var repoAccType = new AccountTypeRepository(_connectionString);
        var repoAccChannel = new AccountChannelRepository(_connectionString);
        
        var accType = await repoAccType.GetAccountTypeByIdAsync(2);
        var accChannel =  await repoAccChannel.GetAccountChannelByIdAsync(2);

        var transUpdate = new Transaction
        {
            AccDate = "26-04-10",
            AccountChannel = accChannel,
            AccountType = accType,
            Description = "กาแฟ",
            Amount = 0,
            Remark = "Starbucks"
        };
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.UpdateTransactionAsync(transUpdate));
    }
    
    [Fact]
    public async Task  DeleteTransactionSuccess()
    {
        var repo = new TransactionRepository(_connectionString);
        await repo.DeleteTransactionAsync(1);
        await Assert.ThrowsAsync<TransactionNotFound>(() => repo.DeleteTransactionAsync(1));
    }

    [Fact]
    public async Task DeleteTransactionFail_IdInvalid()
    {
        var repo = new TransactionRepository(_connectionString);
        await Assert.ThrowsAsync<TransactionValidationError>(() => repo.DeleteTransactionAsync(-1));
    }
    
    [Fact]
    public async Task DeleteTransactionFail_IdNotFound()
    {
        var repo = new TransactionRepository(_connectionString);
        await Assert.ThrowsAsync<TransactionNotFound>(() => repo.DeleteTransactionAsync(999));
    }
}