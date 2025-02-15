using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services;

public class BTInviteService : IBTInviteService
{
  #region Properties
  private readonly ApplicationDbContext _context;
  #endregion

  #region Constructor
  public BTInviteService(ApplicationDbContext context)
  {
    _context = context;
  }
  #endregion

  #region Accept Invite
  public async Task<bool> AcceptInviteAsync(Guid token, string userId)
  {
    Invite invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);

    if (invite == null)
    {
      return false;
    }

    try
    {
      invite.IsValid = false;
      invite.InviteeId = userId;
      invite.JoinDate = DateTimeOffset.Now;
      await _context.SaveChangesAsync();

      return true;
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Add New Invite
  public async Task AddNewInviteAsync(Invite invite)
  {
    try
    {
      await _context.Invites.AddAsync(invite);
      await _context.SaveChangesAsync();
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Any Invite
  public async Task<bool> AnyInviteAsync(Guid token, string email, int companyId)
  {
    try
    {
      bool result = await _context.Invites
          .Where(i => i.CompanyId == companyId)
          .AnyAsync(i => i.CompanyToken == token && i.InviteeEmail == email);

      return result;
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Archive Invite
  public async Task ArchiveInviteAsync(Invite invite)
  {
    try
    {
      invite.Archived = true;
      _context.Update(invite);
      await _context.SaveChangesAsync();
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Get All Invites By Company
  public async Task<List<Invite>> GetAllInvitesByCompanyAsync(int companyId)
  {
    try
    {
      List<Invite> invites = await _context.Invites
        .Where(i => i.CompanyId == companyId)
        .Include(i => i.Company)
        .Include(i => i.Invitor)
        .Include(i => i.Project)
        .ToListAsync();
      return invites;
    }
    catch (Exception)
    {
      throw;
    }
  }
  #endregion

  #region Get Invite
  public async Task<Invite> GetInviteAsync(int inviteId, int companyId)
  {
    try
    {
      Invite invite = await _context.Invites
          .Where(i => i.CompanyId == companyId)
          .Include(i => i.Company)
          .Include(i => i.Invitor)
          .Include(i => i.Project)
          .FirstOrDefaultAsync(i => i.Id == inviteId);

      return invite;
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Get Invite
  public async Task<Invite> GetInviteAsync(Guid token, string email, int companyId)
  {
    try
    {
      Invite invite = await _context.Invites
    .Where(i => i.CompanyId == companyId)
    .Include(i => i.Company)
    .Include(i => i.Invitor)
    .Include(i => i.Project)
    .FirstOrDefaultAsync(i => i.CompanyToken == token && i.InviteeEmail == email);

      return invite;
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Update Invite
  public async Task UpdateInviteAsync(Invite invite)
  {
    try
    {
      _context.Update(invite);
      await _context.SaveChangesAsync();
    }
    catch (Exception)
    {

      throw;
    }
  }
  #endregion

  #region Validate Invite Code
  public async Task<bool> ValidateInviteCodeAsync(Guid token)
  {
    if (token == null)
    {
      return false;
    }

    bool result = false;

    Invite invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);

    if (invite != null)
    {
      //determine invite date
      DateTime inviteDate = invite.InviteDate.DateTime;

      //custom validation of invite based on the date it was issued
      //in this case we are allowing an invite to be valid for 7 days
      bool validDate = (DateTime.Now - inviteDate).TotalDays <= 7;

      if (validDate)
      {
        result = invite.IsValid;
      }
    }
    return result;
  }
  #endregion
}
