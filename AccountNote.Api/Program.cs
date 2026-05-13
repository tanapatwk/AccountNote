using System.Reflection;
using AccountNote.Api.DTOs;
using AccountNote.Api.Exceptions;
using AccountNote.Api.Models;
using AccountNote.Api.Repositories;
using DbUp;
using DbUp.Engine;
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

// ====== Account Channel Section ========

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

app.Run();

