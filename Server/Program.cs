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

builder.Services.AddSingleton<DurationService>();
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<SettingsService>();

var app = builder.Build();

app.UseDefaultFiles();
// Serve files from wwwroot folder
app.UseStaticFiles();

// Serve files from the "data" folder
var dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
if (Directory.Exists(dataFolderPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(dataFolderPath),
        RequestPath = Path.Combine(BasePathHelper.BasePath, "data")
    });
}
app.UseCors();

app.MapControllers();

// Run the application
app.Run();