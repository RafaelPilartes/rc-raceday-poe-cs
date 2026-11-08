using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// Sessions are held server side and keyed by a cookie; the client never sees
// the user id or the role, which is what the brief asks for instead of a token.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "RESTful API for the RaceDay event management system, built to the endpoint plan in "
            + "docs/RaceDay_API_Endpoint_Plan.md.\n\n"
            + "Access is driven by a server-side session. Register through "
            + "POST /api/auth/register, then POST /api/auth/login: the RaceDay.Session cookie "
            + "that comes back is sent automatically by this page, so every endpoint below can be "
            + "tried straight from here. Endpoints marked Organiser or Participant answer 403 for "
            + "the other role, and 401 when there is no session at all."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

// Swagger stays on in every environment: for Part 2 it is the way the API is
// demonstrated and marked, not just a development convenience.
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "RaceDay API");

app.UseHttpsRedirection();
app.UseSession();
app.MapControllers();

app.Run();
