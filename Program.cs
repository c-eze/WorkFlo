using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services.Factories;
using AspnetCoreMvcFull.Security;
using AspnetCoreMvcFull.Services.Interfaces;
using AspnetCoreMvcFull.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

//var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(DataUtility.GetConnectionString(configuration),
    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<BTUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();
services.AddIdentity<BTUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<BTUserClaimsPrincipalFactory>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

services.AddScoped<IBTRolesService, BTRolesService>();
services.AddScoped<IBTCompanyInfoService, BTCompanyInfoService>();
services.AddScoped<IBTProjectService, BTProjectService>();
services.AddScoped<IBTTicketService, BTTicketService>();
services.AddScoped<IBTTicketHistoryService, BTTicketHistoryService>();
services.AddScoped<IBTNotificationService, BTNotificationService>();
services.AddScoped<IBTInviteService, BTInviteService>();
services.AddScoped<IBTFileService, BTFileService>();
services.AddScoped<IBTLookupService, BTLookupService>();

services.AddScoped<IEmailSender, BTEmailService>();

services.Configure<MailSettings>(configuration.GetSection("MailSettings"));

services.AddSingleton<DataProtectionPurposeStrings>();

services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
services.AddControllersWithViews();

services.AddRazorPages();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8081";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

await DataUtility.ManageDataAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
  app.UseMigrationsEndPoint();
}

//app.UseHealthChecks("/health");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
