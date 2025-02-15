using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using AspnetCoreMvcFull.Extensions;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models.Enums;

namespace AspnetCoreMvcFull.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
  private readonly IBTCompanyInfoService _companyInfoService;
  private readonly IBTProjectService _projectService;
  private readonly IBTTicketService _ticketService;
  private readonly IBTRolesService _rolesService;

  public HomeController(ILogger<HomeController> logger, IBTCompanyInfoService companyInfoService, IBTProjectService projectService, IBTTicketService ticketService, IBTRolesService rolesService)
  {
    _logger = logger;
    _companyInfoService = companyInfoService;
    _projectService = projectService;
    _ticketService = ticketService;
    _rolesService = rolesService;
  }

  public IActionResult Index()
    {
        return View();
    }
  public async Task<IActionResult> Profile()
  {
    DashboardViewModel model = new();
    int companyId = User.Identity.GetCompanyId().Value;

    model.Company = await _companyInfoService.GetCompanyInfoByIdAsync(companyId);
    model.Projects = (await _companyInfoService.GetAllProjectsAsync(companyId))
              .Where(p => p.Archived == false)
              .ToList();

    model.Tickets = model.Projects.SelectMany(p => p.Tickets)
              .Where(t => t.Archived == false)
              .ToList();

    model.Members = model.Company.Members.ToList();

    return View(model);
  }

  public async Task<IActionResult> Dashboard()
  {
    DashboardViewModel model = new();
    int companyId = User.Identity.GetCompanyId().Value;

    model.Company = await _companyInfoService.GetCompanyInfoByIdAsync(companyId);
    model.Projects = (await _companyInfoService.GetAllProjectsAsync(companyId))
                        .Where(p => p.Archived == false)
                        .ToList();

    model.Tickets = model.Projects.SelectMany(p => p.Tickets)
                        .Where(t => t.Archived == false)
                        .ToList();

    model.Members = model.Company.Members.ToList();

    return View(model);
  }

  [HttpPost]
  public async Task<JsonResult> CjsTicketsDev()
  {
    int companyId = User.Identity.GetCompanyId().Value;

    List<Ticket> tickets = (await _projectService.GetAllProjectsByCompanyAsync(companyId))
                      .SelectMany(p => p.Tickets)
                      .ToList();

    List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Developer), companyId);

    List<object> chartData = new();

    foreach (BTUser dev in developers)
    {
      chartData.Add(new object[] { dev.FullName, tickets.Where(t => t.DeveloperUser == dev).Count() });
    }

    return Json(chartData);
  }

  [HttpPost]
  public async Task<JsonResult> CjsProjectTickets()
  {
    int companyId = User.Identity.GetCompanyId().Value;

    List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

    List<object> chartData = new();

    foreach (Project prj in projects)
    {
      chartData.Add(new object[] { prj.Name, prj.Tickets.Count() });
    }

    return Json(chartData);
  }

  public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
