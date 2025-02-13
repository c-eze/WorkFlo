using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services
{
  public class BTCompanyInfoService : IBTCompanyInfoService
  {
    #region Properties
    private readonly ApplicationDbContext _context;
    #endregion

    #region Constructor
    public BTCompanyInfoService(ApplicationDbContext context)
    {
      _context = context;
    }
    #endregion

    #region Get All Members
    public async Task<List<BTUser>> GetAllMembersAsync(int companyId)
    {
      List<BTUser> result = new List<BTUser>();
      result = await _context.Users.Where(u => u.CompanyId == companyId).ToListAsync();
      return result;
    }
    #endregion

    #region Get All Projects
    public async Task<List<Project>> GetAllProjectsAsync(int companyId)
    {
      List<Project> result = new List<Project>();
      result = await _context.Projects
                          .Where(p => p.CompanyId == companyId)
                          .Include(p => p.Members)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.Comments)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.Attachments)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.History)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.DeveloperUser)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.OwnerUser)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.Notifications)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.TicketStatus)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.TicketPriority)
                          .Include(p => p.Tickets)
                              .ThenInclude(t => t.TicketType)
                          .Include(p => p.ProjectPriority)
                          .ToListAsync();
      return result;
    }
    #endregion

    #region Get All Ticket
    public async Task<List<Ticket>> GetAllTicketAsync(int companyId)
    {
      List<Ticket> result = new List<Ticket>();
      List<Project> projects = new();

      projects = await GetAllProjectsAsync(companyId);
      result = projects.SelectMany(p => p.Tickets).ToList();

      return result;
    }
    #endregion

    #region Get Company Info By Id
    public async Task<Company> GetCompanyInfoByIdAsync(int? companyId)
    {
      Company result = new();

      if (companyId != null)
      {
        result = await _context.Companies
                        .Include(c => c.Members)
                        .Include(c => c.Projects)
                        .Include(c => c.Invites)
                        .FirstOrDefaultAsync(c => c.Id == companyId);
      }

      return result;
    }
    #endregion
  }
}
