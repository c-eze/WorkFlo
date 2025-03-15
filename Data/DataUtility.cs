using AspnetCoreMvcFull.Models.Enums;
using AspnetCoreMvcFull.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static System.Collections.Specialized.BitVector32;
using System.Reflection.Metadata;
using AspnetCoreMvcFull.Services.Interfaces;
using AspnetCoreMvcFull.Services;  

namespace AspnetCoreMvcFull.Data;

public static class DataUtility
{
  //Company Ids
  private static int company1Id;
  private static int company2Id;
  private static int company3Id;
  private static int company4Id;
  private static int company5Id;

  public static string GetConnectionString(IConfiguration configuration)
  {
    //The default connection string will come from appSettings like usual
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    //It will be automatically overwritten if we are running on Heroku
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    return string.IsNullOrEmpty(databaseUrl) ? connectionString : BuildConnectionString(databaseUrl);
  }

  public static string BuildConnectionString(string databaseUrl)
  {
    //Provides an object representation of a uniform resource identifier (URI) and easy access to the parts of the URI.
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    //Provides a simple way to create and manage the contents of connection strings used by the NpgsqlConnection class.
    var builder = new NpgsqlConnectionStringBuilder
    {
      Host = databaseUri.Host,
      Port = databaseUri.Port,
      Username = userInfo[0],
      Password = userInfo[1],
      Database = databaseUri.LocalPath.TrimStart('/'),
      SslMode = SslMode.Prefer,
      TrustServerCertificate = true
    };
    return builder.ToString();
  }

  public static async Task ManageDataAsync(IHost host)
  {
    using var svcScope = host.Services.CreateScope();
    var svcProvider = svcScope.ServiceProvider;
    //Service: An instance of RoleManager
    var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
    //Service: An instance of RoleManager
    var roleManagerSvc = svcProvider.GetRequiredService<RoleManager<IdentityRole>>();
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    //Service: An instance of the UserManager
    var userManagerSvc = svcProvider.GetRequiredService<UserManager<BTUser>>();
    //Migration: This is the programmatic equivalent to Update-Database
    await dbContextSvc.Database.MigrateAsync();


    //Custom  Bug Tracker Seed Methods
    await SeedRolesAsync(roleManagerSvc);
    await SeedDefaultCompaniesAsync(dbContextSvc);
    await SeedDefaultUsersAsync(userManagerSvc);
    await SeedDemoUsersAsync(userManagerSvc);
    await SeedDefaultTicketTypeAsync(dbContextSvc);
    await SeedDefaultTicketStatusAsync(dbContextSvc);
    await SeedDefaultTicketPriorityAsync(dbContextSvc);
    await SeedDefaultProjectPriorityAsync(dbContextSvc);
    await SeedDefautProjectsAsync(dbContextSvc);
    await SeedDefautTicketsAsync(dbContextSvc);
    await SeedDefaultProjectMembersAsync(dbContextSvc);
  }


  public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
  {
    //Seed Roles
    await roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
    await roleManager.CreateAsync(new IdentityRole(Roles.ProjectManager.ToString()));
    await roleManager.CreateAsync(new IdentityRole(Roles.Developer.ToString()));
    await roleManager.CreateAsync(new IdentityRole(Roles.Submitter.ToString()));
    await roleManager.CreateAsync(new IdentityRole(Roles.DemoUser.ToString()));
  }

  public static async Task SeedDefaultCompaniesAsync(ApplicationDbContext context)
  {
    try
    {
      IList<Company> defaultcompanies = new List<Company>() {
                  new Company() { Name = "Company1", Description="This is default Company 1" },
                  new Company() { Name = "Company2", Description="This is default Company 2" },
                  new Company() { Name = "Company3", Description="This is default Company 3" },
                  new Company() { Name = "Company4", Description="This is default Company 4" },
                  new Company() { Name = "Company5", Description="This is default Company 5" }
              };

      var dbCompanies = context.Companies.Select(c => c.Name).ToList();
      await context.Companies.AddRangeAsync(defaultcompanies.Where(c => !dbCompanies.Contains(c.Name)));
      await context.SaveChangesAsync();

      //Get company Ids
      company1Id = context.Companies.FirstOrDefault(p => p.Name == "Company1").Id;
      company2Id = context.Companies.FirstOrDefault(p => p.Name == "Company2").Id;
      company3Id = context.Companies.FirstOrDefault(p => p.Name == "Company3").Id;
      company4Id = context.Companies.FirstOrDefault(p => p.Name == "Company4").Id;
      company5Id = context.Companies.FirstOrDefault(p => p.Name == "Company5").Id;
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Companies.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }

  public static async Task SeedDefaultProjectPriorityAsync(ApplicationDbContext context)
  {
    try
    {
      IList<Models.ProjectPriority> projectPriorities = new List<ProjectPriority>() {
                                                  new ProjectPriority() { Name = BTProjectPriority.Low.ToString() },
                                                  new ProjectPriority() { Name = BTProjectPriority.Medium.ToString() },
                                                  new ProjectPriority() { Name = BTProjectPriority.High.ToString() },
                                                  new ProjectPriority() { Name = BTProjectPriority.Urgent.ToString() },
              };

      var dbProjectPriorities = context.ProjectPriorities.Select(c => c.Name).ToList();
      await context.ProjectPriorities.AddRangeAsync(projectPriorities.Where(c => !dbProjectPriorities.Contains(c.Name)));
      await context.SaveChangesAsync();

    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Project Priorities.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }

  public static async Task SeedDefautProjectsAsync(ApplicationDbContext context)
  {

    //Get project priority Ids
    int priorityLow = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.Low.ToString()).Id;
    int priorityMedium = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.Medium.ToString()).Id;
    int priorityHigh = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.High.ToString()).Id;
    int priorityUrgent = context.ProjectPriorities.FirstOrDefault(p => p.Name == BTProjectPriority.Urgent.ToString()).Id;

    try
    {
      IList<Project> projects = new List<Project>() {
                   new Project()
                   {
                       CompanyId = company1Id,
                       Name = "Build a Personal Porfolio Website",
                       Description="Single page html, css & javascript page.  Serves as a landing page for candidates and contains a bio and links to all applications and challenges." ,
                       StartDate = new DateTime(2021,8,20),
                       EndDate = new DateTime(2021,8,20).AddMonths(1),
                       ProjectPriorityId = priorityLow
                   },
                   new Project()
                   {
                       CompanyId = company1Id,
                       Name = "Build a Supplemental Blog Web Application",
                       Description="Candidate's custom built web application using .Net Core with MVC, a postgres database and hosted in a Railway container.  The app is designed for the candidate to create, update and maintain a live blog site.",
                       StartDate = new DateTime(2021,8,20),
                       EndDate = new DateTime(2021,8,20).AddMonths(4),
                       ProjectPriorityId = priorityMedium
                   },
                   new Project()
                   {
                       CompanyId = company1Id,
                       Name = "Build an Issue Tracking Web Application",
                       Description="A custom designed .Net Core application with postgres database. The application is a multi-tennent application designed to track issue tickets' progress. Implemented with ASP.NET Identity and user roles. Tickets are maintained in projects which are maintained by users in the role of project manager. Each project has a team and team members.",
                       StartDate = new DateTime(2021,8,20),
                       EndDate = new DateTime(2021,8,20).AddMonths(6),
                       ProjectPriorityId = priorityHigh
                   },
                   new Project()
                   {
                       CompanyId = company2Id,
                       Name = "Build an Address Book Web Application",
                       Description="A custom designed .Net Core application with postgres database. This is an application to serve as a rolodex of contacts for a given user..",
                       StartDate = new DateTime(2021,8,20),
                       EndDate = new DateTime(2021,8,20).AddMonths(2),
                       ProjectPriorityId = priorityLow
                   },
                  new Project()
                   {
                       CompanyId = company1Id,
                       Name = "Build a Movie Information Web Application",
                       Description="A custom designed .Net Core application with postgres database.  An API based application allows users to input and import movie posters and details including cast and crew information.",
                       StartDate = new DateTime(2021,8,20),
                       EndDate = new DateTime(2021,8,20).AddMonths(3),
                       ProjectPriorityId = priorityHigh
                   }
              };

      var dbProjects = context.Projects.Select(c => c.Name).ToList();
      await context.Projects.AddRangeAsync(projects.Where(c => !dbProjects.Contains(c.Name)));
      await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Projects.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }

  public static async Task SeedDefaultProjectMembersAsync(ApplicationDbContext context)
  {
    //Get project Ids
    int portfolioId = context.Projects.FirstOrDefault(p => p.Name == "Build a Personal Porfolio Website").Id;
    int blogId = context.Projects.FirstOrDefault(p => p.Name == "Build a Supplemental Blog Web Application").Id;
    int bugtrackerId = context.Projects.FirstOrDefault(p => p.Name == "Build an Issue Tracking Web Application").Id;
    int movieId = context.Projects.FirstOrDefault(p => p.Name == "Build a Movie Information Web Application").Id;

    //Get user Ids
    BTUser pm1 = await context.Users.FirstOrDefaultAsync(u => u.UserName == "ProjectManager1@bugtracker.com");

    Project bugTracker = await context.Projects
      .Include(p => p.Members)
      .FirstOrDefaultAsync(p => p.Id == bugtrackerId);

    if(!bugTracker.Members.Any(m => m.Id == pm1.Id)) {
      try
      {
        bugTracker.Members.Add(pm1);
        await context.SaveChangesAsync();
      }
      catch (Exception)
      {
        throw;
      }
    }
  }

  public static IFormFile GetImageAsFormFile(string file)
  {
    // Validate the URL
    if (string.IsNullOrEmpty(file))
    {
      throw new ArgumentException("Image filename cannot be null or empty.", nameof(file));
    }

    try
    {
      var stream = File.OpenRead(file);
      var fileName = Path.GetFileName(file);
      var contentType = GetContentTypeFromUrl(file);

      // Return the IFormFile object
      return new FormFile(stream, 0, stream.Length, null, fileName)
      {
        Headers = new HeaderDictionary(),
        ContentType = contentType
      };
    }
    catch (Exception ex)
    {
      // Handle general exceptions
      throw new InvalidOperationException("An error occurred while processing the image.", ex);
    }
  }

  private static string GetContentTypeFromUrl(string imageUrl)
  {
    var fileExtension = Path.GetExtension(imageUrl).ToLowerInvariant();

    return fileExtension switch
    {
      ".jpg" or ".jpeg" => "image/jpeg",
      ".png" => "image/png",
      ".gif" => "image/gif",
      ".bmp" => "image/bmp",
      ".tiff" or ".tif" => "image/tiff",
      ".webp" => "image/webp",
      _ => "application/octet-stream"  // Default to a binary content type if unknown
    };
  }

  public static async Task<byte[]> ConvertFileToByteArrayAsync(IFormFile file)
  {
    try
    {
      MemoryStream memoryStream = new();
      await file.CopyToAsync(memoryStream);
      byte[] byteFile = memoryStream.ToArray();
      memoryStream.Close();
      memoryStream.Dispose();

      return byteFile;
    }
    catch (Exception)
    {

      throw;
    }
  }

  public static async Task SeedDefaultUsersAsync(UserManager<BTUser> userManager)
  {  
    //Seed Default Admin User
    var defaultUser = new BTUser
    {
      UserName = "btadmin1@bugtracker.com",
      Email = "btadmin1@bugtracker.com",
      FirstName = "Bill",
      LastName = "Tucker",
      EmailConfirmed = true,
      CompanyId = company1Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\4.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Admin User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }

    //Seed Default Admin User
    defaultUser = new BTUser
    {
      UserName = "btadmin2@bugtracker.com",
      Email = "btadmin2@bugtracker.com",
      FirstName = "Steve",
      LastName = "Johnson",
      EmailConfirmed = true,
      CompanyId = company2Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\1.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Admin User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default ProjectManager1 User
    defaultUser = new BTUser
    {
      UserName = "ProjectManager1@bugtracker.com",
      Email = "ProjectManager1@bugtracker.com",
      FirstName = "John",
      LastName = "Williams",
      EmailConfirmed = true,
      CompanyId = company1Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\2.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.ProjectManager.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default ProjectManager1 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default ProjectManager2 User
    defaultUser = new BTUser
    {
      UserName = "ProjectManager2@bugtracker.com",
      Email = "ProjectManager2@bugtracker.com",
      FirstName = "Jane",
      LastName = "Smith",
      EmailConfirmed = true,
      CompanyId = company2Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\11.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.ProjectManager.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default ProjectManager2 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default Developer1 User
    defaultUser = new BTUser
    {
      UserName = "Developer1@bugtracker.com",
      Email = "Developer1@bugtracker.com",
      FirstName = "Elon",
      LastName = "Brown",
      EmailConfirmed = true,
      CompanyId = company1Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\5.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Developer1 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default Developer2 User
    defaultUser = new BTUser
    {
      UserName = "Developer2@bugtracker.com",
      Email = "Developer2@bugtracker.com",
      FirstName = "James",
      LastName = "Garcia",
      EmailConfirmed = true,
      CompanyId = company2Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\9.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Developer2 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default Developer3 User
    defaultUser = new BTUser
    {
      UserName = "Developer3@bugtracker.com",
      Email = "Developer3@bugtracker.com",
      FirstName = "Natasha",
      LastName = "Jones",
      EmailConfirmed = true,
      CompanyId = company1Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\6.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Developer3 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default Developer4 User
    defaultUser = new BTUser
    {
      UserName = "Developer4@bugtracker.com",
      Email = "Developer4@bugtracker.com",
      FirstName = "Carol",
      LastName = "Miller",
      EmailConfirmed = true,
      CompanyId = company2Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\8.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Developer4 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default Developer5 User
    defaultUser = new BTUser
    {
      UserName = "Developer5@bugtracker.com",
      Email = "Developer5@bugtracker.com",
      FirstName = "Tony",
      LastName = "Davis",
      EmailConfirmed = true,
      CompanyId = company1Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\7.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Developer5 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }

    //Seed Default Developer6 User
    defaultUser = new BTUser
    {
      UserName = "Developer6@bugtracker.com",
      Email = "Developer6@bugtracker.com",
      FirstName = "Bruce",
      LastName = "Rodriguez",
      EmailConfirmed = true,
      CompanyId = company2Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\3.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Developer5 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }

    //Seed Default Submitter1 User
    defaultUser = new BTUser
    {
      UserName = "Submitter1@bugtracker.com",
      Email = "Submitter1@bugtracker.com",
      FirstName = "Scott",
      LastName = "Martinez",
      EmailConfirmed = true,
      CompanyId = company1Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\12.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Submitter.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Submitter1 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Default Submitter2 User
    defaultUser = new BTUser
    {
      UserName = "Submitter2@bugtracker.com",
      Email = "Submitter2@bugtracker.com",
      FirstName = "Sue",
      LastName = "Anderson",
      EmailConfirmed = true,
      CompanyId = company2Id,
      AvatarFormFile = GetImageAsFormFile("..\\workflo\\wwwroot\\img\\avatars\\10.png")
    };

    defaultUser.AvatarFileData = await ConvertFileToByteArrayAsync(defaultUser.AvatarFormFile);
    defaultUser.AvatarFileName = defaultUser.AvatarFormFile.FileName;
    defaultUser.AvatarContentType = defaultUser.AvatarFormFile.ContentType;

    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Submitter.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Default Submitter2 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }

  }

  public static async Task SeedDemoUsersAsync(UserManager<BTUser> userManager)
  {
    //Seed Demo Admin User
    var defaultUser = new BTUser
    {
      UserName = "demoadmin@bugtracker.com",
      Email = "demoadmin@bugtracker.com",
      FirstName = "Demo",
      LastName = "Admin",
      EmailConfirmed = true,
      CompanyId = company1Id
    };
    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
        await userManager.AddToRoleAsync(defaultUser, Roles.DemoUser.ToString());

      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Demo Admin User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Demo ProjectManager User
    defaultUser = new BTUser
    {
      UserName = "demopm@bugtracker.com",
      Email = "demopm@bugtracker.com",
      FirstName = "Demo",
      LastName = "ProjectManager",
      EmailConfirmed = true,
      CompanyId = company1Id
    };
    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.ProjectManager.ToString());
        await userManager.AddToRoleAsync(defaultUser, Roles.DemoUser.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Demo ProjectManager1 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Demo Developer User
    defaultUser = new BTUser
    {
      UserName = "demodev@bugtracker.com",
      Email = "demodev@bugtracker.com",
      FirstName = "Demo",
      LastName = "Developer",
      EmailConfirmed = true,
      CompanyId = company1Id
    };
    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Developer.ToString());
        await userManager.AddToRoleAsync(defaultUser, Roles.DemoUser.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Demo Developer1 User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Demo Submitter User
    defaultUser = new BTUser
    {
      UserName = "demosub@bugtracker.com",
      Email = "demosub@bugtracker.com",
      FirstName = "Demo",
      LastName = "Submitter",
      EmailConfirmed = true,
      CompanyId = company1Id
    };
    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Submitter.ToString());
        await userManager.AddToRoleAsync(defaultUser, Roles.DemoUser.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Demo Submitter User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }


    //Seed Demo New User
    defaultUser = new BTUser
    {
      UserName = "demonew@bugtracker.com",
      Email = "demonew@bugtracker.com",
      FirstName = "Demo",
      LastName = "NewUser",
      EmailConfirmed = true,
      CompanyId = company2Id
    };
    try
    {
      var user = await userManager.FindByEmailAsync(defaultUser.Email);
      if (user == null)
      {
        await userManager.CreateAsync(defaultUser, "Abc&123!");
        await userManager.AddToRoleAsync(defaultUser, Roles.Submitter.ToString());
        await userManager.AddToRoleAsync(defaultUser, Roles.DemoUser.ToString());
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Demo New User.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }



  public static async Task SeedDefaultTicketTypeAsync(ApplicationDbContext context)
  {
    try
    {
      IList<TicketType> ticketTypes = new List<TicketType>() {
                   new TicketType() { Name = BTTicketType.NewDevelopment.ToString() },      // Ticket involves development of a new, uncoded solution 
                   new TicketType() { Name = BTTicketType.WorkTask.ToString() },            // Ticket involves development of the specific ticket description 
                   new TicketType() { Name = BTTicketType.Defect.ToString()},               // Ticket involves unexpected development/maintenance on a previously designed feature/functionality
                   new TicketType() { Name = BTTicketType.ChangeRequest.ToString() },       // Ticket involves modification development of a previously designed feature/functionality
                   new TicketType() { Name = BTTicketType.Enhancement.ToString() },         // Ticket involves additional development on a previously designed feature or new functionality
                   new TicketType() { Name = BTTicketType.GeneralTask.ToString() }          // Ticket involves no software development but may involve tasks such as configuations, or hardware setup
              };

      var dbTicketTypes = context.TicketTypes.Select(c => c.Name).ToList();
      await context.TicketTypes.AddRangeAsync(ticketTypes.Where(c => !dbTicketTypes.Contains(c.Name)));
      await context.SaveChangesAsync();

    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Ticket Types.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }

  public static async Task SeedDefaultTicketStatusAsync(ApplicationDbContext context)
  {
    try
    {
      IList<TicketStatus> ticketStatuses = new List<TicketStatus>() {
                  new TicketStatus() { Name = BTTicketStatus.New.ToString() },                 // Newly Created ticket having never been assigned
                  new TicketStatus() { Name = BTTicketStatus.Development.ToString() },         // Ticket is assigned and currently being worked 
                  new TicketStatus() { Name = BTTicketStatus.Testing.ToString()  },            // Ticket is assigned and is currently being tested
                  new TicketStatus() { Name = BTTicketStatus.Resolved.ToString()  },           // Ticket remains assigned to the developer but work in now complete
              };

      var dbTicketStatuses = context.TicketStatuses.Select(c => c.Name).ToList();
      await context.TicketStatuses.AddRangeAsync(ticketStatuses.Where(c => !dbTicketStatuses.Contains(c.Name)));
      await context.SaveChangesAsync();

    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Ticket Statuses.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }

  public static async Task SeedDefaultTicketPriorityAsync(ApplicationDbContext context)
  {
    try
    {
      IList<TicketPriority> ticketPriorities = new List<TicketPriority>() {
                                                  new TicketPriority() { Name = BTTicketPriority.Low.ToString()  },
                                                  new TicketPriority() { Name = BTTicketPriority.Medium.ToString() },
                                                  new TicketPriority() { Name = BTTicketPriority.High.ToString()},
                                                  new TicketPriority() { Name = BTTicketPriority.Urgent.ToString()},
              };

      var dbTicketPriorities = context.TicketPriorities.Select(c => c.Name).ToList();
      await context.TicketPriorities.AddRangeAsync(ticketPriorities.Where(c => !dbTicketPriorities.Contains(c.Name)));
      context.SaveChanges();

    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Ticket Priorities.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }



  public static async Task SeedDefautTicketsAsync(ApplicationDbContext context)
  {
    //Get project Ids
    int portfolioId = context.Projects.FirstOrDefault(p => p.Name == "Build a Personal Porfolio Website").Id;
    int blogId = context.Projects.FirstOrDefault(p => p.Name == "Build a Supplemental Blog Web Application").Id;
    int bugtrackerId = context.Projects.FirstOrDefault(p => p.Name == "Build an Issue Tracking Web Application").Id;
    int movieId = context.Projects.FirstOrDefault(p => p.Name == "Build a Movie Information Web Application").Id;

    //Get ticket type Ids
    int typeNewDev = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.NewDevelopment.ToString()).Id;
    int typeWorkTask = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.WorkTask.ToString()).Id;
    int typeDefect = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.Defect.ToString()).Id;
    int typeEnhancement = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.Enhancement.ToString()).Id;
    int typeChangeRequest = context.TicketTypes.FirstOrDefault(p => p.Name == BTTicketType.ChangeRequest.ToString()).Id;

    //Get ticket priority Ids
    int priorityLow = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.Low.ToString()).Id;
    int priorityMedium = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.Medium.ToString()).Id;
    int priorityHigh = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.High.ToString()).Id;
    int priorityUrgent = context.TicketPriorities.FirstOrDefault(p => p.Name == BTTicketPriority.Urgent.ToString()).Id;

    //Get ticket status Ids
    int statusNew = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.New.ToString()).Id;
    int statusDev = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.Development.ToString()).Id;
    int statusTest = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.Testing.ToString()).Id;
    int statusResolved = context.TicketStatuses.FirstOrDefault(p => p.Name == BTTicketStatus.Resolved.ToString()).Id;


    try
    {
      IList<Ticket> tickets = new List<Ticket>() {
                              //PORTFOLIO
                              new Ticket() {Title = "1. Define Website Purpose and Content Structure", Description = "Determine the goals of the website (e.g., showcase work, blog, resume). Organize sections like \"About Me,\" \"Portfolio,\" \"Resume,\" \"Contact,\" and possibly a blog.", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "2. Design Website Layout and User Interface", Description = "Create wireframes or mockups for the site layout using design tools (e.g., Figma, Adobe XD). Plan the color scheme, typography, and overall visual style to match brand.", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "3. Choose a Technology Stack", Description = "Decide whether to use HTML/CSS/JavaScript or frameworks like React or Vue.js. Select any additional tools (e.g., CSS frameworks like Bootstrap, Tailwind CSS).", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "4. Develop Website Front-End", Description = "Code the website layout, navigation, and interactive elements using HTML, CSS, and JavaScript. Ensure mobile responsiveness with media queries or a responsive design framework.", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityUrgent, TicketStatusId = statusTest, TicketTypeId = typeDefect},
                              new Ticket() {Title = "5. Integrate Portfolio Content", Description = "Add projects, images, descriptions, and any relevant multimedia for portfolio section. Organize work into categories or highlight key projects.", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "6. Set Up a Contact Form", Description = "Implement a simple contact form that allows visitors to reach out. Ensure form validation and email integration", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "7. Implement Website Hosting and Domain Name", Description = "Choose a hosting provider (e.g., Netlify, GitHub Pages, Vercel) and deploy the site. Register a custom domain name and link it to hosted website.", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "8. Test Website and Optimize Performance", Description = "Test the website across different browsers and devices for compatibility. Optimize loading speed (e.g., image compression, lazy loading) and ensure SEO basics are covered.", Created = DateTimeOffset.Now, ProjectId = portfolioId, TicketPriorityId = priorityUrgent, TicketStatusId = statusTest, TicketTypeId = typeDefect},
                              //BLOG
                              new Ticket() {Title = "1. Define Project Scope and Requirements", Description = "Identify the main purpose of the blog (e.g., personal, professional, niche topic). Determine the target audience and content style (e.g., articles, tutorials, opinion pieces).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeDefect},
                              new Ticket() {Title = "2. Choose a Platform or Technology Stack", Description = "Decide whether to use a CMS (e.g., WordPress, Ghost) or custom development (e.g., React, Next.js). Select a database solution (if applicable) for storing blog content (e.g., MySQL, MongoDB).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "3. Design Website Layout and User Interface", Description = "Create wireframes or mockups for key pages (e.g., homepage, blog post page, categories). Select color schemes, fonts, and branding elements.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "4. Develop Site Structure and Navigation", Description = "Plan site architecture (e.g., homepage, blog categories, archive pages, individual blog posts). Design navigation menus (e.g., main menu, footer links).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "5. Set Up Hosting and Domain", Description = "Choose a hosting provider (e.g., Bluehost, SiteGround, DigitalOcean, or cloud-based like Netlify or Vercel). Purchase and set up a custom domain name.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityLow, TicketStatusId = statusDev,  TicketTypeId = typeDefect},
                              new Ticket() {Title = "6. Set Up Content Management System (CMS)", Description = "Install and configure the chosen CMS (e.g., WordPress, Ghost). Set up necessary plugins for SEO, performance, and security.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew,  TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "7. Create Homepage and Blog Post Layout Templates", Description = "Develop a homepage layout to display the latest blog posts and content. Design a blog post page template with sections like title, author, content, comments, and related posts.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "8. Develop Content Creation and Editing Interface", Description = "Create an easy-to-use content editor for writing and publishing posts (either through a CMS or custom-built). Implement functionality for adding images, videos, and embedded content.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityUrgent, TicketStatusId = statusDev,  TicketTypeId = typeNewDev},
                              new Ticket() {Title = "9. Set Up Categories and Tags", Description = "Create categories to organize blog posts (e.g., \"Tech,\" \"Lifestyle,\" \"Tutorials\"). Implement tags for post-specific topics (e.g., \"Web Development,\" \"SEO Tips\").", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityLow, TicketStatusId = statusNew,  TicketTypeId = typeDefect},
                              new Ticket() {Title = "10. Implement Comment System", Description = "Integrate a comment system (e.g., Disqus, native CMS comment feature). Add spam protection (e.g., CAPTCHA or Akismet) to prevent unwanted comments.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew, TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "11. Implement Social Media Sharing Features", Description = "Add social sharing buttons to blog posts (e.g., Facebook, Twitter, LinkedIn). Enable shareable URLs and meta tags for better sharing.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev,  TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "12. Add Search Functionality", Description = "Implement a search bar for users to find specific blog posts or content. Optimize search functionality with relevant filters (e.g., categories, tags, post types).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew,  TicketTypeId = typeNewDev},
                              new Ticket() {Title = "13. Set Up SEO Optimization", Description = "Configure SEO-friendly URLs, titles, and meta descriptions. Implement on-page SEO practices (e.g., headings, image alt text, internal linking). Set up a sitemap and submit it to search engines.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeDefect},
                              new Ticket() {Title = "14. Develop Mobile Responsiveness", Description = "Ensure the blog design is fully responsive and optimized for mobile devices. Test all components on various screen sizes (e.g., smartphones, tablets).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev,  TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "15. Integrate Email Newsletter System", Description = "Set up an email subscription form for readers to receive updates on new posts. Integrate with email marketing platforms (e.g., Mailchimp, ConvertKit).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew,  TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "16. Deploy the Website", Description = "Publish the website to the chosen hosting platform. Set up domain name and ensure proper DNS configuration.", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "17. Test Website and Optimize Performance", Description = "Test the website across multiple browsers and devices. Optimize site speed (e.g., image compression, lazy loading, caching). Implement security measures (e.g., HTTPS, regular updates, backups).", Created = DateTimeOffset.Now, ProjectId = blogId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev,  TicketTypeId = typeNewDev},
                              //BUGTRACKER                                                                                                                         
                              new Ticket() {Title = "1. Define Project Scope and Requirements", Description = "Identify key features for the issue tracking application (e.g., issue creation, assignment, comments, reporting). Define user roles (e.g., Admin, User, Manager) and their permissions. Establish success metrics for the application (e.g., issue resolution time, user engagement).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "2. Set Up Development Environment", Description = "Install the necessary SDKs for ASP.NET MVC. Set up the local development environment with SQL Server or another database of choice.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "3. Create Project Repository and Version Control", Description = "Initialize a Git repository for version control. Set up collaboration tools (e.g., GitHub, GitLab). Define a branching strategy (e.g., feature branches, main branch).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "4. Design Database Schema", Description = "Create a database schema for issues, users, comments, and roles. Define relationships between tables (e.g., Users <-> Issues, Issues <-> Comments). Set up Entity Framework (EF) or another ORM for data access.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "5. Implement User Authentication and Authorization", Description = "Use ASP.NET Identity for user authentication (e.g., login, registration). Implement role-based access control (RBAC) to manage user roles and permissions. Enable password recovery and email verification.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "6. Set Up Project Structure in ASP.NET MVC", Description = "Create the basic structure for the ASP.NET MVC project (Controllers, Views, Models). Set up routing for key pages (e.g., Dashboard, Issues, Profile). Implement basic layout and common elements (e.g., navigation bar, footer).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "7. Design the User Interface (UI)", Description = "Design wireframes or mockups for key pages (e.g., Issue Dashboard, Issue Detail Page, User Profile). Choose a UI framework (e.g., Bootstrap, Tailwind CSS) for styling and responsiveness. Create reusable UI components (e.g., buttons, cards, modals).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "8. Develop the Dashboard Page", Description = "Create a dashboard view displaying a summary of active issues, tasks, and performance metrics. Display key information, such as open issues, resolved issues, and user activity. Implement pagination or infinite scroll to handle large data sets.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "9. Create the Issue List View", Description = "Develop a view to display all issues with basic details (e.g., title, status, assigned user). Implement sorting and filtering options for issues (e.g., by status, priority, assignee). Enable search functionality to allow users to find specific issues.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "10. Create the Issue Creation Form", Description = "Build a form to allow users to create new issues (e.g., title, description, priority, due date). Implement input validation for required fields and proper formatting. Enable file attachment uploads for issue-related documents (e.g., screenshots, reports).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "11. Develop Issue Detail Page", Description = "Implement a page that shows detailed information about a specific issue (e.g., title, description, status, history). Display associated comments, status history, and file attachments. Enable users to add comments or update the status of an issue.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "12. Implement Status Updates for Issues", Description = "Create functionality to allow users to update the status of an issue (e.g., Open, In Progress, Resolved). Log status changes with timestamps and user information. Display status history for transparency and tracking.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "13. Create User Profile Page", Description = "Build a profile page to display user-specific information (e.g., username, role, assigned issues). Allow users to edit their profile details (e.g., password, email). Show user activity (e.g., number of issues resolved, comments posted).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "14. Set Up Role-Based Access Control (RBAC)", Description = "Define roles such as Admin, User, and Manager within the system. Implement access restrictions for certain pages and actions based on the user role. Allow admins to assign roles to users.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "15. Implement Comment System", Description = "Allow users to add comments to issues. Display comments with user names and timestamps. Implement comment editing and deletion functionality (for the commenter).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "16. Set Up Email Notification System", Description = "Implement email notifications for key actions (e.g., issue creation, status updates, new comments). Configure email templates for notification messages. Allow users to manage their email notification preferences.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "17. Develop Search Functionality for Issues", Description = "Implement a search bar that enables users to find issues by keywords, tags, or other attributes. Optimize search performance using indexing or full-text search capabilities. Include advanced filters (e.g., by date, assignee, priority).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "18. Set Up File Attachments for Issues", Description = "Allow users to upload files (e.g., screenshots, documents) when creating or editing issues. Display uploaded files in the issue detail view. Implement file size and type validation to prevent unwanted file types.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "19. Set Up API Endpoints for Integration", Description = "Design and implement RESTful API endpoints for CRUD operations (e.g., create, read, update, delete issues). Secure API endpoints with authentication and authorization (e.g., JWT). Document APIs for third-party integration or future use.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "20. Implement Filtering and Sorting for Issues", Description = "Implement filters to view issues by criteria such as status, assignee, priority, and creation date. Enable sorting of issues based on various attributes (e.g., by most recent, by priority). Implement multi-filter functionality to allow advanced filtering combinations.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "21. Create Reports and Analytics", Description = "Build reporting functionality to visualize issue data (e.g., number of open vs. resolved issues). Generate downloadable reports in CSV or PDF format. Create charts or graphs to show issue trends over time (e.g., issue resolution time, backlog).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "22. Implement Data Validation and Error Handling", Description = "Add data validation for forms (e.g., issue creation, user profile updates). Handle errors gracefully with user-friendly error messages. Use logging (e.g., Serilog or NLog) for tracking errors and application issues.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "23. Ensure Cross-Browser Compatibility", Description = "Test the application in different browsers (e.g., Chrome, Firefox, Edge, Safari). Fix any layout or functionality issues specific to certain browsers. Ensure responsiveness across devices, including mobile and tablet.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "24. Optimize Performance", Description = "Optimize SQL queries for efficiency and speed. Implement lazy loading for large datasets (e.g., issue lists, comments). Use caching for frequently accessed data (e.g., issue lists, user profiles).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "25. Implement Security Best Practices", Description = "Use HTTPS for secure communication across the application. Implement CSRF protection for forms and actions that modify data. Validate and sanitize inputs to prevent SQL injection and XSS attacks.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "26. Set Up Continuous Integration (CI) and Continuous Deployment (CD)", Description = "Configure CI/CD pipelines using tools like Azure DevOps or GitHub Actions. Automate testing, building, and deployment processes. Set up staging and production environments for smooth deployment.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "27. Perform User Acceptance Testing (UAT)", Description = "Conduct usability testing with end-users (e.g., developers, managers). Collect feedback on functionality, performance, and user interface. Address issues raised during UAT and refine the application.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "28. Conduct Load and Stress Testing", Description = "Test how the application handles a high volume of concurrent users and requests. Identify and address performance bottlenecks. Simulate stress scenarios (e.g., large number of issues, file uploads).", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "29. Launch the Application", Description = "Perform a final review of all features and functionality. Deploy the application to the production environment. Announce the launch and provide user documentation.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "30. Post-Launch Support and Maintenance", Description = "Monitor the application for bugs and performance issues post-launch. Provide regular updates and patches to address issues and enhance features. Collect user feedback and iterate on the application as needed.", Created = DateTimeOffset.Now, ProjectId = bugtrackerId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              //MOVIE
                              new Ticket() {Title = "1. Define Project Scope and Requirements", Description = "Define core features for the application, such as movie details input, movie poster uploads, and cast/crew management. Identify roles (e.g., Admin, User) and permissions for managing movie data. Establish metrics and goals (e.g., number of movies added, user interactions).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeDefect},
                              new Ticket() {Title = "2. Set Up Development Environment", Description = "Set up PostgreSQL database locally and install necessary PostgreSQL drivers for C#. Set up ASP.NET MVC environment and create a new project.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev, TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "3. Set Up Database Schema", Description = "Design the PostgreSQL schema to store movie data (e.g., Movie, Cast, Crew, Genres). Define relationships between tables (e.g., Movies <-> Cast, Movies <-> Crew). Set up Entity Framework (EF) for interacting with the database.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "4. Create Project Repository and Version Control", Description = "Initialize a Git repository for version control. Set up GitHub, GitLab, or Bitbucket for collaboration and project management. Define a branching strategy for development (e.g., feature branches, main branch).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "5. Design Movie Model and Entities", Description = "Create C# models for the movie details (e.g., Movie, Cast, Crew, Genre). Include properties like title, release date, description, and genres. Define foreign keys for relationships between entities (e.g., Movie has many Cast and Crew).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityLow, TicketStatusId = statusDev,  TicketTypeId = typeDefect},
                              new Ticket() {Title = "6. Set Up User Authentication and Authorization", Description = "Implement ASP.NET Identity for user authentication (e.g., login, registration, user roles). Set up role-based access control for users (e.g., Admin can add or delete movie data, Users can only view). Allow for user profile management (e.g., change password, email preferences).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew,  TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "7. Create API Endpoints for Movie Data", Description = "Design and implement RESTful API endpoints for CRUD operations (e.g., Create, Read, Update, Delete for Movies). Implement API endpoints for managing Cast and Crew details. Secure the API with token-based authentication (e.g., JWT).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew, TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "8. Set Up Movie Poster Upload and Storage", Description = "Implement functionality to allow users to upload movie posters (e.g., images, PDFs). Store uploaded images in a directory on the server or use a cloud service (e.g., AWS S3, Azure Blob Storage). Display movie posters on the movie detail page.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityUrgent, TicketStatusId = statusDev,  TicketTypeId = typeNewDev},
                              new Ticket() {Title = "9. Implement Search Functionality for Movies", Description = "Create a search bar for users to find movies by title, genre, or year of release. Implement advanced filtering (e.g., search by cast, crew, or genre). Optimize search performance with indexing or full-text search in PostgreSQL.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityLow, TicketStatusId = statusNew,  TicketTypeId = typeDefect},
                              new Ticket() {Title = "10. Design and Implement Movie Detail Page", Description = "Create a page that displays detailed information about a selected movie (e.g., title, description, poster, cast, crew). Display cast and crew details, including roles and responsibilities (e.g., actor, director, producer). Add the ability to edit or delete movie information (for Admins).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityMedium, TicketStatusId = statusNew, TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "11. Create Cast and Crew Management System", Description = "Allow users to add and manage cast and crew details (e.g., name, role, bio). Allow movie administrators to associate cast and crew with specific movies. Implement a CRUD interface for managing cast and crew data.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev,  TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "12. Implement Movie Genre Management", Description = "Create a system for adding and managing genres (e.g., Drama, Action, Comedy). Enable the association of genres with movies. Display genre tags on the movie detail pages.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew,  TicketTypeId = typeNewDev},
                              new Ticket() {Title = "13. Set Up Data Import Functionality", Description = "Implement an API or UI that allows users to import movie details from external sources (e.g., using a movie database API like TMDb or OMDb). Ensure that imported data includes movies, posters, cast, and crew. Handle data validation and conflict resolution (e.g., duplicate movie entries).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityLow, TicketStatusId = statusNew, TicketTypeId = typeDefect},
                              new Ticket() {Title = "14. Design and Implement API Rate Limiting", Description = "Set up rate limiting for API endpoints to prevent abuse (e.g., limit requests per user). Implement logging for API usage and errors. Define maximum limits for data requests to ensure system stability.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev,  TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "15. Implement Movie and Cast Reviews/Rating System", Description = "Allow users to submit ratings and reviews for movies. Display user reviews and average ratings on the movie detail page. Implement the ability to edit or delete user reviews.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew,  TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "16. Implement Security Best Practices", Description = "Secure sensitive data using encryption (e.g., user passwords, movie data). Use HTTPS for secure communication between the client and server. Prevent common vulnerabilities such as SQL injection and XSS by sanitizing inputs.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev},
                              new Ticket() {Title = "17. Create and Implement Pagination for Movie Listings", Description = "Implement pagination for the movie list to improve performance and user experience. Allow users to navigate between pages of movies (e.g., 10 movies per page). Implement sorting functionality (e.g., by release date, rating, or genre).", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusDev,  TicketTypeId = typeNewDev},
                              new Ticket() {Title = "18. Set Up Email Notifications for Admin and User Activities", Description = "Send email notifications to admins when new movies are added or edited. Notify users when a movie they are following gets updated or receives new reviews. Allow users to manage email notification preferences.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityMedium, TicketStatusId = statusDev,  TicketTypeId = typeEnhancement},
                              new Ticket() {Title = "19. Implement Testing for API and Application", Description = "Write unit tests for API endpoints (e.g., CRUD operations for movies, cast, and crew). Perform integration testing to ensure all parts of the system work together. Conduct load testing to handle high traffic for movie data import/export.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityHigh, TicketStatusId = statusNew,  TicketTypeId = typeChangeRequest},
                              new Ticket() {Title = "20. Deploy the Application to Production", Description = "Set up a production environment (e.g., on AWS, Azure, or DigitalOcean). Deploy the application and database, ensuring proper configuration for production. Monitor application performance and ensure that backups of the database are regularly taken.", Created = DateTimeOffset.Now, ProjectId = movieId, TicketPriorityId = priorityUrgent, TicketStatusId = statusNew, TicketTypeId = typeNewDev}

              };


      var dbTickets = context.Tickets.Select(c => c.Title).ToList();
      await context.Tickets.AddRangeAsync(tickets.Where(c => !dbTickets.Contains(c.Title)));
      await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
      Console.WriteLine("*************  ERROR  *************");
      Console.WriteLine("Error Seeding Tickets.");
      Console.WriteLine(ex.Message);
      Console.WriteLine("***********************************");
      throw;
    }
  }

}
