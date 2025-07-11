// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations; 
using AspnetCoreMvcFull.Models.Enums;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages; 

namespace AspnetCoreMvcFull.Areas.Identity.Pages.Account;

public class RegisterByInviteModel : PageModel
{
  private readonly SignInManager<BTUser> _signInManager;
  private readonly UserManager<BTUser> _userManager;
  private readonly IUserStore<BTUser> _userStore;
  private readonly IUserEmailStore<BTUser> _emailStore;
  private readonly ILogger<RegisterByInviteModel> _logger;
  private readonly IEmailSender _emailSender;
  private readonly IBTInviteService _inviteService;
  private readonly IBTProjectService _projectService;

  public RegisterByInviteModel(
    UserManager<BTUser> userManager,
    IUserStore<BTUser> userStore,
    SignInManager<BTUser> signInManager,
    ILogger<RegisterByInviteModel> logger,
    IEmailSender emailSender,
    IBTInviteService inviteService,
    IBTProjectService projectService)
  {
    _userManager = userManager;
    _userStore = userStore;
    _emailStore = GetEmailStore();
    _signInManager = signInManager;
    _logger = logger;
    _emailSender = emailSender;
    _inviteService = inviteService;
    _projectService = projectService;
  }

  /// <summary>
  ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
  ///     directly from your code. This API may change or be removed in future releases.
  /// </summary>
  [BindProperty]
  public InputModel Input { get; set; }

  /// <summary>
  ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
  ///     directly from your code. This API may change or be removed in future releases.
  /// </summary>
  public string ReturnUrl { get; set; }

  /// <summary>
  ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
  ///     directly from your code. This API may change or be removed in future releases.
  /// </summary>
  public IList<AuthenticationScheme> ExternalLogins { get; set; }

  public Invite GuestInvite { get; set; }

  /// <summary>
  ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
  ///     directly from your code. This API may change or be removed in future releases.
  /// </summary>
  public class InputModel
  {
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    public int CompanyId { get; set; }

    public int ProjectId { get; set; }

    public Guid Token { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
  }
  public async Task OnGetAsync(Guid token, string email, int companyId, string returnUrl = null)
  {
    GuestInvite = await _inviteService.GetInviteAsync(token, email, companyId);

    ReturnUrl = returnUrl;
    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
  }

  public async Task<IActionResult> OnPostAsync(string returnUrl = null)
  {
    returnUrl ??= Url.Content("~/Tickets/AllTickets");
    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    if (ModelState.IsValid)
    {
      var user = CreateUser();

      user.FirstName = Input.FirstName;
      user.LastName = Input.LastName;
      user.CompanyId = Input.CompanyId;

      await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
      await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
      var result = await _userManager.CreateAsync(user, Input.Password);

      if (result.Succeeded)
      {
        _logger.LogInformation("User created a new account with password.");

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var emailResult = await _userManager.ConfirmEmailAsync(user, code);

        await _userManager.AddToRoleAsync(user, Roles.Submitter.ToString());

        await _projectService.AddUserToProjectAsync(userId, Input.ProjectId);

        await _emailSender.SendEmailAsync(Input.Email, "WorkFlo Registration Confirmation",
                      $"Congratulations! You are registered with WorkFlo.<br><br>Your credentials are:<br><br>&emsp;&emsp;Username:{Input.Email} <br>&emsp;&emsp;Password: {Input.Password}<br><br>Thank you");

        var inviteAccepted = await _inviteService.AcceptInviteAsync(Input.Token, userId);

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
      }
      foreach (var error in result.Errors)
      {
        ModelState.AddModelError(string.Empty, error.Description);
      }
    }

    // If we got this far, something failed, redisplay form
    return Page();
  }

  private BTUser CreateUser()
  {
    try
    {
      return Activator.CreateInstance<BTUser>();
    }
    catch
    {
      throw new InvalidOperationException($"Can't create an instance of '{nameof(BTUser)}'. " +
          $"Ensure that '{nameof(BTUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
          $"override the register page in /Areas/Identity/Pages/Account/RegisterByInvite.cshtml");
    }
  }

  private IUserEmailStore<BTUser> GetEmailStore()
  {
    if (!_userManager.SupportsUserEmail)
    {
      throw new NotSupportedException("The default UI requires a user store with email support.");
    }
    return (IUserEmailStore<BTUser>)_userStore;
  }
}
