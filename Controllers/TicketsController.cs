using AspnetCoreMvcFull.Extensions;
using AspnetCoreMvcFull.Models.Enums;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using System.Drawing.Printing;

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
public class TicketsController : Controller
{
  private readonly UserManager<BTUser> _userManager;
  private readonly IBTProjectService _projectService;
  private readonly IBTLookupService _lookupService;
  private readonly IBTTicketService _ticketService;
  private readonly IBTFileService _fileService;
  private readonly IBTTicketHistoryService _historyService;
  private readonly IBTNotificationService _notificationService;

  public TicketsController(UserManager<BTUser> userManager,
               IBTProjectService projectService,
               IBTLookupService lookupService,
               IBTTicketService ticketService,
               IBTFileService fileService,
               IBTTicketHistoryService historyService,
               IBTNotificationService notificationService)
  {
    _userManager = userManager;
    _projectService = projectService;
    _lookupService = lookupService;
    _ticketService = ticketService;
    _fileService = fileService;
    _historyService = historyService;
    _notificationService = notificationService;
  }

  public async Task<IActionResult> MyTickets()
  {
    BTUser bTUser = await _userManager.GetUserAsync(User);

    List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(bTUser.Id, bTUser.CompanyId);

    return View(tickets);
  }

  public async Task<IActionResult> AllTickets(int? page, string currentFilter, string searchString)
  {
    if (searchString != null)
    {
      page = 1;
    }
    else
    {
      searchString = currentFilter;
    }

    ViewData["CurrentFilter"] = searchString;

    int companyId = User.Identity.GetCompanyId().Value;

    List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);

    if (!String.IsNullOrEmpty(searchString))
    {
      tickets = tickets.FindAll(t => t.Title.ToLower().Contains(searchString) || t.Project.Name.ToLower().Contains(searchString));
    }

    var pageNumber = page ?? 1;
    var pageSize = 10; // number of items per page

    return View(tickets.Where(t => t.Archived == false).ToPagedList(pageNumber, pageSize)); 
  }

  public async Task<IActionResult> ArchivedTickets(int? page, string currentFilter, string searchString)
  {
    if (searchString != null)
    {
      page = 1;
    }
    else
    {
      searchString = currentFilter;
    }

    ViewData["CurrentFilter"] = searchString;

    int companyId = User.Identity.GetCompanyId().Value;

    List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);

    if (!String.IsNullOrEmpty(searchString))
    {
      tickets = tickets.FindAll(t => t.Title.ToLower().Contains(searchString) || t.Project.Name.ToLower().Contains(searchString));
    }

    var pageNumber = page ?? 1;
    var pageSize = 10; // number of items per page

    return View(tickets.ToPagedList(pageNumber, pageSize));
  }

  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> UnassignedTickets(int? page, string currentFilter, string searchString)
  {
    if (searchString != null)
    {
      page = 1;
    }
    else
    {
      searchString = currentFilter;
    }

    ViewData["CurrentFilter"] = searchString;

    int companyId = User.Identity.GetCompanyId().Value;
    string btUserId = _userManager.GetUserId(User);

    List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);

    if (!String.IsNullOrEmpty(searchString))
    {
      tickets = tickets.FindAll(t => t.Title.ToLower().Contains(searchString) || t.Project.Name.ToLower().Contains(searchString));
    }

    var pageNumber = page ?? 1;
    var pageSize = 10; // number of items per page

    if (User.IsInRole(nameof(Roles.Admin)))
    {
      return View(tickets.Where(t => t.Archived == false).ToPagedList(pageNumber, pageSize));
    }
    else
    {
      List<Ticket> pmTickets = new();

      foreach (Ticket ticket in tickets)
      {
        if (await _projectService.IsAssignedProjectManagerAsync(btUserId, ticket.ProjectId))
        {
          pmTickets.Add(ticket);
        }
      }
      return View(pmTickets.Where(t => t.Archived == false).ToPagedList(pageNumber, pageSize));
    }
  }

  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpGet]
  public async Task<IActionResult> AssignDeveloper(int id)
  {
    AssignDeveloperViewModel model = new();

    model.Ticket = await _ticketService.GetTicketByIdAsync(id);
    model.Developers = new SelectList(await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, nameof(Roles.Developer)), "Id", "FullName");

    return View(model);
  }

  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost]
  public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
  {
    if (model.DeveloperId != null)
    {
      BTUser btUser = await _userManager.GetUserAsync(User);

      //old ticket
      Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);

      try
      {
        await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);

      }
      catch (Exception)
      {

        throw;
      }

      //new ticket
      Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
      await _historyService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);

      //ticket notification
      Notification newNotification = new()
      {
        TicketId = newTicket.Id,
        Title = $"{btUser.FullName} assigned a ticket to you",
        Message = $"{newTicket.Title}",
        Created = DateTimeOffset.Now,
        RecipientId = newTicket.DeveloperUserId,
        SenderId = btUser.Id
      };

      await _notificationService.AddNotificationAsync(newNotification);

      return RedirectToAction(nameof(Details), new { id = model.Ticket.Id });
    }

    return RedirectToAction(nameof(AssignDeveloper), new { id = model.Ticket.Id });
  }

  // GET: Tickets/Details/5
  public async Task<IActionResult> Details(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

    if (ticket == null)
    {
      return NotFound();
    }

    return View(ticket);
  }

  // GET: Tickets/Create
  public async Task<IActionResult> Create()
  {
    BTUser btUser = await _userManager.GetUserAsync(User);

    int companyId = User.Identity.GetCompanyId().Value;

    if (User.IsInRole(nameof(Roles.Admin)))
    {
      ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(companyId), "Id", "Name");
    }
    else
    {
      ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
    }

    ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
    ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
    return View();
  }

  // POST: Tickets/Create
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
  {
    BTUser btUser = await _userManager.GetUserAsync(User);

    if (ModelState.IsValid)
    {
      try
      {
        ticket.Created = DateTimeOffset.Now;
        ticket.OwnerUserId = btUser.Id;

        ticket.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync(nameof(BTTicketStatus.New))).Value;

        await _ticketService.AddNewTicketAsync(ticket);

        //Ticket History
        Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
        await _historyService.AddHistoryAsync(null, newTicket, btUser.Id);
      }
      catch (Exception)
      {

        throw;
      }
      return RedirectToAction("AllTickets", "Tickets");
    }


    if (User.IsInRole(nameof(Roles.Admin)))
    {
      ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(btUser.CompanyId), "Id", "Name");
    }
    else
    {
      ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
    }

    ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
    ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
    return View(ticket);
  }

  // GET: Tickets/Edit/5
  public async Task<IActionResult> Edit(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

    if (ticket == null)
    {
      return NotFound();
    }

    ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
    ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
    ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

    return View(ticket);
  }

  // POST: Tickets/Edit/5
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
  {
    if (id != ticket.Id)
    {
      return NotFound();
    }

    if (ModelState.IsValid)
    {
      BTUser btUser = await _userManager.GetUserAsync(User);
      Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

      try
      {
        ticket.Updated = DateTimeOffset.Now;
        await _ticketService.UpdateTicketAsync(ticket);
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!await TicketExists(ticket.Id))
        {
          return NotFound();
        }
        else
        {
          throw;
        }
      }

      Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
      await _historyService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);

      return RedirectToAction("AllTickets");
    }

    ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
    ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
    ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

    return View(ticket);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> AddTicketComment([Bind("Id,TicketId,Comment,UserId")] TicketComment ticketComment)
  {
    if (ModelState.IsValid)
    {
      BTUser btUser = await _userManager.GetUserAsync(User);
      try
      {
        //ticketComment.UserId = _userManager.GetUserId(User);
        ticketComment.Created = DateTimeOffset.Now;

        await _ticketService.AddTicketCommentAsync(ticketComment);

        //Add history
        await _historyService.AddHistoryAsync(ticketComment.TicketId, nameof(TicketComment), ticketComment.UserId);

        //ticket notification
        Ticket ticket = await _ticketService.GetTicketByIdAsync(ticketComment.TicketId);

        if (ticket.DeveloperUserId is not null)
        {
          Notification newNotification = new()
          {
            TicketId = ticketComment.TicketId,
            Title = $"{btUser.FullName} added a comment",
            Message = $"{ticket.Title}",
            Created = DateTimeOffset.Now,
            RecipientId = ticket.DeveloperUserId,
            SenderId = btUser.Id
          };

          await _notificationService.AddNotificationAsync(newNotification);
        }
      }
      catch (Exception)
      {

        throw;
      }
    }

    return RedirectToAction("Details", new { id = ticketComment.TicketId });
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
  {
    string statusMessage;

    if (ModelState.IsValid && ticketAttachment.FormFile != null)
    {
      BTUser btUser = await _userManager.GetUserAsync(User);
      try
      {
        ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
        ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
        ticketAttachment.FileContentType = ticketAttachment.FormFile.ContentType;

        ticketAttachment.Created = DateTimeOffset.Now;
        ticketAttachment.UserId = _userManager.GetUserId(User);

        await _ticketService.AddTicketAttachmentAsync(ticketAttachment);

        await _historyService.AddHistoryAsync(ticketAttachment.TicketId, nameof(TicketAttachment), ticketAttachment.UserId);

        //ticket notification
        Ticket ticket = await _ticketService.GetTicketByIdAsync(ticketAttachment.TicketId);

        Notification newNotification = new()
        {
          TicketId = ticket.Id,
          Title = $"{btUser.FullName} added an attachment",
          Message = $"{ticket.Title}",
          Created = DateTimeOffset.Now,
          RecipientId = ticket.DeveloperUserId,
          SenderId = btUser.Id
        };

        await _notificationService.AddNotificationAsync(newNotification);
      }
      catch (Exception)
      {

        throw;
      }

      statusMessage = "Success: New attachment added to Ticket.";
    }
    else
    {
      statusMessage = "Error: Invalid data.";

    }

    return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
  }

  public async Task<IActionResult> ShowFile(int id)
  {
    TicketAttachment ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
    string fileName = ticketAttachment.FileName;
    byte[] fileData = ticketAttachment.FileData;
    string ext = Path.GetExtension(fileName).Replace(".", "");

    Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
    return File(fileData, $"application/{ext}");
  }

  // GET: Tickets/Archive/5
  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> Archive(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

    if (ticket == null)
    {
      return NotFound();
    }

    return View(ticket);
  }

  // POST: Tickets/Archive/5
  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost, ActionName("Archive")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> ArchiveConfirmed(int id)
  {
    Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
    ticket.Archived = true;
    await _ticketService.UpdateTicketAsync(ticket);

    return RedirectToAction(nameof(AllTickets));
  }

  // GET: Tickets/Restore/5
  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> Restore(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

    if (ticket == null)
    {
      return NotFound();
    }

    return View(ticket);
  }

  // POST: Tickets/Restore/5
  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost, ActionName("Restore")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> RestoreConfirmed(int id)
  {
    Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
    ticket.Archived = false;
    await _ticketService.UpdateTicketAsync(ticket);

    return RedirectToAction(nameof(Index));
  }

  private async Task<bool> TicketExists(int id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    return (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Any(t => t.Id == id);
  }
}
