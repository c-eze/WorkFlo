using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using AspnetCoreMvcFull.Extensions;
using AspnetCoreMvcFull.Models.ViewModels;

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
