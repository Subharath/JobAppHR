using JobAppHR.Models;
using JobAppHR.Repository;
using JobAppHR.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var enableDevUserFallback = builder.Configuration.GetValue<bool>("Authentication:EnableDevUserFallback");
var devUserId = builder.Configuration["Authentication:DevUserId"] ?? "dev-user";
var devUserName = builder.Configuration["Authentication:DevUserName"] ?? "Developer";
var devUserEmail = builder.Configuration["Authentication:DevUserEmail"] ?? "dev@example.com";
var devUserGroup = builder.Configuration["Authentication:DevUserGroup"] ?? "DEV";
var devUserRole = builder.Configuration["Authentication:DevUserRole"] ?? "Admin";

// Add services to the container.
builder.Services.AddControllersWithViews();

// Persist Data Protection keys to a fixed folder to prevent Antiforgery/Session key loss on App Pool recycle
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"C:\inetpub\wwwroot\JobAppHR\Keys"))
    .SetApplicationName("JobAppHR");

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromSeconds(60);
});

builder.Services.Configure<FormOptions>(x => x.ValueCountLimit = 10000);

// MODIFIED FOR LOCAL DEVELOPMENT - Allow anonymous access without Azure AD
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/AzureLogin";
        options.Cookie.Name = "JobAppHRWebCookie";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // ALLOW ANONYMOUS ACCESS FOR LOCAL DEVELOPMENT
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAssertion(_ => true) // Always return true = always authorized
        .Build();
    
    options.AddPolicy("NormalUserPolicy",
        policy => policy.RequireAssertion(_ => true)); // Always allow

    options.AddPolicy("AdminUserPolicy",
        policy => policy.RequireAssertion(_ => true)); // Always allow
});

builder.Services.AddScoped<IDBOperations, DBOperations>();
builder.Services.AddScoped<IUtilityFn, UtilityFn>();
builder.Services.AddScoped<IFilterProcess, FilterProcess>();
builder.Services.AddScoped<IManualProcess, ManualProcess>();
builder.Services.AddScoped<IFastAPIProcess, FastAPIProcess>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
    StaticData.BaseUrl = app.Configuration["URL:BaseUrl-Local"];
else 
    StaticData.BaseUrl = app.Configuration["URL:BaseUrl-Production"];

StaticData.DefaultConnection = app.Configuration["ConnectionStrings:DefaultConnection"];
StaticData.UploadPath = app.Configuration["Application:UploadPath"];
StaticData.FastAPIUrl = app.Configuration["URL:FastAPIUrl"];

app.UseHttpsRedirection();

app.UseStatusCodePages(async context => {
    if (context.HttpContext.Response.StatusCode == 404)
    {
        context.HttpContext.Response.Redirect("/Home/AccessDenied");
    }
});

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// Dev or opt-in fallback for server testing when Azure AD is bypassed.
if (app.Environment.IsDevelopment() || enableDevUserFallback)
{
    app.Use(async (context, next) =>
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            var userId = context.Session.GetString("UserId") ?? devUserId;
            var userName = context.Session.GetString("UserName") ?? devUserName;
            var userEmail = context.Session.GetString("UserEmail") ?? devUserEmail;
            var userGroup = context.Session.GetString("UserGroup") ?? devUserGroup;
            var userRole = context.Session.GetString("UserRole") ?? devUserRole;

            var claims = new List<Claim>
            {
                new Claim("UserId", userId),
                new Claim("UserName", userName),
                new Claim("UserEmail", userEmail),
                new Claim("UserGroup", userGroup),
                new Claim("UserRole", userRole)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(identity);
        }

        await next();
    });
}

// Enable auth (cookie-based) so HttpContext.User is populated when a cookie exists
app.UseAuthentication();
// Keep authorization middleware to prevent errors, but currently policies allow all
app.UseAuthorization();

app.UseCookiePolicy(
new CookiePolicyOptions
{
    Secure = app.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always
});

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    //context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; style-src 'self'; script-src 'self'; connect-src 'self'; object-src 'self'; frame-ancestors 'none'; img-src 'self'; form-action 'self'");
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
