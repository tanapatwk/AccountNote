using AccountNote.Api.Models;

namespace AccountNote.Api.Repositories;

public interface IAccountChannelRepository
{
    Task<IEnumerable<AccountChannel>> GetAllAccountChannelsAsync();
    Task<AccountChannel> GetAccountChannelByIdAsync(int id);
    Task AddAccountChannelAsync(AccountChannel accountChannel);
    Task UpdateAccountChannelAsync(AccountChannel accountChannel);
    Task DeleteAccountChannelAsync(int id);
}