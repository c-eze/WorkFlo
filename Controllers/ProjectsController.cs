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

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
public class ProjectsController : Controller
{
  private readonly IBTRolesService _rolesService;
  private readonly IBTLookupService _lookupService;
  private readonly IBTFileService _fileService;
  private readonly IBTProjectService _projectService;
  private readonly IBTCompanyInfoService _companyInfoService;
  private readonly UserManager<BTUser> _userManager;

  public ProjectsController(IBTRolesService rolesService,
                IBTLookupService lookupService,
                IBTFileService fileService,
                IBTProjectService projectService,
                IBTCompanyInfoService companyInfoService,
                UserManager<BTUser> userManager)
  {
    _rolesService = rolesService;
    _lookupService = lookupService;
    _fileService = fileService;
    _projectService = projectService;
    _companyInfoService = companyInfoService;
    _userManager = userManager;
  }

  public async Task<IActionResult> MyProjects()
  {
    string userId = _userManager.GetUserId(User);

    List<Project> projects = await _projectService.GetUserProjectsAsync(userId);

    return View(projects);
  }

  public async Task<IActionResult> AllProjects()
  {
    List<Project> projects = new();

    int companyId = User.Identity.GetCompanyId().Value;

    if (User.IsInRole(nameof(Roles.Admin)) || User.IsInRole(nameof(Roles.ProjectManager)))
    {
      projects = await _companyInfoService.GetAllProjectsAsync(companyId);
    }
    else
    {
      projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);
    }
    return View(projects);
  }

  public async Task<IActionResult> ArchivedProjects()
  {
    int companyId = User.Identity.GetCompanyId().Value;

    List<Project> projects = await _projectService.GetArchivedProjectsByCompanyAsync(companyId);

    return View(projects);
  }

  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> UnassignedProjects()
  {
    int companyId = User.Identity.GetCompanyId().Value;

    List<Project> projects = new();

    projects = await _projectService.GetUnassignedProjectsAsync(companyId);

    return View(projects);
  }

  [Authorize(Roles = "Admin")]
  [HttpGet]
  public async Task<IActionResult> AssignPM(int projectId)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    AssignPMViewModel model = new();

    model.Project = await _projectService.GetProjectByIdAsync(projectId, companyId);

    model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(Roles.ProjectManager), companyId), "Id", "FullName");

    return View(model);
  }

  [Authorize(Roles = "Admin")]
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> AssignPM(AssignPMViewModel model)
  {
    if (!string.IsNullOrEmpty(model.PMID))
    {
      await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);

      return RedirectToAction(nameof(Details), new { id = model.Project.Id });
    }

    return RedirectToAction(nameof(AssignPM), new { projectId = model.Project.Id });
  }

  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpGet]
  public async Task<IActionResult> AssignMembers(int id)
  {
    ProjectMembersViewModel model = new();

    int companyId = User.Identity.GetCompanyId().Value;

    model.Project = await _projectService.GetProjectByIdAsync(id, companyId);

    List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Developer), companyId);

    List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Submitter), companyId);

    List<BTUser> companyMembers = developers.Concat(submitters).ToList();

    List<string> projectMembers = model.Project.Members.Select(m => m.Id).ToList();

    model.Users = new MultiSelectList(companyMembers, "Id", "FullName", projectMembers);

    return View(model);
  }

  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> AssignMembers(ProjectMembersViewModel model)
  {
    if (model.SelectedUsers != null)
    {
      List<String> memberIds = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id))
                  .Select(m => m.Id).ToList();

      //remove current members
      foreach (string members in memberIds)
      {
        await _projectService.RemoveUserFromProjectAsync(members, model.Project.Id);
      }

      //add selected members
      foreach (string members in model.SelectedUsers)
      {
        await _projectService.AddUserToProjectAsync(members, model.Project.Id);
      }

      return RedirectToAction("Details", new { id = model.Project.Id });

    }
    return RedirectToAction(nameof(AssignMembers), new { id = model.Project.Id });
  }

  // GET: Projects/Details/5
  public async Task<IActionResult> Details(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    int companyId = User.Identity.GetCompanyId().Value;

    Project project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

    if (project == null)
    {
      return NotFound();
    }

    return View(project);
  }

  // GET: Projects/Create
  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> Create()
  {
    int companyId = User.Identity.GetCompanyId().Value;

    //add ViewModel instance "AddProjectWithPMViewModel"
    AddProjectWithPMViewModel model = new();

    //Load SelectLists with data ie. PMList & PriorityList
    model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
    model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");

    return View(model);
  }

  // POST: Projects/Create
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
  {
    if (model != null)
    {
      int companyId = User.Identity.GetCompanyId().Value;

      try
      {
        if (model.Project.ImageFormFile != null)
        {
          model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
          model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
          model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
        }

        model.Project.CompanyId = companyId;

        await _projectService.AddNewProjectAsync(model.Project);

        //Add PM if one was chosen
        if (!string.IsNullOrEmpty(model.PmId))
        {
          await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
        }

        //TODO: Redirect to All Projects
        return RedirectToAction("AllProjects");

      }
      catch (Exception)
      {

        throw;
      }
    }

    return RedirectToAction("Create");
  }

  // GET: Projects/Edit/5
  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> Edit(int? id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    //add ViewModel instance "AddProjectWithPMViewModel"
    AddProjectWithPMViewModel model = new();

    model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

    //Load SelectLists with data ie. PMList & PriorityList
    model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
    model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");

    return View(model);
  }

  // POST: Projects/Edit/5
  // To protect from overposting attacks, enable the specific properties you want to bind to.
  // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
  {
    if (model != null)
    {
      try
      {
        if (model.Project.ImageFormFile != null)
        {
          model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
          model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
          model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
        }

        await _projectService.UpdateProjectAsync(model.Project);

        //Add PM if one was chosen
        if (!string.IsNullOrEmpty(model.PmId))
        {
          await _projectService.AddProjectManagerAsync(model.PmId, model.Project.Id);
        }

        //TODO: Redirect to All Projects
        return RedirectToAction("AllProjects");
      }
      catch (DbUpdateConcurrencyException)
      {
        if (!await ProjectExists(model.Project.Id))
        {
          return NotFound();
        }
        else
        {
          throw;
        }
      }
    }
    return RedirectToAction("Edit");
  }

  // GET: Projects/Archive/5
  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> Archive(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    int companyId = User.Identity.GetCompanyId().Value;
    var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

    if (project == null)
    {
      return NotFound();
    }

    return View(project);
  }

  // POST: Projects/Archive/5
  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost, ActionName("Archive")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> ArchiveConfirmed(int id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    var project = await _projectService.GetProjectByIdAsync(id, companyId);

    await _projectService.ArchiveProjectAsync(project);

    return RedirectToAction(nameof(AllProjects));
  }

  // GET: Projects/Restore/5
  [Authorize(Roles = "Admin, ProjectManager")]
  public async Task<IActionResult> Restore(int? id)
  {
    if (id == null)
    {
      return NotFound();
    }

    int companyId = User.Identity.GetCompanyId().Value;
    var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

    if (project == null)
    {
      return NotFound();
    }

    return View(project);
  }

  // POST: Projects/Restore/5
  [Authorize(Roles = "Admin, ProjectManager")]
  [HttpPost, ActionName("Restore")]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> RestoreConfirmed(int id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    var project = await _projectService.GetProjectByIdAsync(id, companyId);

    await _projectService.RestoreProjectAsync(project);

    return RedirectToAction(nameof(ArchivedProjects));
  }

  private async Task<bool> ProjectExists(int id)
  {
    int companyId = User.Identity.GetCompanyId().Value;

    return (await _projectService.GetAllProjectsByCompanyAsync(companyId)).Any(p => p.Id == id);
  }
}
