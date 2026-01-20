using JobAppHR.Models;
using JobAppHR.Repository;
using JobAppHR.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromSeconds(60);
});

builder.Services.Configure<FormOptions>(x => x.ValueCountLimit = 10000);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Specify where to redirect un-authenticated users
        options.LoginPath = "/Home/AzureLogin";

        // Specify the name of the auth cookie.
        // ASP.NET picks a dumb name by default.
        options.Cookie.Name = "JobAppHRWebCookie";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NormalUserPolicy",
        policy => policy.RequireClaim("UserRole","Normal","Admin"));

    options.AddPolicy("AdminUserPolicy",
        policy => policy.RequireClaim("UserRole","Admin"));
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

app.UseAuthentication();

app.UseAuthorization();

app.UseSession(); //The order of middleware is important. Call UseSession after UseRouting and before MapRazorPages and MapDefaultControllerRoute

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
