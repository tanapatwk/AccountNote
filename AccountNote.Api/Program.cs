using System.Data.Common;
using System.Reflection;
using AccountNote.Api.DTOs;
using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using AccountNote.Api.Repositories;
using DbUp;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");
    
var upgrader = DeployChanges.To
    .SqliteDatabase(connectionString)
    .WithScriptsAndCodeEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .Build();
    
var result = upgrader.PerformUpgrade();
if (!result.Successful)
{  
    Log.Error("Migration ล้มเหลว {result.Error}", result.Error);
}
else
{
    Log.Information("Migration สำเร็จ");
}

builder.Services.AddSingleton<IAccountChannelRepository>(new AccountChannelRepository(connectionString));
builder.Services.AddSingleton<ITransactionRepository>(new TransactionRepository(connectionString));
builder.Services.AddSingleton<IAccountTypeRepository>(new AccountTypeRepository(connectionString));

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        if (exception is TransactionValidationError tvError)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = tvError.Message
            });
        }
        else if (exception is TransactionNotFound tvNotFound)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = tvNotFound.Message
            });
        }
        else if (exception is AccountChannelValidationError aChValid)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = aChValid.Message,
            });
        }
        else if (exception is AccountChannelNotFound accChNotFound)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = accChNotFound.Message
            });
        }
        else if (exception is AccountTypeNotFound accTypeNotFound)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = accTypeNotFound.Message
            });
        }
        else if (exception is AccountTypeValidationError accTypeValid)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = accTypeValid.Message
            });
        }
        else if (exception is DbException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "เกิดข้อผิดพลาดในการบันทึกข้อมูล กรุณาตรวจสอบอีกครั้ง"
            });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error occurred. Please try again later."
            });
        }
    });
});

// ====== Account Type Section ========

// GetAllAccountTypesAsync()
app.MapGet("/account-types", async (IAccountTypeRepository repo) =>
{
    var accountTypes = (await repo.GetAllAccountTypesAsync())
        .Select(t => new AccountTypeResponse(
            t.Id,
            t.IsPaid,
            t.Title,
            t.CreatedAt
        ));
    return Results.Ok(accountTypes);
});


// GetAccountTypeByIdAsync()
app.MapGet("/account-types/{id}", async (IAccountTypeRepository repo, int id) =>
{
    var accountType = (await repo.GetAccountTypeByIdAsync(id));
    return Results.Ok(new AccountTypeResponse(
        accountType.Id,
        accountType.IsPaid,
        accountType.Title,
        accountType.CreatedAt
    ));
});

// AddAccountTypeAsync
app.MapPost("/account-types/", async (AccountTypeRequest request, IAccountTypeRepository repo) =>
{
    var accountType = new AccountType
    {
        Title = request.Title,
        IsPaid = request.IsPaid
    };
    
    await repo.AddAccountTypeAsync(accountType);
    return Results.Created("/account-types/", null);
});

app.MapPut("/account-types/{id}", async (int id, AccountTypeRequest request, IAccountTypeRepository repo) =>
{
    var accountType = new AccountType
    {
        Id = id,
        Title = request.Title,
        IsPaid = request.IsPaid
    };
    
    await repo.UpdateAccountTypeAsync(accountType);
    return Results.NoContent();
});

app.MapDelete("/account-types/{id}", async(int id, IAccountTypeRepository repo) =>
{
    await repo.DeleteAccountTypeAsync(id);
    return Results.NoContent();
});

// ====== Account Channel Section ========

app.MapGet("/account-channels/", async (IAccountChannelRepository repo) =>
{
    var accountCh = (await repo.GetAllAccountChannelsAsync())
        .Select(t => new AccountChannelResponse(
            t.Id, t.Title, t.Description, t.CreatedAt));
    return Results.Ok(accountCh);
});

app.MapGet("/account-channels/{id}", async (int id, IAccountChannelRepository repo) =>
{
    var accountCh = (await repo.GetAccountChannelByIdAsync(id));
    return Results.Ok(new AccountChannelResponse(
        accountCh.Id, accountCh.Title, accountCh.Description, accountCh.CreatedAt));
});

app.MapPost("/account-channels/", async (AccountChannelRequest request, IAccountChannelRepository repo) =>
{
    var accountChannel = new AccountChannel
    {
        Title = request.Title,
        Description = request.Description
    };
    
    await repo.AddAccountChannelAsync(accountChannel);
    return Results.Created("/account-channels/", null);
});

app.MapPut("/account-channels/{id}", async (int id, AccountChannelRequest request, IAccountChannelRepository repo) =>
{
    var accountChannel = new AccountChannel
    {
        Id = id,
        Title = request.Title,
        Description = request.Description
    };
    await repo.UpdateAccountChannelAsync(accountChannel);
    return Results.NoContent();
});

app.MapDelete("/account-channels/{id}", async (int id, IAccountChannelRepository repo) =>
{
    await repo.DeleteAccountChannelAsync(id);
    return Results.NoContent();
});

// ======= Transaction Section =======

app.MapGet("/transactions/", async (ITransactionRepository repo) =>
{
    var transactions = (await repo.GetAllTransactionsAsync())
        .Select(t => new TransactionResponse(
            t.Id,
            t.AccDate,
            new AccountTypeResponse(
                t.AccountType.Id,
                t.AccountType.IsPaid,
                t.AccountType.Title,
                t.AccountType.CreatedAt
            ),
            new AccountChannelResponse(
                t.AccountChannel.Id,
                t.AccountChannel.Title,
                t.AccountChannel.Description,
                t.AccountChannel.CreatedAt
            ),
            t.Description,
            t.Amount,
            t.Remark,
            t.CreatedAt
        ));
    return Results.Ok(transactions);
});

app.MapGet("/transactions/{id}", async (int id, ITransactionRepository repo) =>
{
    var transaction = (await repo.GetTransactionByIdAsync(id));
    return Results.Ok(new TransactionResponse(
        transaction.Id,
        transaction.AccDate,
        new AccountTypeResponse(
            transaction.AccountType.Id,
            transaction.AccountType.IsPaid,
            transaction.AccountType.Title,
            transaction.AccountType.CreatedAt
        ),
        new AccountChannelResponse(
            transaction.AccountChannel.Id,
            transaction.AccountChannel.Title,
            transaction.AccountChannel.Description,
            transaction.AccountChannel.CreatedAt
        ), 
        transaction.Description,
        transaction.Amount,
        transaction.Remark,
        transaction.CreatedAt
    ));
});

app.MapPost("/transactions/", async (
    TransactionRequest request, 
    ITransactionRepository transRepo,
    IAccountChannelRepository accChRepo,
    IAccountTypeRepository accTypeRepo ) =>
{
    var accType = await accTypeRepo.GetAccountTypeByIdAsync(request.AccTypeId);
    var accCh = await accChRepo.GetAccountChannelByIdAsync(request.AccChId);
    
    await transRepo.AddTransactionAsync( new Transaction
    {   
        AccDate = request.AccDate,
        AccountType =  accType,
        AccountChannel = accCh,
        Description = request.Description,
        Amount = request.Amount,
        Remark = request.Remark,
    });
    return Results.Created("/transactions/", null);
});

app.MapPut("/transactions/{id}", async (
    int id, 
    TransactionRequest request,
    ITransactionRepository transRepo,
    IAccountTypeRepository accTypeRepo,
    IAccountChannelRepository accChRepo) =>
{
    var accType = await accTypeRepo.GetAccountTypeByIdAsync(request.AccTypeId);
    var accCh = await accChRepo.GetAccountChannelByIdAsync(request.AccChId);

    await transRepo.UpdateTransactionAsync(new Transaction
    {
        Id = id,
        AccDate = request.AccDate,
        AccountType =  accType,
        AccountChannel = accCh,
        Description = request.Description,
        Amount = request.Amount,
        Remark = request.Remark
    });
    return Results.NoContent();
});

app.MapDelete("/transactions/{id}", async (int id, ITransactionRepository repo) =>
{
    await repo.DeleteTransactionAsync(id);
    return Results.NoContent();
});

app.Run();

