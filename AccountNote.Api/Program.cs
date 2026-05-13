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
    return Results.Created($"/account-types/{accountType.Id}", accountType);
});

app.MapDelete("/account-types/{id}", async(int id, IAccountTypeRepository repo) =>
{
    await repo.DeleteAccountTypeAsync(id);
    return Results.NoContent();
});

app.Run();

