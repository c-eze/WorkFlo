using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IBTInviteService
  {
    public Task<bool> AcceptInviteAsync(Guid token, string userId);

    public Task AddNewInviteAsync(Invite invite);

    public Task<bool> AnyInviteAsync(Guid token, string email, int companyId);

    public Task ArchiveInviteAsync(Invite invite);

    public Task<List<Invite>> GetAllInvitesByCompanyAsync(int companyId);

    public Task<Invite> GetInviteAsync(int inviteId, int companyId);

    public Task<Invite> GetInviteAsync(Guid token, string email, int companyId);

    public Task UpdateInviteAsync(Invite invite);

    public Task<bool> ValidateInviteCodeAsync(Guid token);
  }
}
