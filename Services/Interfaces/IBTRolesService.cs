using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Identity;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IBTRolesService
  {
    public Task<bool> IsUserInRoleAsync(BTUser user, string roleName);

    public Task<List<IdentityRole>> GetRolesAsync();

    public Task<IEnumerable<string>> GetUserRolesAsync(BTUser user);

    public Task<bool> AddUserToRoleAsync(BTUser user, string roleName);

    public Task<bool> RemoveUserFromRoleAsync(BTUser user, string roleName);

    public Task<bool> RemoveUserFromRolesAsync(BTUser user, IEnumerable<string> roles);


    public Task<List<BTUser>> GetUsersInRoleAsync(string roleName, int companyId);
    public Task<List<BTUser>> GetUsersNotInRoleAsync(string roleName, int companyId);
    public Task<string> GetRoleNameByIdAsync(string roleId);
  }
}
