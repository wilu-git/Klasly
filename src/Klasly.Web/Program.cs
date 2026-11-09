using Klasly.Web.Infrastructure;
using Klasly.Web.Repositories;
using Klasly.Web.Repositories.Interfaces;
using Klasly.Web.Services;
using Klasly.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = new Uri(builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl is not configured."));

builder.Services.AddControllersWithViews(options => options.Filters.Add<ApiExceptionFilter>());

// The JWT from the API is stored inside this encrypted cookie (StoreTokens), never readable by page JavaScript.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Klasly.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = false; // the login must end when the JWT inside it ends
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events.OnRedirectToLogin = context =>
        {
            // The browser still sent an auth cookie that no longer works: the login (and its JWT) expired.
            if (context.Request.Cookies.ContainsKey("Klasly.Auth"))
            {
                var tempData = context.HttpContext.RequestServices
                    .GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context.HttpContext);
                tempData["Error"] = "Your session ended, please log in again.";
                tempData.Save(); // outside MVC, TempData isn't saved automatically
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

// Every API-backed repository is a typed HttpClient that carries the user's JWT (guide §4, §13).
void AddApiClient<TRepository, TImplementation>()
    where TRepository : class
    where TImplementation : class, TRepository =>
    builder.Services.AddHttpClient<TRepository, TImplementation>(client =>
        {
            client.BaseAddress = apiBaseUrl;
            client.Timeout = TimeSpan.FromSeconds(15); // a dead API shows the error page quickly
        })
        .AddHttpMessageHandler<BearerTokenHandler>();

AddApiClient<IAuthRepository, AuthRepository>();
AddApiClient<ITermRepository, TermRepository>();
AddApiClient<ISubjectRepository, SubjectRepository>();
AddApiClient<IAccountRepository, AccountRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITermService, TermService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IAccountService, AccountService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
