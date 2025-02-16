using AspnetCoreMvcFull.Extensions;
using AspnetCoreMvcFull.Models.ViewModels;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; 

namespace AspnetCoreMvcFull.Controllers;

[Authorize]
public class UserRolesController : Controller
{
  private readonly IBTRolesService _rolesService;
  private readonly IBTCompanyInfoService _companyInfoService;

  public UserRolesController(IBTRolesService rolesService,
                             IBTCompanyInfoService companyInfoService)
  {
    _rolesService = rolesService;
    _companyInfoService = companyInfoService;
  }

  [HttpGet]
  public async Task<IActionResult> ManageUserRoles()
  {
    //Add an instance of the ViewModel as a List (model)
    List<ManageUserRolesViewModel> model = new List<ManageUserRolesViewModel>();

    //Get CompanyId
    int companyId = User.Identity.GetCompanyId().Value;

    //Get all company users
    List<BTUser> users = await _companyInfoService.GetAllMembersAsync(companyId);

    //Loop over the user to populate the ViewModel
    // - instantiate ViewModel
    // - use _rolesService
    // - Create multi-selects
    foreach (BTUser user in users)
    {
      ManageUserRolesViewModel viewModel = new ManageUserRolesViewModel();
      viewModel.BTUser = user;
      IEnumerable<string> selected = await _rolesService.GetUserRolesAsync(user);
      viewModel.Roles = new MultiSelectList(await _rolesService.GetRolesAsync(), "Name", "Name", selected);

      model.Add(viewModel);
    }

    //Return the model to the View
    return View(model);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel member)
  {
    //get the company id
    int companyId = User.Identity.GetCompanyId().Value;

    //instantiate the user
    BTUser btUser = (await _companyInfoService.GetAllMembersAsync(companyId)).FirstOrDefault(u => u.Id == member.BTUser.Id);

    //get the roles for the user
    IEnumerable<string> roles = await _rolesService.GetUserRolesAsync(btUser);

    //grab the selected role
    string userRole = member.SelectedRoles.FirstOrDefault();

    if (!string.IsNullOrEmpty(userRole))
    {
      //remove user from their roles
      if (await _rolesService.RemoveUserFromRolesAsync(btUser, roles))
      {
        //add user to the new role
        await _rolesService.AddUserToRoleAsync(btUser, userRole);
      }
    }

    //navigate back to the view
    return RedirectToAction(nameof(ManageUserRoles));
  }
}
