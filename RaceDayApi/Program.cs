using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using RaceDayApi.Data;
using RaceDayApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Local secrets (such as the Azure storage connection string) can live in this git-ignored file.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// Logged-in state lives in a server-side session. The cookie only holds the session id.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDayApi.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Images go to Azure Blob Storage when a connection string is set (user-secrets, an environment variable or
// appsettings.Local.json). Without one, the API keeps them on its own disk, so it still runs on any machine.
var blobConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    var containerName = builder.Configuration["AzureStorage:ContainerName"] ?? "raceday-images";
    builder.Services.AddSingleton(new BlobContainerClient(blobConnectionString, containerName));
    builder.Services.AddScoped<IImageStorage, AzureBlobImageStorage>();
}
else
{
    builder.Services.AddScoped<IImageStorage, LocalImageStorage>();
}
builder.Services.AddScoped<OrganiserAccess>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Events, categories, enrolments and results for road running, walking and cycling events. "
            + "Log in with POST /api/auth/login first; the session cookie is then sent automatically from Swagger UI."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

var app = builder.Build();

// Creates or updates the schema and loads the demo data when the database is empty.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("Database:SeedDemoData", true))
    {
        await DemoDataSeeder.SeedAsync(db);
        await UpcomingEventsSeeder.SeedAsync(db);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

// Images saved by LocalImageStorage are served from wwwroot/uploads. The folder is git-ignored, so a fresh clone
// may not have it yet. I create it here and point the file provider at it, so it works either way.
var uploadFolder = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadFolder);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadFolder),
    RequestPath = "/uploads"
});

app.UseSession();

app.MapControllers();

app.Run();

// Exposes the entry point to the test project so WebApplicationFactory can host the API.
public partial class Program;
