using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; 

namespace AspnetCoreMvcFull.Controllers;

public class NotificationsController : Controller
{
  private readonly ApplicationDbContext _context;
  private readonly IBTNotificationService _notificationService;
  private readonly UserManager<BTUser> _userManager;

  public NotificationsController(ApplicationDbContext context,
                                     IBTNotificationService notificationService,
                                     UserManager<BTUser> userManager)
  {
    _context = context;
    _notificationService = notificationService;
    _userManager = userManager;
  }

  public async Task<IActionResult> Inbox()
  {
    //BTUser btUser = await _userManager.GetUserAsync(User); 

    string userId = _userManager.GetUserId(User);

    List<Notification> notifications = new List<Notification>();

    try
    {
      notifications = await _notificationService.GetReceivedNotificationsAsync(userId);
    }
    catch (Exception)
    {

      throw;
    }

    return View(notifications.Where(n => n.Archived == false));
  }

  public async Task<IActionResult> Note(int id, int ticketId)
  {
    await _notificationService.UpdateNotificationViewByIdAsync(id);

    return RedirectToAction("Details", "Tickets", new { id = ticketId });
  }

  // GET: Notifications
  public async Task<IActionResult> Index()
  {
    var applicationDbContext = _context.Notifications.Include(n => n.Recipient).Include(n => n.Sender).Include(n => n.Ticket);
    return View(await applicationDbContext.ToListAsync());
  }

  // GET: Notifications/Details/5
  public async Task<IActionResult> Details(int? id)
  {
    if (id == null || _context.Notifications == null)
    {
      return NotFound();
    }

    var notification = await _context.Notifications
        .Include(n => n.Recipient)
        .Include(n => n.Sender)
        .Include(n => n.Ticket)
        .FirstOrDefaultAsync(m => m.Id == id);
    if (notification == null)
    {
      return NotFound();
    }

    return View(notification);
  }

  // GET: Notifications/Create
  public IActionResult Create()
  {
    ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id");
    ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id");
    ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description");
    return View();
  }

  // POST: Notifications/Create
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create([Bind("Id,TicketId,Title,Message,Created,RecipientId,SenderId,Viewed")] Notification notification)
  {
    if (ModelState.IsValid)
    {
      _context.Add(notification);
      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }
    ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id", notification.RecipientId);
    ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id", notification.SenderId);
    ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", notification.TicketId);
    return View(notification);
  }

  // GET: Notifications/Edit/5
  public async Task<IActionResult> Edit(int? id)
  {
    if (id == null || _context.Notifications == null)
    {
      return NotFound();
    }

    var notification = await _context.Notifications.FindAsync(id);
    if (notification == null)
    {
      return NotFound();
    }
    ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id", notification.RecipientId);
    ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id", notification.SenderId);
    ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", notification.TicketId);
    return View(notification);
  }

  // POST: Notifications/Edit/5
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(int id, [Bind("Id,TicketId,Title,Message,Created,RecipientId,SenderId,Viewed")] Notification notification)
  {
    if (id != notification.Id)
    {
      return NotFound();
    }

    if (ModelState.IsValid)
    {
      try
      {
        _context.Update(notification);
        await _context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!NotificationExists(notification.Id))
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
    ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id", notification.RecipientId);
    ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id", notification.SenderId);
    ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", notification.TicketId);
    return View(notification);
  }

  // GET: Notifications/Delete/5
  public async Task<IActionResult> Archive(int? id)
  {
    if (id == null || _context.Notifications == null)
    {
      return NotFound();
    }

    var notification = await _context.Notifications
        .Include(n => n.Recipient)
        .Include(n => n.Sender)
        .Include(n => n.Ticket)
        .FirstOrDefaultAsync(m => m.Id == id);
    if (notification == null)
    {
      return NotFound();
    }

    return View(notification);
  }

  // POST: Notifications/Archive/5
  [HttpPost, ActionName("Archive")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> ArchiveConfirmed(int id)
  {
    Notification note = await _notificationService.GetNotificationByIdAsync(id);
    note.Archived = true;
    await _notificationService.UpdateNotificationAsync(note);

    return RedirectToAction(nameof(Inbox));
  }

  private bool NotificationExists(int id)
  {
    return (_context.Notifications?.Any(e => e.Id == id)).GetValueOrDefault();
  }
}
