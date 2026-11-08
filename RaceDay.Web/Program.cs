using System.Globalization;
using RaceDay.Web.Services;

// One culture everywhere, including inside the container, so dates and
// distances do not follow the host machine. en-GB rather than en-ZA because
// en-ZA writes decimals with a comma, which an HTML number input never sends.
var displayCulture = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = displayCulture;
CultureInfo.DefaultThreadCurrentUICulture = displayCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Every data operation goes to the RaceDay API; this app never touches the
// database. ApiBaseUrl is overridden by an environment variable when the app
// runs in Docker, because the API stays on the host.
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
