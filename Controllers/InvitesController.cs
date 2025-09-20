using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Extensions;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Security;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Controllers;

public class InvitesController : Controller
{
  private readonly ApplicationDbContext _context;
  private readonly IBTInviteService _inviteService;
  private readonly IBTProjectService _projectService;
  private readonly IEmailSender _emailService;
  private readonly IDataProtector _protector;

  public InvitesController(ApplicationDbContext context,
               IDataProtectionProvider dataProtectionProvider,
               DataProtectionPurposeStrings dataProtectionPurposeStrings,
               IBTInviteService inviteService,
               IEmailSender emailService,
               IBTProjectService projectService)
  {
    _context = context;
    _protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeStrings.InviteRouteValue);
    _inviteService = inviteService;
    _emailService = emailService;
    _projectService = projectService;
  }

  // GET: Invites
  public async Task<IActionResult> Index()
  {
    int companyId = User.Identity.GetCompanyId().Value;

    List<Invite> invites = await _inviteService.GetAllInvitesByCompanyAsync(companyId);

    return View(invites);
  }

  public async Task<IActionResult> Resend(int id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    Invite invite = await _inviteService.GetInviteAsync(id, companyId);

    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", invite.CompanyId);
    ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);

    return View(invite);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Resend([Bind("Id,InviteDate,InviteDate,CompanyId,ProjectId,InviterId,InviteeEmail,InviteeFirstName,InviteeLastName,IsValid")] Invite invite)
  {
    if (ModelState.IsValid)
    {
      invite.EncryptedToken = _protector.Protect(invite.CompanyToken.ToString());
      invite.EncryptedEmail = _protector.Protect(invite.InviteeEmail);
      invite.EncryptedCompanyId = _protector.Protect(invite.CompanyId.ToString());

      var returnUrl = Url.Action(
        "ProcessInvite",
        "Invites",
        new { token = invite.EncryptedToken, email = invite.EncryptedEmail, companyId = invite.EncryptedCompanyId },
        protocol: Request.Scheme);

      await _inviteService.UpdateInviteAsync(invite);
      await _emailService.SendEmailAsync(invite.InviteeEmail, $"Workflo Invitation", $"Hello {invite.InviteeFirstName} {invite.InviteeLastName},<br><br>Click the following link to join our team on Workflo: <a href='{returnUrl}'>Click Here</a>.<br><br>If clicking the link above does not take you to the activation screen, paste the following link into your browser's URL:<br>{returnUrl}<br><br>Thank you,<br>Admin");
      return RedirectToAction("Index", "Invites");
    }
    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", invite.CompanyId);
    ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
    return View(invite);
  }

  public async Task<IActionResult> ProcessInvite(string token, string email, string companyId)
  {
    var inviteToken = Guid.Parse(_protector.Unprotect(token));
    var inviteMail = _protector.Unprotect(email);
    var inviteCompanyId = Convert.ToInt32(_protector.Unprotect(companyId));

    var validationResult = await _inviteService.ValidateInviteCodeAsync(inviteToken);
    if (validationResult)
    {
      return RedirectToPage("/Account/RegisterByInvite", new { area = "Identity", token = inviteToken, email = inviteMail, companyId = inviteCompanyId });
    }

    return View("/Account/Login", new { area = "Identity" });
  }

  // GET: Invites/Archive/5
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Archive(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    int companyId = User.Identity.GetCompanyId().Value;

    Invite invite = await _inviteService.GetInviteAsync(id.Value, companyId);

    if (invite == null)
    {
      return NotFound();
    }

    return View(invite);
  }

  // POST: Invites/Archive/5
  [Authorize(Roles = "Admin")]
  [HttpPost, ActionName("Archive")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> ArchiveConfirmed(int id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    Invite invite = await _inviteService.GetInviteAsync(id, companyId);
    invite.Archived = true;
    await _inviteService.UpdateInviteAsync(invite);

    return RedirectToAction(nameof(Index));
  }

  // GET: Invites/Details/5
  public async Task<IActionResult> Details(int? id)
  {
    if (id == null || _context.Invites == null)
    {
      return NotFound();
    }

    var invite = await _context.Invites
        .Include(i => i.Company)
        .Include(i => i.Invitee)
        .Include(i => i.Project)
        .FirstOrDefaultAsync(m => m.Id == id);
    if (invite == null)
    {
      return NotFound();
    }

    return View(invite);
  }

  // GET: Invites/Create
  public async Task<IActionResult> Create()
  {
    int companyId = User.Identity.GetCompanyId().Value;
    var projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", companyId);
    ViewData["ProjectId"] = new SelectList(projects, "Id", "Name");

    return View();
  }

  // POST: Invites/Create
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create([Bind("InviteDate,CompanyId,ProjectId,InviterId,InviteeEmail,InviteeFirstName,InviteeLastName,IsValid")] Invite invite)
  {
    if (ModelState.IsValid)
    {
      invite.CompanyToken = Guid.NewGuid();
      invite.EncryptedToken = _protector.Protect(invite.CompanyToken.ToString());
      invite.EncryptedEmail = _protector.Protect(invite.InviteeEmail);
      invite.EncryptedCompanyId = _protector.Protect(invite.CompanyId.ToString());

      var returnUrl = Url.Action(
          "ProcessInvite",
          "Invites",
          new { token = invite.EncryptedToken, email = invite.EncryptedEmail, companyId = invite.EncryptedCompanyId },
          protocol: Request.Scheme);

      await _inviteService.AddNewInviteAsync(invite);
      await _emailService.SendEmailAsync(invite.InviteeEmail, $"Workflo Invitation", $"Hello {invite.InviteeFirstName} {invite.InviteeLastName},<br><br>Click the following link to join our team on Workflo: <a href='{returnUrl}'>Click Here</a>.<br><br>If clicking the link above does not take you to the activation screen, paste the following link into your browser's URL:<br><br>{returnUrl}<br><br>Thank you,<br>Admin");
      return RedirectToAction("Index", "Invites");
    }
    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", invite.CompanyId);
    ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
    return View(invite);
  }

  // GET: Invites/Edit/5
  public async Task<IActionResult> Edit(int? id)
  {
    if (id == null || _context.Invites == null)
    {
      return NotFound();
    }

    var invite = await _context.Invites.FindAsync(id);
    if (invite == null)
    {
      return NotFound();
    }
    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", invite.CompanyId);
    ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
    ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
    return View(invite);
  }

  // POST: Invites/Edit/5
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(int id, [Bind("Id,InviteDate,JoinDate,CompanyToken,CompanyId,ProjectId,InviterId,InviteeId,InviteeEmail,InviteeFirstName,InviteeLastName,IsValid")] Invite invite)
  {
    if (id != invite.Id)
    {
      return NotFound();
    }

    if (ModelState.IsValid)
    {
      try
      {
        _context.Update(invite);
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!InviteExists(invite.Id))
        {
          return NotFound();
        }
        else
        {
          throw;
        }
      }
      return RedirectToAction(nameof(Index));
    }
    ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", invite.CompanyId);
    ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
    ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
    return View(invite);
  }

  // GET: Invites/Delete/5
  public async Task<IActionResult> Delete(int? id)
  {
    if (id == null || _context.Invites == null)
    {
      return NotFound();
    }

    var invite = await _context.Invites
        .Include(i => i.Company)
        .Include(i => i.Invitee)
        .Include(i => i.Project)
        .FirstOrDefaultAsync(m => m.Id == id);
    if (invite == null)
    {
      return NotFound();
    }

    return View(invite);
  }

  // POST: Invites/Delete/5
  [HttpPost, ActionName("Delete")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> DeleteConfirmed(int id)
  {
    if (_context.Invites == null)
    {
      return Problem("Entity set 'ApplicationDbContext.Invites'  is null.");
    }
    var invite = await _context.Invites.FindAsync(id);
    if (invite != null)
    {
      _context.Invites.Remove(invite);
    }

    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Index));
  }

  private bool InviteExists(int id)
  {
    return (_context.Invites?.Any(e => e.Id == id)).GetValueOrDefault();
  }
}

