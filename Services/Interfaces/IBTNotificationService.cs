using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IBTNotificationService
  {
    public Task AddNotificationAsync(Notification notification);

    public Task<Notification> GetNotificationByIdAsync(int notificationId);

    public Task<List<Notification>> GetReceivedNotificationsAsync(string userId);

    public Task<List<Notification>> GetSentNotificationAsync(string userId);

    public Task SendEmailNotificationsByRoleAsync(Notification notification, int companyId, string role);

    public Task SendMembersEmailNotificationsAsync(Notification notification, List<BTUser> members);

    public Task<bool> SendEmailNotificationAsync(Notification notification, string emailSubject);

    public Task UpdateNotificationViewByIdAsync(int notificationId);

    public Task UpdateNotificationAsync(Notification notification);
  }
}
