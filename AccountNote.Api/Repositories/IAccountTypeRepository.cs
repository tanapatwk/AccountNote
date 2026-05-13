using AccountNote.Api.Models;

namespace AccountNote.Api.Repositories;

public interface IAccountTypeRepository
{
    Task<IEnumerable<AccountType>> GetAllAccountTypesAsync();
    Task<AccountType> GetAccountTypeByIdAsync(int id);
    Task AddAccountTypeAsync(AccountType accountType);
    Task UpdateAccountTypeAsync(AccountType accountType);
    Task DeleteAccountTypeAsync(int id);
}