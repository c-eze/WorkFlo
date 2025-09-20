using AspnetCoreMvcFull.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using RestSharp;
using RestSharp.Authenticators;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public class BTEmailService : IEmailSender
  {
    #region Properties
    private readonly MailSettings _mailSettings;
    #endregion

    #region Constructor
    public BTEmailService(IOptions<MailSettings> mailSettings)
    {
      _mailSettings = mailSettings.Value;
    }
    #endregion

    #region Send Email
    public async Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
    {
      var options = new RestClientOptions("https://api.mailgun.net")
      {
        Authenticator = new HttpBasicAuthenticator("api", Environment.GetEnvironmentVariable("MailPassword") ?? _mailSettings.MailPassword)
      };

      var emailSender = _mailSettings.Email ?? Environment.GetEnvironmentVariable("Email");

      //MimeMessage email = new();

      //email.Sender = MailboxAddress.Parse(emailSender);
      //email.To.Add(MailboxAddress.Parse(emailTo));
      //email.Subject = subject;

      var builder = new BodyBuilder
      {
        HtmlBody = htmlMessage
      };

      //email.Body = builder.ToMessageBody();

      var client = new RestClient(options);
      var request = new RestRequest("/v3/chikere.dev/messages.mime", Method.Post);
      request.AlwaysMultipartFormData = true;
      request.AddParameter("from", $"Workflo <{emailSender}>");
      request.AddParameter("to", $"<{emailTo}>");
      request.AddParameter("subject", subject); 
      request.AddParameter("text", builder.ToString());
      await client.ExecuteAsync(request);  
    }
    #endregion

      //#region Send Email
      //public async Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
      //{
      //  var emailSender = _mailSettings.Email ?? Environment.GetEnvironmentVariable("Email");

      //  MimeMessage email = new();

      //  email.Sender = MailboxAddress.Parse(emailSender);
      //  email.To.Add(MailboxAddress.Parse(emailTo));
      //  email.Subject = subject;

      //  var builder = new BodyBuilder
      //  {
      //    HtmlBody = htmlMessage
      //  };

      //  email.Body = builder.ToMessageBody();

      //  using var smtp = new SmtpClient();
      //  try
      //  {
      //    var host = _mailSettings.MailHost ?? Environment.GetEnvironmentVariable("MailHost");
      //    var port = _mailSettings.MailPort != 0 ? _mailSettings.MailPort : int.Parse(Environment.GetEnvironmentVariable("MailPort")!);
      //    var password = _mailSettings.MailPassword ?? Environment.GetEnvironmentVariable("MailPassword");

      //    await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
      //    await smtp.AuthenticateAsync(emailSender, password);

      //    await smtp.SendAsync(email);
      //    smtp.Disconnect(true);
      //  }
      //  catch (Exception)
      //  {

      //    throw;
      //  }
      //}
      //#endregion
  }
}
