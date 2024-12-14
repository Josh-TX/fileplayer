using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Enable CORS to allow requests from any domain
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()  // Allow requests from any origin (domain)
              .AllowAnyHeader()  // Allow any headers
              .AllowAnyMethod(); // Allow any HTTP method (GET, POST, etc.)
    });
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1024L * 1024 * 1024 * 2; // 2 GB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024 * 1024 * 2; // 2 GB
});

builder.Services.AddSingleton<DurationService>();
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<DownloadService>();

var app = builder.Build();

app.UseDefaultFiles();
// Serve files from wwwroot folder
app.UseStaticFiles();

// Serve files from the "data" folder
#if DEBUG
var dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
#else
var dataFolderPath = "/data";
#endif
if (Directory.Exists(dataFolderPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(dataFolderPath),
        RequestPath = "/data"
    });
}
app.UseCors();

app.MapControllers();

// Run the application
app.Run();