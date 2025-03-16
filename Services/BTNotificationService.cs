using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services
{
  public class BTNotificationService : IBTNotificationService
  {
    #region Properties
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IBTRolesService _rolesService;
    #endregion

    #region Constructor
    public BTNotificationService(ApplicationDbContext context,
                          IEmailSender emailSender,
                          IBTRolesService rolesService)
    {
      _context = context;
      _emailSender = emailSender;
      _rolesService = rolesService;
    }
    #endregion

    #region Add Notification
    public async Task AddNotificationAsync(Notification notification)
    {
      try
      {
        await _context.AddAsync(notification);
        await _context.SaveChangesAsync();
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Get Notification By Id
    public async Task<Notification> GetNotificationByIdAsync(int notificationId)
    {
      try
      {
        return await _context.Notifications
                   .Include(n => n.Ticket)
                   .Include(n => n.Recipient)
                   .Include(n => n.Sender)
                   .FirstOrDefaultAsync(n => n.Id == notificationId);
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Get Received Notifications
    public async Task<List<Notification>> GetReceivedNotificationsAsync(string userId)
    {
      try
      {
        List<Notification> notifications = await _context.Notifications
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .Include(n => n.Ticket)
                .ThenInclude(t => t.Project)
            .Where(n => n.RecipientId == userId).ToListAsync();

        return notifications;
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Get Sent Notification
    public async Task<List<Notification>> GetSentNotificationAsync(string userId)
    {
      try
      {
        List<Notification> notifications = await _context.Notifications
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .Include(n => n.Ticket)
                .ThenInclude(t => t.Project)
            .Where(n => n.SenderId == userId).ToListAsync();

        return notifications;
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Send Email Notifications By Role
    public async Task SendEmailNotificationsByRoleAsync(Notification notification, int companyId, string role)
    {
      try
      {
        List<BTUser> members = await _rolesService.GetUsersInRoleAsync(role, companyId);

        foreach (BTUser bTUser in members)
        {
          notification.RecipientId = bTUser.Id;
          await SendEmailNotificationAsync(notification, notification.Title);
        }
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Send Email Notification
    public async Task<bool> SendEmailNotificationAsync(Notification notification, string emailSubject)
    {
      BTUser btUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == notification.RecipientId);

      if (btUser != null)
      {
        string btUserEmail = btUser.Email;
        string message = notification.Message;

        //send email
        try
        {
          await _emailSender.SendEmailAsync(btUserEmail, emailSubject, message);
          return true;
        }
        catch (Exception)
        {

          throw;
        }
      }
      else
      {
        return false;
      }
    }
    #endregion

    #region Send Members Email Notifications
    public async Task SendMembersEmailNotificationsAsync(Notification notification, List<BTUser> members)
    {
      try
      {
        foreach (BTUser bTUser in members)
        {
          notification.RecipientId = bTUser.Id;
          await SendEmailNotificationAsync(notification, notification.Title);
        }
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Update Notification View By Id
    public async Task UpdateNotificationViewByIdAsync(int notificationId)
    {
      Notification note = await _context.Notifications
                        .FirstOrDefaultAsync(t => t.Id == notificationId);

      if (note.Viewed != false)
      {
        return;
      }

      note.Viewed = true;

      try
      {
        _context.Update(note);
        await _context.SaveChangesAsync();
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion

    #region Update Notification
    public async Task UpdateNotificationAsync(Notification notification)
    {
      try
      {
        _context.Update(notification);
        await _context.SaveChangesAsync();
      }
      catch (Exception)
      {

        throw;
      }
    }
    #endregion
  }
}
